using REB.Engine.ECS;
using REB.Engine.KingsCourt.Components;

namespace REB.Engine.KingsCourt.Systems;

/// <summary>
/// Updates the King's persistent relationship score after each run payout.
/// Watches <see cref="PayoutCalculationSystem.PayoutEvents"/> and on each event:
/// <list type="number">
///   <item>Appends a <see cref="RunHistoryEntry"/> to the ring buffer.</item>
///   <item>Adjusts <see cref="KingRelationshipComponent.Score"/> based on King reaction.</item>
///   <item>Re-derives <see cref="KingRelationshipComponent.Tier"/> from the new score.</item>
/// </list>
/// Score change per run: Pleased +10, Neutral +5, Dissatisfied −5, Furious −10.
/// Score is clamped to [0, 100]; Tier thresholds: 80/60/40/20.
/// </summary>
[RunAfter(typeof(PayoutCalculationSystem))]
public sealed class KingRelationshipSystem : GameSystem
{
    public override void Update(float deltaTime)
    {
        if (!World.TryGetSystem<PayoutCalculationSystem>(out var payoutCalc)) return;
        if (payoutCalc!.PayoutEvents.Count == 0) return;

        Entity king = FindKing();
        if (!World.IsAlive(king) || !World.HasComponent<KingRelationshipComponent>(king)) return;

        ref var rel = ref World.GetComponent<KingRelationshipComponent>(king);

        Entity summary = FindRunSummary();
        var rs = World.IsAlive(summary)
            ? World.GetComponent<RunSummaryComponent>(summary)
            : RunSummaryComponent.Empty;

        foreach (var evt in payoutCalc.PayoutEvents)
        {
            var entry = new RunHistoryEntry(
                rs.LootGoldValue,
                rs.PrincessHealth,
                rs.PrincessDeliveredSafely,
                rs.BossDefeated,
                evt.FinalPayout);

            rel.AddRun(entry);

            float scoreDelta = evt.KingReaction switch
            {
                KingReactionState.Pleased      =>  10f,
                KingReactionState.Neutral      =>   5f,
                KingReactionState.Dissatisfied =>  -5f,
                KingReactionState.Furious      => -10f,
                _                              =>   0f,
            };

            rel.Score = Math.Clamp(rel.Score + scoreDelta, 0f, 100f);
            rel.Tier  = DeriveRelationshipTier(rel.Score);
        }
    }

    // =========================================================================
    //  Tier derivation
    // =========================================================================

    private static KingRelationshipTier DeriveRelationshipTier(float score) => score switch
    {
        >= 80f => KingRelationshipTier.Beloved,
        >= 60f => KingRelationshipTier.Respected,
        >= 40f => KingRelationshipTier.Known,
        >= 20f => KingRelationshipTier.Suspected,
        _      => KingRelationshipTier.Despised,
    };

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
