using REB.Engine.ECS;

namespace REB.Engine.Tavern.Components;

/// <summary>
/// Manages the Tavern scene lifecycle on the singleton entity tagged <c>"Tavern"</c>.
/// <see cref="Systems.TavernSceneSystem"/> drives transitions:
/// <list type="bullet">
///   <item>Inactive → Open when <c>KingsCourtPhase.Dismissed</c> is detected.</item>
///   <item>Open → Inactive after <see cref="OpenDuration"/> elapses.</item>
/// </list>
/// </summary>
public struct TavernStateComponent : IComponent
{
    /// <summary>Current phase of the tavern scene.</summary>
    public TavernPhase Phase;

    /// <summary>Whether the tavern scene is currently running.</summary>
    public bool SceneActive;

    /// <summary>Seconds spent in the current phase.</summary>
    public float PhaseTimer;

    /// <summary>How long the tavern stays open before auto-closing (default 60 s).</summary>
    public float OpenDuration;

    public static TavernStateComponent Default => new()
    {
        Phase        = TavernPhase.Inactive,
        SceneActive  = false,
        PhaseTimer   = 0f,
        OpenDuration = 60f,
    };
}
