using REB.Engine.ECS;

namespace REB.Engine.Player.Components;

/// <summary>
/// Tracks the currently playing animation clip for a character entity.
/// Updated by <see cref="REB.Engine.Player.Systems.AnimationSystem"/>.
/// Actual skeletal/sprite playback is deferred to the rendering layer (Epic 4+).
/// </summary>
public struct AnimationComponent : IComponent
{
    /// <summary>Name of the currently active clip, e.g. "Idle", "Walk", "CarryWalk".</summary>
    public string CurrentClip;

    /// <summary>Seconds elapsed within the current clip.</summary>
    public float ElapsedTime;

    /// <summary>Playback rate multiplier (1.0 = normal speed).</summary>
    public float PlaybackSpeed;

    /// <summary>Whether the clip loops when it reaches <see cref="ClipDuration"/>.</summary>
    public bool IsLooping;

    /// <summary>Length of the clip in seconds. 0 means unbounded / no auto-reset.</summary>
    public float ClipDuration;

    public static AnimationComponent Default => new()
    {
        CurrentClip   = "Idle",
        ElapsedTime   = 0f,
        PlaybackSpeed = 1f,
        IsLooping     = true,
        ClipDuration  = 0f,
    };
}
