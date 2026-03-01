namespace REB.Engine.Hazards;

/// <summary>
/// Lifecycle state of an environmental hazard.
/// Transitions: <see cref="Armed"/> → <see cref="Triggered"/> → <see cref="Resetting"/> → <see cref="Armed"/>.
/// SwingingBlade stays Armed continuously and applies per-frame overlap damage.
/// </summary>
public enum HazardState
{
    /// <summary>Ready to fire. Activates when an entity enters the trigger radius.</summary>
    Armed,

    /// <summary>Currently active (spikes up, blade mid-swing). Deals damage in this state.</summary>
    Triggered,

    /// <summary>Returning to Armed. No damage dealt. Counts down via ResetTimer.</summary>
    Resetting,
}
