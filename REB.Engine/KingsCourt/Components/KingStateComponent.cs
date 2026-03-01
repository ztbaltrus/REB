using REB.Engine.ECS;

namespace REB.Engine.KingsCourt.Components;

/// <summary>
/// Tracks the King entity's current scene phase, reaction state, and phase timers.
/// Attach to the entity tagged <c>"King"</c>.
/// Driven by <see cref="Systems.KingsCourtSceneSystem"/>.
/// </summary>
public struct KingStateComponent : IComponent
{
    /// <summary>Current court scene phase.</summary>
    public KingsCourtPhase Phase;

    /// <summary>King's computed reaction to the crew's run performance.</summary>
    public KingReactionState ReactionState;

    /// <summary>Seconds elapsed within the current phase.</summary>
    public float PhaseTimer;

    /// <summary>True while a court scene is in progress.</summary>
    public bool SceneActive;

    /// <summary>
    /// True once <see cref="Systems.PayoutCalculationSystem"/> has fired for this run.
    /// Prevents double-calculation if the Payout phase persists multiple frames.
    /// </summary>
    public bool PayoutCalculated;

    // ── Phase durations (configurable so tests can run short scenes) ───────────

    /// <summary>Seconds to spend in the Arriving phase.</summary>
    public float ArrivingDuration;

    /// <summary>Seconds to spend in the Review phase.</summary>
    public float ReviewDuration;

    /// <summary>Seconds to spend in the Negotiation phase (player window).</summary>
    public float NegotiationDuration;

    /// <summary>Seconds to spend in the Payout phase.</summary>
    public float PayoutDuration;

    public static KingStateComponent Default => new()
    {
        Phase              = KingsCourtPhase.Inactive,
        ReactionState      = KingReactionState.Neutral,
        PhaseTimer         = 0f,
        SceneActive        = false,
        PayoutCalculated   = false,
        ArrivingDuration   = 3f,
        ReviewDuration     = 5f,
        NegotiationDuration = 30f,
        PayoutDuration     = 3f,
    };
}
