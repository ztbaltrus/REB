namespace REB.Engine.KingsCourt;

/// <summary>
/// The King's overall mood after reviewing the crew's performance.
/// Computed by <see cref="Systems.KingsCourtSceneSystem"/> from
/// <see cref="Components.RunSummaryComponent"/> data.
/// </summary>
public enum KingReactionState
{
    /// <summary>Crew delivered a healthy princess with excellent loot. King is magnanimous.</summary>
    Pleased,

    /// <summary>Acceptable results. King is grudgingly satisfied.</summary>
    Neutral,

    /// <summary>Poor haul or roughed-up princess. King is visibly displeased.</summary>
    Dissatisfied,

    /// <summary>Princess hurt, undelivered, or almost no loot. The King is theatrical in his rage.</summary>
    Furious,
}
