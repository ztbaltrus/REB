using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using REB.Engine.Audio.Components;
using REB.Engine.ECS;
using REB.Engine.Rendering.Components;

namespace REB.Engine.Audio.Systems;

/// <summary>
/// Manages <see cref="SoundEffectInstance"/> lifetimes and applies positional audio.
/// <para>
/// Lifecycle per source entity:
///   • <c>Play = true</c>  → create or resume instance, apply 3D position.
///   • <c>Play = false</c> → stop and release instance.
///   • Entity destroyed   → instance is stopped and removed automatically.
/// </para>
/// </summary>
public sealed class AudioSystem : GameSystem
{
    private readonly AudioListener _listener = new();
    private readonly AudioEmitter  _emitter  = new();

    /// <summary>Tracked instances keyed by entity index.</summary>
    private readonly Dictionary<uint, SoundEffectInstance> _instances = new();

    public override void Update(float deltaTime)
    {
        UpdateListener();
        UpdateSources();
        CleanupStoppedInstances();
    }

    // -------------------------------------------------------------------------
    //  Listener
    // -------------------------------------------------------------------------

    private void UpdateListener()
    {
        foreach (var entity in World.Query<AudioListenerComponent, TransformComponent>())
        {
            ref var listener  = ref World.GetComponent<AudioListenerComponent>(entity);
            ref var transform = ref World.GetComponent<TransformComponent>(entity);

            if (!listener.IsActive) continue;

            _listener.Position = transform.Position;
            _listener.Forward  = transform.Forward;
            _listener.Up       = transform.Up;
            break;
        }
    }

    // -------------------------------------------------------------------------
    //  Sources
    // -------------------------------------------------------------------------

    private void UpdateSources()
    {
        foreach (var entity in World.Query<AudioSourceComponent>())
        {
            ref var source = ref World.GetComponent<AudioSourceComponent>(entity);
            if (source.SoundEffect == null) continue;

            if (source.Play)
            {
                EnsureInstance(entity, ref source);
            }
            else
            {
                StopInstance(entity.Index);
            }
        }
    }

    private void EnsureInstance(Entity entity, ref AudioSourceComponent source)
    {
        if (!_instances.TryGetValue(entity.Index, out var instance) ||
            instance.IsDisposed ||
            instance.State == SoundState.Stopped)
        {
            instance?.Dispose();
            instance = source.SoundEffect!.CreateInstance();
            instance.IsLooped = source.IsLooping;
            _instances[entity.Index] = instance;
        }

        instance.Volume = source.Volume;
        instance.Pitch  = source.Pitch;
        instance.Pan    = source.Pan;

        if (source.Positional && World.TryGetComponent<TransformComponent>(entity, out var transform))
        {
            _emitter.Position = transform.Position;
            instance.Apply3D(_listener, _emitter);
        }

        if (instance.State != SoundState.Playing)
            instance.Play();
    }

    private void StopInstance(uint entityIndex)
    {
        if (!_instances.TryGetValue(entityIndex, out var instance)) return;
        instance.Stop();
        instance.Dispose();
        _instances.Remove(entityIndex);
    }

    // -------------------------------------------------------------------------
    //  Cleanup
    // -------------------------------------------------------------------------

    private void CleanupStoppedInstances()
    {
        // Remove instances whose entities have been destroyed or that finished playing.
        var toRemove = new List<uint>();
        foreach (var (index, instance) in _instances)
        {
            if (!instance.IsLooped && instance.State == SoundState.Stopped)
                toRemove.Add(index);
        }
        foreach (var index in toRemove)
        {
            _instances[index].Dispose();
            _instances.Remove(index);
        }
    }

    public override void OnShutdown()
    {
        foreach (var instance in _instances.Values)
        {
            instance.Stop();
            instance.Dispose();
        }
        _instances.Clear();
    }

    // -------------------------------------------------------------------------
    //  Public helpers
    // -------------------------------------------------------------------------

    /// <summary>Master volume applied to all sources (0–1).</summary>
    public float MasterVolume
    {
        get => SoundEffect.MasterVolume;
        set => SoundEffect.MasterVolume = Math.Clamp(value, 0f, 1f);
    }
}
