using REB.Engine.ECS;
using REB.Engine.KingsCourt.Components;

namespace REB.Engine.KingsCourt.Systems;

/// <summary>
/// Calculates the run payout when the King's Court enters the
/// <see cref="KingsCourtPhase.Payout"/> phase.
/// <para>Payout formula (all modifiers are fractions of BasePayout):</para>
/// <list type="bullet">
///   <item>BasePayout = LootGoldValue + LootItemCount × 10 (finder's fee)</item>
///   <item>Princess health: ±20 % (linear around 50 % health)</item>
///   <item>Princess goodwill: 0–10 % bonus</item>
///   <item>Drop penalty: −10 % per drop, capped at −50 %</item>
///   <item>Delivery penalty: −90 % if princess not delivered</item>
///   <item>Boss bonus: +25 %</item>
///   <item>Negotiation modifier: variable (set by NegotiationMinigameSystem)</item>
///   <item>Relationship modifier: tier-based bonus/penalty</item>
/// </list>
/// FinalPayout is clamped to [0, ∞) — the King pays nothing at worst.
/// </summary>
[RunAfter(typeof(NegotiationMinigameSystem))]
public sealed class PayoutCalculationSystem : GameSystem
{
    /// <summary>Payout events fired this frame. Cleared at the start of each update.</summary>
    public IReadOnlyList<PayoutEvent> PayoutEvents => _events;

    private readonly List<PayoutEvent> _events = new();

    public override void Update(float deltaTime)
    {
        _events.Clear();

        Entity king = FindKing();
        if (!World.IsAlive(king)) return;

        ref var ks = ref World.GetComponent<KingStateComponent>(king);
        if (ks.Phase != KingsCourtPhase.Payout || ks.PayoutCalculated) return;

        Entity summary = FindRunSummary();
        if (!World.IsAlive(summary)) return;

        var rs = World.GetComponent<RunSummaryComponent>(summary);

        // Fetch optional modifiers (gracefully absent in tests).
        float dispositionPercent = World.HasComponent<KingDispositionComponent>(king)
            ? World.GetComponent<KingDispositionComponent>(king).DispositionModifierPercent
            : 0f;

        float relationshipPercent = World.HasComponent<KingRelationshipComponent>(king)
            ? World.GetComponent<KingRelationshipComponent>(king).TierBonusPercent
            : 0f;

        // ── Compute breakdown ─────────────────────────────────────────────────
        var breakdown = new PayoutBreakdownComponent();

        breakdown.BasePayout = rs.LootGoldValue + rs.LootItemCount * 10f;
        float b = breakdown.BasePayout;

        // Health modifier: (health−50)/50 × b × 0.2  → range [−0.2b, +0.2b]
        breakdown.PrincessHealthBonus =
            (rs.PrincessHealth - 50f) / 50f * b * 0.2f;

        // Goodwill modifier: 0 to +0.1b
        breakdown.PrincessGoodwillBonus =
            rs.PrincessGoodwill / 100f * b * 0.1f;

        // Drop penalty: negative
        breakdown.DropPenalty =
            -MathF.Min(rs.PrincessDropCount * 0.1f, 0.5f) * b;

        // Delivery penalty: 0 if delivered, −0.9b otherwise
        breakdown.DeliveryPenalty =
            rs.PrincessDeliveredSafely ? 0f : -b * 0.9f;

        // Boss bonus
        breakdown.BossBonus =
            rs.BossDefeated ? b * 0.25f : 0f;

        // Negotiation modifier
        breakdown.NegotiationModifier = b * dispositionPercent / 100f;

        // Relationship modifier
        breakdown.RelationshipModifier = b * relationshipPercent / 100f;

        breakdown.FinalPayout = MathF.Max(0f,
            b
            + breakdown.PrincessHealthBonus
            + breakdown.PrincessGoodwillBonus
            + breakdown.DropPenalty
            + breakdown.DeliveryPenalty
            + breakdown.BossBonus
            + breakdown.NegotiationModifier
            + breakdown.RelationshipModifier);

        breakdown.IsCalculated = true;

        // Write breakdown back to RunSummary entity.
        World.SetComponent(summary, breakdown);

        // Mark King payout complete to avoid re-calculation.
        ks.PayoutCalculated = true;

        // Determine King's reaction for the event (from KingStateComponent).
        _events.Add(new PayoutEvent(breakdown.FinalPayout, ks.ReactionState));
    }

    // =========================================================================
    //  Helpers
    // =========================================================================

    private Entity FindKing()
    {
        foreach (var e in World.GetEntitiesWithTag("King"))
            return e;
        return Entity.Null;
    }

    private Entity FindRunSummary()
    {
        foreach (var e in World.GetEntitiesWithTag("RunSummary"))
            return e;
        return Entity.Null;
    }
}
