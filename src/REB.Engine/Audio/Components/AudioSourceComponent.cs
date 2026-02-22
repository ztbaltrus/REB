using Microsoft.Xna.Framework.Audio;
using REB.Engine.ECS;

namespace REB.Engine.Audio.Components;

/// <summary>
/// Attaches a sound source to an entity. The <see cref="Systems.AudioSystem"/> manages the
/// underlying <see cref="SoundEffectInstance"/> and updates its 3D position each frame.
/// </summary>
public struct AudioSourceComponent : IComponent
{
    /// <summary>The sound asset to play. Assign via ContentManager before setting <see cref="Play"/>.</summary>
    public SoundEffect? SoundEffect;

    /// <summary>Volume multiplier [0, 1].</summary>
    public float Volume;

    /// <summary>Pitch shift [-1, 1]. 0 = no shift.</summary>
    public float Pitch;

    /// <summary>Pan [-1 = left, 0 = center, 1 = right]. Ignored when 3D audio is active.</summary>
    public float Pan;

    /// <summary>When true the system loops the sound effect.</summary>
    public bool IsLooping;

    /// <summary>
    /// Set to true to trigger playback. The system clears this once playback has started.
    /// Set to false to request a stop.
    /// </summary>
    public bool Play;

    /// <summary>When true the system applies 3D positional audio using the entity's Transform.</summary>
    public bool Positional;

    public static AudioSourceComponent Default => new()
    {
        Volume     = 1f,
        Pitch      = 0f,
        Pan        = 0f,
        IsLooping  = false,
        Play       = false,
        Positional = false,
    };
}
