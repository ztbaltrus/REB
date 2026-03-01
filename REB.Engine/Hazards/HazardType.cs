namespace REB.Engine.Hazards;

/// <summary>
/// The category of an environmental hazard entity.
/// Determines trigger shape, animation, and damage timing in
/// <see cref="Systems.TrapTriggerSystem"/>.
/// </summary>
public enum HazardType
{
    /// <summary>Floor spikes that burst upward when stepped on.</summary>
    SpikeTrap,

    /// <summary>Open pit that deals instant damage and pins the victim below.</summary>
    Pit,

    /// <summary>Pendulum blade that sweeps back and forth on a fixed period.</summary>
    SwingingBlade,
}
