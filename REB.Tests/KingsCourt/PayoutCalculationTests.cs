using REB.Engine.ECS;
using REB.Engine.KingsCourt;
using REB.Engine.KingsCourt.Components;
using REB.Engine.KingsCourt.Systems;
using Xunit;

namespace REB.Tests.KingsCourt;

// ---------------------------------------------------------------------------
//  PayoutCalculationSystem tests
//
//  NegotiationMinigameSystem is not registered; RunAfter is silently ignored.
//  Tests set KingStateComponent.Phase = Payout directly to trigger calculation.
// ---------------------------------------------------------------------------

public sealed class PayoutCalculationTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static (World world, PayoutCalculationSystem payoutCalc) BuildWorld()
    {
        var world      = new World();
        var payoutCalc = new PayoutCalculationSystem();
        world.RegisterSystem(payoutCalc);
        return (world, payoutCalc);
    }

    /// <summary>Creates a King entity in the Payout phase.</summary>
    private static Entity AddKingInPayout(World world,
        KingReactionState reaction = KingReactionState.Neutral)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "King");

        var ks = KingStateComponent.Default;
        ks.Phase         = KingsCourtPhase.Payout;
        ks.ReactionState = reaction;
        world.AddComponent(e, ks);
        return e;
    }

    private static Entity AddRunSummary(World world,
        float loot       = 500f,
        int   items      = 10,
        float health     = 100f,
        float goodwill   = 100f,
        bool  delivered  = true,
        int   drops      = 0,
        bool  boss       = false)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "RunSummary");
        world.AddComponent(e, new RunSummaryComponent
        {
            LootGoldValue           = loot,
            LootItemCount           = items,
            PrincessHealth          = health,
            PrincessGoodwill        = goodwill,
            PrincessDeliveredSafely = delivered,
            PrincessDropCount       = drops,
            BossDefeated            = boss,
            IsComplete              = true,
        });
        return e;
    }

    private static PayoutBreakdownComponent GetBreakdown(World world, Entity summary) =>
        world.GetComponent<PayoutBreakdownComponent>(summary);

    // -------------------------------------------------------------------------
    //  Trigger guard
    // -------------------------------------------------------------------------

    [Fact]
    public void Payout_DoesNotCalculate_WhenPhaseIsNotPayout()
    {
        var (world, payoutCalc) = BuildWorld();
        var king = world.CreateEntity();
        world.AddTag(king, "King");
        world.AddComponent(king, KingStateComponent.Default);  // Inactive phase
        AddRunSummary(world);

        world.Update(0.016f);

        Assert.Empty(payoutCalc.PayoutEvents);
        world.Dispose();
    }

    [Fact]
    public void Payout_DoesNotCalculate_WhenPayoutAlreadyCalculated()
    {
        var (world, payoutCalc) = BuildWorld();
        var king = AddKingInPayout(world);
        var summary = AddRunSummary(world);
        world.Update(0.016f);  // first calculation

        Assert.Single(payoutCalc.PayoutEvents);

        // Next frame: PayoutCalculated=true, events cleared, no repeat.
        world.Update(0.016f);
        Assert.Empty(payoutCalc.PayoutEvents);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  BasePayout
    // -------------------------------------------------------------------------

    [Fact]
    public void BasePayout_IsLootGoldValue_Plus_ItemCount_Times_Ten()
    {
        var (world, _) = BuildWorld();
        AddKingInPayout(world);
        var summary = AddRunSummary(world,
            loot: 400f, items: 5,
            health: 50f, goodwill: 0f,
            delivered: true, drops: 0, boss: false);

        world.Update(0.016f);

        // BasePayout = 400 + 5×10 = 450. Health=50 → bonus=0. Goodwill=0 → bonus=0.
        var bd = GetBreakdown(world, summary);
        Assert.Equal(450f, bd.BasePayout);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Princess health bonus
    // -------------------------------------------------------------------------

    [Fact]
    public void HealthBonus_IsPositive_WhenHealthAboveFifty()
    {
        var (world, _) = BuildWorld();
        AddKingInPayout(world);
        // loot=500, items=0 → base=500; health=100 → bonus = (100−50)/50 × 500 × 0.2 = +100
        var summary = AddRunSummary(world,
            loot: 500f, items: 0, health: 100f, goodwill: 0f,
            delivered: true, drops: 0, boss: false);

        world.Update(0.016f);

        var bd = GetBreakdown(world, summary);
        Assert.Equal(100f, bd.PrincessHealthBonus, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void HealthBonus_IsNegative_WhenHealthBelowFifty()
    {
        var (world, _) = BuildWorld();
        AddKingInPayout(world);
        // health=0 → bonus = (0−50)/50 × 500 × 0.2 = −100
        var summary = AddRunSummary(world,
            loot: 500f, items: 0, health: 0f, goodwill: 0f,
            delivered: true, drops: 0, boss: false);

        world.Update(0.016f);

        var bd = GetBreakdown(world, summary);
        Assert.Equal(-100f, bd.PrincessHealthBonus, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void HealthBonus_IsZero_WhenHealthIsExactlyFifty()
    {
        var (world, _) = BuildWorld();
        AddKingInPayout(world);
        var summary = AddRunSummary(world,
            loot: 500f, items: 0, health: 50f, goodwill: 0f,
            delivered: true, drops: 0, boss: false);

        world.Update(0.016f);

        Assert.Equal(0f, GetBreakdown(world, summary).PrincessHealthBonus, precision: 3);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Goodwill bonus
    // -------------------------------------------------------------------------

    [Fact]
    public void GoodwillBonus_IsMaxTenPercent_WhenGoodwillIsHundred()
    {
        var (world, _) = BuildWorld();
        AddKingInPayout(world);
        // base=500, goodwill=100 → 100/100 × 500 × 0.1 = +50
        var summary = AddRunSummary(world,
            loot: 500f, items: 0, health: 50f, goodwill: 100f,
            delivered: true, drops: 0, boss: false);

        world.Update(0.016f);

        Assert.Equal(50f, GetBreakdown(world, summary).PrincessGoodwillBonus, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void GoodwillBonus_IsZero_WhenGoodwillIsZero()
    {
        var (world, _) = BuildWorld();
        AddKingInPayout(world);
        var summary = AddRunSummary(world,
            loot: 500f, items: 0, health: 50f, goodwill: 0f,
            delivered: true, drops: 0, boss: false);

        world.Update(0.016f);

        Assert.Equal(0f, GetBreakdown(world, summary).PrincessGoodwillBonus, precision: 3);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Drop penalty
    // -------------------------------------------------------------------------

    [Fact]
    public void DropPenalty_IsTenPercentPerDrop()
    {
        var (world, _) = BuildWorld();
        AddKingInPayout(world);
        // base=500, drops=2 → −2×0.1 × 500 = −100
        var summary = AddRunSummary(world,
            loot: 500f, items: 0, health: 50f, goodwill: 0f,
            delivered: true, drops: 2, boss: false);

        world.Update(0.016f);

        Assert.Equal(-100f, GetBreakdown(world, summary).DropPenalty, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void DropPenalty_CappedAtFiftyPercent()
    {
        var (world, _) = BuildWorld();
        AddKingInPayout(world);
        // base=500, drops=10 → min(1.0,0.5) × 500 = −250
        var summary = AddRunSummary(world,
            loot: 500f, items: 0, health: 50f, goodwill: 0f,
            delivered: true, drops: 10, boss: false);

        world.Update(0.016f);

        Assert.Equal(-250f, GetBreakdown(world, summary).DropPenalty, precision: 3);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Delivery penalty
    // -------------------------------------------------------------------------

    [Fact]
    public void DeliveryPenalty_IsNintyPercentOfBase_WhenNotDelivered()
    {
        var (world, _) = BuildWorld();
        AddKingInPayout(world);
        // base=500, not delivered → −0.9×500 = −450
        var summary = AddRunSummary(world,
            loot: 500f, items: 0, health: 50f, goodwill: 0f,
            delivered: false, drops: 0, boss: false);

        world.Update(0.016f);

        Assert.Equal(-450f, GetBreakdown(world, summary).DeliveryPenalty, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void DeliveryPenalty_IsZero_WhenDelivered()
    {
        var (world, _) = BuildWorld();
        AddKingInPayout(world);
        var summary = AddRunSummary(world, delivered: true);

        world.Update(0.016f);

        Assert.Equal(0f, GetBreakdown(world, summary).DeliveryPenalty, precision: 3);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Boss bonus
    // -------------------------------------------------------------------------

    [Fact]
    public void BossBonus_IsTwentyFivePercent_WhenBossDefeated()
    {
        var (world, _) = BuildWorld();
        AddKingInPayout(world);
        // base=500, boss=true → +0.25×500 = +125
        var summary = AddRunSummary(world,
            loot: 500f, items: 0, health: 50f, goodwill: 0f,
            delivered: true, drops: 0, boss: true);

        world.Update(0.016f);

        Assert.Equal(125f, GetBreakdown(world, summary).BossBonus, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void BossBonus_IsZero_WhenBossNotDefeated()
    {
        var (world, _) = BuildWorld();
        AddKingInPayout(world);
        var summary = AddRunSummary(world, boss: false);

        world.Update(0.016f);

        Assert.Equal(0f, GetBreakdown(world, summary).BossBonus, precision: 3);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Negotiation modifier
    // -------------------------------------------------------------------------

    [Fact]
    public void NegotiationModifier_UsesKingDispositionPercent()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInPayout(world);

        // Add a disposition of +20 %.
        var disp = KingDispositionComponent.Default;
        disp.DispositionModifierPercent = 20f;
        world.AddComponent(king, disp);

        // base=500, disposition=20 → negotiation = 500×20/100 = +100
        var summary = AddRunSummary(world,
            loot: 500f, items: 0, health: 50f, goodwill: 0f,
            delivered: true, drops: 0, boss: false);

        world.Update(0.016f);

        Assert.Equal(100f, GetBreakdown(world, summary).NegotiationModifier, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void NegotiationModifier_IsZero_WhenNoDispositionComponent()
    {
        // PayoutCalculationSystem treats absent KingDispositionComponent as 0 %.
        var (world, _) = BuildWorld();
        AddKingInPayout(world);
        var summary = AddRunSummary(world,
            loot: 500f, items: 0, health: 50f, goodwill: 0f,
            delivered: true, drops: 0, boss: false);

        world.Update(0.016f);

        Assert.Equal(0f, GetBreakdown(world, summary).NegotiationModifier, precision: 3);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Relationship modifier
    // -------------------------------------------------------------------------

    [Fact]
    public void RelationshipModifier_UsesTorBonusPercent()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInPayout(world);

        // Beloved tier → +20 %
        var rel = KingRelationshipComponent.Default;
        rel.Score = 90f;
        rel.Tier  = KingRelationshipTier.Beloved;
        world.AddComponent(king, rel);

        // base=500, tier bonus=+20 → 500×20/100 = +100
        var summary = AddRunSummary(world,
            loot: 500f, items: 0, health: 50f, goodwill: 0f,
            delivered: true, drops: 0, boss: false);

        world.Update(0.016f);

        Assert.Equal(100f, GetBreakdown(world, summary).RelationshipModifier, precision: 3);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  FinalPayout floor
    // -------------------------------------------------------------------------

    [Fact]
    public void FinalPayout_IsNeverNegative()
    {
        var (world, _) = BuildWorld();
        AddKingInPayout(world);
        // Catastrophic run: low loot, not delivered, 10 drops.
        var summary = AddRunSummary(world,
            loot: 10f, items: 0, health: 0f, goodwill: 0f,
            delivered: false, drops: 10, boss: false);

        world.Update(0.016f);

        Assert.True(GetBreakdown(world, summary).FinalPayout >= 0f);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  PayoutEvent published
    // -------------------------------------------------------------------------

    [Fact]
    public void PayoutEvent_Published_OnCalculation()
    {
        var (world, payoutCalc) = BuildWorld();
        AddKingInPayout(world, KingReactionState.Pleased);
        AddRunSummary(world, loot: 300f, items: 5, health: 100f,
            goodwill: 80f, delivered: true, drops: 0, boss: true);

        world.Update(0.016f);

        Assert.Single(payoutCalc.PayoutEvents);
        Assert.Equal(KingReactionState.Pleased, payoutCalc.PayoutEvents[0].KingReaction);
        world.Dispose();
    }

    [Fact]
    public void PayoutEvent_FinalPayout_MatchesBreakdown()
    {
        var (world, payoutCalc) = BuildWorld();
        AddKingInPayout(world);
        var summary = AddRunSummary(world,
            loot: 200f, items: 5, health: 80f, goodwill: 60f,
            delivered: true, drops: 1, boss: false);

        world.Update(0.016f);

        var breakdown = GetBreakdown(world, summary);
        Assert.Equal(breakdown.FinalPayout, payoutCalc.PayoutEvents[0].FinalPayout, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void PayoutEvents_ClearedEachFrame()
    {
        var (world, payoutCalc) = BuildWorld();
        AddKingInPayout(world);
        AddRunSummary(world);

        world.Update(0.016f);
        Assert.Single(payoutCalc.PayoutEvents);

        world.Update(0.016f);  // PayoutCalculated=true, no new event; list cleared.
        Assert.Empty(payoutCalc.PayoutEvents);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  IsCalculated flag
    // -------------------------------------------------------------------------

    [Fact]
    public void Breakdown_IsCalculatedFlag_SetAfterUpdate()
    {
        var (world, _) = BuildWorld();
        AddKingInPayout(world);
        var summary = AddRunSummary(world);

        world.Update(0.016f);

        Assert.True(GetBreakdown(world, summary).IsCalculated);
        world.Dispose();
    }
}
