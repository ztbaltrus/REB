using REB.Engine.ECS;

namespace REB.Engine.UI.Components;

/// <summary>
/// Singleton music-state component, attached to an entity tagged "DynamicMusic".
/// <see cref="Systems.DynamicMusicSystem"/> writes to this each frame.
/// </summary>
public struct DynamicMusicComponent : IComponent
{
    /// <summary>Track that is currently audible.</summary>
    public MusicTrack CurrentTrack;

    /// <summary>Track the system wants to switch to (may differ during a crossfade).</summary>
    public MusicTrack TargetTrack;

    /// <summary>Seconds elapsed in the current crossfade (0 when idle).</summary>
    public float TransitionTimer;

    /// <summary>Total seconds for a crossfade between tracks.</summary>
    public float TransitionDuration;

    public static DynamicMusicComponent Default => new()
    {
        CurrentTrack       = MusicTrack.None,
        TargetTrack        = MusicTrack.None,
        TransitionTimer    = 0f,
        TransitionDuration = 2.0f,
    };
}
