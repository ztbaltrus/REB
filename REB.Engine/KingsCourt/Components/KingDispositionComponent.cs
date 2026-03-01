using REB.Engine.ECS;

namespace REB.Engine.KingsCourt.Components;

/// <summary>
/// Tracks the cumulative disposition modifier (in percentage points) that the
/// Negotiator has earned or lost during the Negotiation phase.
/// Applied by <see cref="Systems.PayoutCalculationSystem"/> on top of the base payout.
/// </summary>
public struct KingDispositionComponent : IComponent
{
    /// <summary>
    /// Cumulative disposition modifier in percentage points.
    /// Positive = King pays more; negative = King pays less.
    /// Clamped to [−30, +30] to prevent degenerate outcomes.
    /// </summary>
    public float DispositionModifierPercent;

    /// <summary>True if the crew has already bribed the King's advisor this run.</summary>
    public bool HasBribedAdvisor;

    /// <summary>Gold cost of a bribe (enforced by GoldCurrencySystem in Epic 8).</summary>
    public int BribeCost;

    public static KingDispositionComponent Default => new()
    {
        DispositionModifierPercent = 0f,
        HasBribedAdvisor           = false,
        BribeCost                  = 50,
    };
}
