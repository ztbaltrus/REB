using REB.Engine.ECS;

namespace REB.Engine.KingsCourt.Components;

/// <summary>
/// Itemised breakdown of the run payout, populated by
/// <see cref="Systems.PayoutCalculationSystem"/> and stored on the RunSummary entity.
/// Displayed in the payout UI (Epic 9).
/// </summary>
public struct PayoutBreakdownComponent : IComponent
{
    // ── Inputs ─────────────────────────────────────────────────────────────────

    /// <summary>LootGoldValue + LootItemCount × 10 (finder's fee per item).</summary>
    public float BasePayout;

    // ── Modifiers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Princess health modifier. Positive when health &gt; 50, negative when &lt; 50.
    /// Formula: (health − 50) / 50 × BasePayout × 0.2.
    /// </summary>
    public float PrincessHealthBonus;

    /// <summary>
    /// Princess goodwill bonus (always ≥ 0).
    /// Formula: goodwill / 100 × BasePayout × 0.1.
    /// </summary>
    public float PrincessGoodwillBonus;

    /// <summary>
    /// Drop penalty (always ≤ 0).
    /// Formula: −min(dropCount × 0.1, 0.5) × BasePayout.
    /// </summary>
    public float DropPenalty;

    /// <summary>
    /// Penalty for not delivering the princess (0 if delivered safely, −90 % otherwise).
    /// </summary>
    public float DeliveryPenalty;

    /// <summary>Boss victory bonus (+25 % of BasePayout, or 0).</summary>
    public float BossBonus;

    /// <summary>Cumulative negotiation modifier (BasePayout × DispositionModifierPercent / 100).</summary>
    public float NegotiationModifier;

    /// <summary>Relationship tier bonus/penalty (BasePayout × tier multiplier).</summary>
    public float RelationshipModifier;

    // ── Result ─────────────────────────────────────────────────────────────────

    /// <summary>Sum of all fields above, clamped to [0, ∞). This is what the crew receives.</summary>
    public float FinalPayout;

    /// <summary>True once PayoutCalculationSystem has filled this breakdown.</summary>
    public bool IsCalculated;
}
