using REB.Engine.ECS;

namespace REB.Engine.UI.Components;

/// <summary>
/// Post-run results shown on the Run Summary screen.
/// Populated by <see cref="Systems.RunSummaryUISystem"/> from
/// <c>PayoutCalculationSystem.PayoutEvents</c> at end-of-run.
/// </summary>
public struct RunSummaryUIComponent : IComponent
{
    /// <summary>Final gold payout awarded to the party.</summary>
    public float FinalPayout;

    /// <summary>Effective loot value delivered to the King.</summary>
    public int TreasureValue;

    /// <summary>Total run duration in seconds for the score screen.</summary>
    public float RunDurationSeconds;

    /// <summary>Human-readable label for the King's reaction (e.g. "Pleased", "Furious").</summary>
    public string KingReactionLabel;

    public static RunSummaryUIComponent Default => new() { KingReactionLabel = string.Empty };
}
