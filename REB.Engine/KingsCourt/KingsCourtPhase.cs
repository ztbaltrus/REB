namespace REB.Engine.KingsCourt;

/// <summary>
/// Lifecycle phases of the King's Court end-of-run scene.
/// Driven by <see cref="Systems.KingsCourtSceneSystem"/>.
/// </summary>
public enum KingsCourtPhase
{
    /// <summary>Court scene has not started. Waiting for the run to complete.</summary>
    Inactive,

    /// <summary>The King makes his entrance. Crew is lined up. ~3 seconds.</summary>
    Arriving,

    /// <summary>King reviews the loot and princess condition. Dialogue fires here. ~5 seconds.</summary>
    Review,

    /// <summary>Negotiator window — players can intervene to adjust the payout. ~30 seconds.</summary>
    Negotiation,

    /// <summary>King tallies the final payout. PayoutCalculationSystem triggers here. ~3 seconds.</summary>
    Payout,

    /// <summary>Crew is dismissed. Scene ends and SceneActive is cleared.</summary>
    Dismissed,
}
