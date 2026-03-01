using REB.Engine.ECS;

namespace REB.Engine.Hazards.Components;

/// <summary>
/// Describes an environmental hazard tile or entity.
/// State machine is driven by <see cref="Systems.TrapTriggerSystem"/>.
/// </summary>
public struct HazardComponent : IComponent
{
    /// <summary>Category of hazard (determines trigger shape and animation).</summary>
    public HazardType Type;

    /// <summary>Current lifecycle state.</summary>
    public HazardState State;

    /// <summary>Damage applied when the hazard fires. Princess receives double.</summary>
    public float Damage;

    /// <summary>
    /// Radius (world units) checked for overlapping players or the princess.
    /// Not used by SwingingBlade (uses OscillationHalfWidth instead).
    /// </summary>
    public float TriggerRadius;

    /// <summary>Seconds the hazard stays in the Triggered state before resetting.</summary>
    public float TriggeredDuration;

    /// <summary>Countdown within Triggered state. Transitions to Resetting at zero.</summary>
    public float TriggeredTimer;

    /// <summary>Seconds to wait in Resetting before returning to Armed.</summary>
    public float ResetTime;

    /// <summary>Countdown within Resetting state. Transitions to Armed at zero.</summary>
    public float ResetTimer;

    // ── SwingingBlade-specific ─────────────────────────────────────────────────

    /// <summary>Period (seconds) of one full left-right oscillation cycle.</summary>
    public float OscillationPeriod;

    /// <summary>Current oscillation phase in radians. Advanced each frame by TrapTriggerSystem.</summary>
    public float OscillationPhase;

    /// <summary>Half-width (world units) of the blade sweep zone on the X axis.</summary>
    public float OscillationHalfWidth;

    // ── Preset factories ───────────────────────────────────────────────────────

    public static HazardComponent SpikeTrap => new()
    {
        Type                 = HazardType.SpikeTrap,
        State                = HazardState.Armed,
        Damage               = 20f,
        TriggerRadius        = 0.6f,
        TriggeredDuration    = 0.5f,
        TriggeredTimer       = 0f,
        ResetTime            = 3f,
        ResetTimer           = 0f,
        OscillationPeriod    = 0f,
        OscillationPhase     = 0f,
        OscillationHalfWidth = 0f,
    };

    public static HazardComponent Pit => new()
    {
        Type                 = HazardType.Pit,
        State                = HazardState.Armed,
        Damage               = 30f,
        TriggerRadius        = 1.0f,
        TriggeredDuration    = 0.1f,
        TriggeredTimer       = 0f,
        ResetTime            = 0f,   // pits do not reset
        ResetTimer           = 0f,
        OscillationPeriod    = 0f,
        OscillationPhase     = 0f,
        OscillationHalfWidth = 0f,
    };

    public static HazardComponent SwingingBlade => new()
    {
        Type                 = HazardType.SwingingBlade,
        State                = HazardState.Armed,
        Damage               = 25f,
        TriggerRadius        = 0f,
        TriggeredDuration    = 0f,
        TriggeredTimer       = 0f,
        ResetTime            = 0f,   // always active
        ResetTimer           = 0f,
        OscillationPeriod    = 2f,
        OscillationPhase     = 0f,
        OscillationHalfWidth = 1.5f,
    };
}
