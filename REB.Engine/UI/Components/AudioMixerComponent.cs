using REB.Engine.ECS;

namespace REB.Engine.UI.Components;

/// <summary>
/// Global audio mix levels. Attach to an entity tagged "AudioMixer".
/// Read by <see cref="Systems.SpatialAudioSystem"/> and the music system.
/// </summary>
public struct AudioMixerComponent : IComponent
{
    /// <summary>Overall volume multiplier [0, 1].</summary>
    public float MasterVolume;

    /// <summary>Music bus volume multiplier [0, 1].</summary>
    public float MusicVolume;

    /// <summary>Sound-effects bus volume multiplier [0, 1].</summary>
    public float SfxVolume;

    public static AudioMixerComponent Default => new()
    {
        MasterVolume = 1.0f,
        MusicVolume  = 0.7f,
        SfxVolume    = 1.0f,
    };
}
