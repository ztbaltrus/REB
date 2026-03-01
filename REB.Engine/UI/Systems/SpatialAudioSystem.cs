using REB.Engine.ECS;
using REB.Engine.Rendering.Components;
using REB.Engine.UI.Components;

namespace REB.Engine.UI.Systems;

/// <summary>
/// Applies distance-based volume attenuation to entities that act as spatial audio sources.
/// <para>
/// Entities with both <see cref="AudioMixerComponent"/> and <see cref="TransformComponent"/>
/// are treated as audio sources. The listener position is taken from the entity tagged
/// "MainCamera"; if no camera exists, the first "Player" entity is used instead.
/// </para>
/// <para>
/// Attenuation model: full volume within <see cref="MinRange"/> units; linear falloff to
/// silence at <see cref="MaxRange"/> units.
/// </para>
/// </summary>
public sealed class SpatialAudioSystem : GameSystem
{
    /// <summary>Distance within which sounds are heard at full volume.</summary>
    public const float MinRange = 5f;

    /// <summary>Distance beyond which sounds are completely silent.</summary>
    public const float MaxRange = 50f;

    /// <summary>Volume fraction [0, 1] computed for the most recently processed source this frame.</summary>
    public float LastComputedVolume { get; private set; }

    public override void Update(float deltaTime)
    {
        var listenerPos = FindListenerPosition();

        foreach (var e in World.Query<AudioMixerComponent, TransformComponent>())
        {
            var   srcPos  = World.GetComponent<TransformComponent>(e).Position;
            float dist    = Microsoft.Xna.Framework.Vector3.Distance(listenerPos, srcPos);
            float volume  = ComputeVolumeFraction(dist);

            LastComputedVolume = volume;

            ref var mixer = ref World.GetComponent<AudioMixerComponent>(e);
            mixer.SfxVolume = volume;
        }
    }

    // =========================================================================
    //  Public helpers (used directly by tests)
    // =========================================================================

    /// <summary>
    /// Returns a volume fraction [0, 1] for a given <paramref name="distance"/>
    /// using linear falloff between <see cref="MinRange"/> and <see cref="MaxRange"/>.
    /// </summary>
    public static float ComputeVolumeFraction(float distance)
    {
        if (distance <= MinRange) return 1f;
        if (distance >= MaxRange) return 0f;
        return 1f - (distance - MinRange) / (MaxRange - MinRange);
    }

    // =========================================================================
    //  Private helpers
    // =========================================================================

    private Microsoft.Xna.Framework.Vector3 FindListenerPosition()
    {
        foreach (var e in World.GetEntitiesWithTag("MainCamera"))
        {
            if (World.HasComponent<TransformComponent>(e))
                return World.GetComponent<TransformComponent>(e).Position;
        }

        foreach (var e in World.GetEntitiesWithTag("Player"))
        {
            if (World.HasComponent<TransformComponent>(e))
                return World.GetComponent<TransformComponent>(e).Position;
        }

        return Microsoft.Xna.Framework.Vector3.Zero;
    }
}
