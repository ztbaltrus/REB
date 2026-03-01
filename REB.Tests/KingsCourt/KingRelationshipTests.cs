using REB.Engine.ECS;
using REB.Engine.KingsCourt;
using REB.Engine.KingsCourt.Components;
using REB.Engine.KingsCourt.Systems;
using Xunit;

namespace REB.Tests.KingsCourt;

// ---------------------------------------------------------------------------
//  KingRelationshipSystem tests
//
//  PayoutCalculationSystem is registered alongside KingRelationshipSystem so
//  its PayoutEvents list is populated by PayoutCalculationSystem.Update().
//  Tests set KingStateComponent.Phase = Payout to trigger the full pipeline.
// ---------------------------------------------------------------------------

public sealed class KingRelationshipTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// World with both PayoutCalculationSystem and KingRelationshipSystem registered
    /// so that payout events flow into the relationship system automatically.
    /// </summary>
    private static (World world, KingRelationshipSystem relSystem) BuildWorld()
    {
        var world      = new World();
        var payoutCalc = new PayoutCalculationSystem();
        var relSystem  = new KingRelationshipSystem();
        world.RegisterSystem(payoutCalc);
        world.RegisterSystem(relSystem);
        return (world, relSystem);
    }

    /// <summary>Creates a King entity in the Payout phase with a relationship component.</summary>
    private static Entity AddKingInPayout(World world,
        KingReactionState reaction  = KingReactionState.Neutral,
        float             relScore  = 50f)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "King");

        var ks = KingStateComponent.Default;
        ks.Phase         = KingsCourtPhase.Payout;
        ks.ReactionState = reaction;
        world.AddComponent(e, ks);

        var rel = KingRelationshipComponent.Default;
        rel.Score = relScore;
        rel.Tier  = DeriveExpectedTier(relScore);
        world.AddComponent(e, rel);
        return e;
    }

    private static Entity AddRunSummary(World world,
        float loot = 300f, int items = 5,
        float health = 80f, float goodwill = 60f,
        bool delivered = true, bool boss = false)
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
            BossDefeated            = boss,
            IsComplete              = true,
        });
        return e;
    }

    private static KingRelationshipComponent GetRel(World world, Entity king) =>
        world.GetComponent<KingRelationshipComponent>(king);

    // Mirror the tier derivation from KingRelationshipSystem.
    private static KingRelationshipTier DeriveExpectedTier(float score) => score switch
    {
        >= 80f => KingRelationshipTier.Beloved,
        >= 60f => KingRelationshipTier.Respected,
        >= 40f => KingRelationshipTier.Known,
        >= 20f => KingRelationshipTier.Suspected,
        _      => KingRelationshipTier.Despised,
    };

    // -------------------------------------------------------------------------
    //  Score adjustments per reaction
    // -------------------------------------------------------------------------

    [Fact]
    public void Score_IncreasesByTen_WhenKingIspleased()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInPayout(world, reaction: KingReactionState.Pleased, relScore: 50f);
        AddRunSummary(world);

        world.Update(0.016f);

        Assert.Equal(60f, GetRel(world, king).Score, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void Score_IncreasesByFive_WhenKingIsNeutral()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInPayout(world, reaction: KingReactionState.Neutral, relScore: 50f);
        AddRunSummary(world);

        world.Update(0.016f);

        Assert.Equal(55f, GetRel(world, king).Score, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void Score_DecreasesByFive_WhenKingIsDissatisfied()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInPayout(world,
            reaction: KingReactionState.Dissatisfied, relScore: 50f);
        AddRunSummary(world);

        world.Update(0.016f);

        Assert.Equal(45f, GetRel(world, king).Score, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void Score_DecreasesByTen_WhenKingIsFurious()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInPayout(world, reaction: KingReactionState.Furious, relScore: 50f);
        AddRunSummary(world);

        world.Update(0.016f);

        Assert.Equal(40f, GetRel(world, king).Score, precision: 3);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Score clamping
    // -------------------------------------------------------------------------

    [Fact]
    public void Score_ClampedAtOneHundred()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInPayout(world, reaction: KingReactionState.Pleased, relScore: 95f);
        AddRunSummary(world);

        world.Update(0.016f);  // +10 → would be 105, clamped to 100

        Assert.Equal(100f, GetRel(world, king).Score, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void Score_ClampedAtZero()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInPayout(world, reaction: KingReactionState.Furious, relScore: 5f);
        AddRunSummary(world);

        world.Update(0.016f);  // −10 → would be −5, clamped to 0

        Assert.Equal(0f, GetRel(world, king).Score, precision: 3);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Tier derivation
    // -------------------------------------------------------------------------

    [Fact]
    public void Tier_DerivedAsBeloved_WhenScoreAtOrAboveEighty()
    {
        var (world, _) = BuildWorld();
        // Start at 75, pleased adds 10 → 85 → Beloved
        var king = AddKingInPayout(world, reaction: KingReactionState.Pleased, relScore: 75f);
        AddRunSummary(world);

        world.Update(0.016f);

        Assert.Equal(KingRelationshipTier.Beloved, GetRel(world, king).Tier);
        world.Dispose();
    }

    [Fact]
    public void Tier_DerivedAsRespected_WhenScoreBetweenSixtyAndEighty()
    {
        var (world, _) = BuildWorld();
        // Start at 60, neutral adds 5 → 65 → Respected
        var king = AddKingInPayout(world, reaction: KingReactionState.Neutral, relScore: 60f);
        AddRunSummary(world);

        world.Update(0.016f);

        Assert.Equal(KingRelationshipTier.Respected, GetRel(world, king).Tier);
        world.Dispose();
    }

    [Fact]
    public void Tier_DerivedAsKnown_WhenScoreBetweenFortyAndSixty()
    {
        var (world, _) = BuildWorld();
        // Start at 50, neutral adds 5 → 55 → Known
        var king = AddKingInPayout(world, reaction: KingReactionState.Neutral, relScore: 50f);
        AddRunSummary(world);

        world.Update(0.016f);

        Assert.Equal(KingRelationshipTier.Known, GetRel(world, king).Tier);
        world.Dispose();
    }

    [Fact]
    public void Tier_DerivedAsSuspected_WhenScoreBetweenTwentyAndForty()
    {
        var (world, _) = BuildWorld();
        // Start at 30, furious subtracts 10 → 20 → Suspected
        var king = AddKingInPayout(world, reaction: KingReactionState.Furious, relScore: 30f);
        AddRunSummary(world);

        world.Update(0.016f);

        Assert.Equal(KingRelationshipTier.Suspected, GetRel(world, king).Tier);
        world.Dispose();
    }

    [Fact]
    public void Tier_DerivedAsDespised_WhenScoreBelowTwenty()
    {
        var (world, _) = BuildWorld();
        // Start at 15, furious subtracts 10 → 5 → Despised
        var king = AddKingInPayout(world, reaction: KingReactionState.Furious, relScore: 15f);
        AddRunSummary(world);

        world.Update(0.016f);

        Assert.Equal(KingRelationshipTier.Despised, GetRel(world, king).Tier);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Run history ring buffer
    // -------------------------------------------------------------------------

    [Fact]
    public void RunHistory_RecordsFirstEntry()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInPayout(world, reaction: KingReactionState.Neutral);
        AddRunSummary(world, loot: 200f, delivered: true, boss: false);

        world.Update(0.016f);

        var rel = GetRel(world, king);
        Assert.Equal(1, rel.TotalRunCount);

        var entry = rel.GetHistory(0);
        Assert.Equal(200f, entry.LootGoldValue, precision: 3);
        Assert.True(entry.PrincessDelivered);
        Assert.False(entry.BossDefeated);
        world.Dispose();
    }

    [Fact]
    public void TotalRunCount_IncrementedEachRun()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInPayout(world);
        AddRunSummary(world);

        world.Update(0.016f);

        Assert.Equal(1, GetRel(world, king).TotalRunCount);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  No-op guards
    // -------------------------------------------------------------------------

    [Fact]
    public void Relationship_NotUpdated_WhenNoPayoutEvents()
    {
        var (world, _) = BuildWorld();
        var king = world.CreateEntity();
        world.AddTag(king, "King");

        // King is in Inactive phase — PayoutCalculationSystem fires no events.
        world.AddComponent(king, KingStateComponent.Default);
        world.AddComponent(king, KingRelationshipComponent.Default);
        AddRunSummary(world);

        world.Update(0.016f);

        Assert.Equal(50f, GetRel(world, king).Score, precision: 3);  // unchanged default
        world.Dispose();
    }

    [Fact]
    public void Relationship_NotUpdated_WhenKingLacksRelationshipComponent()
    {
        // Should not throw — system guard handles missing component gracefully.
        var (world, _) = BuildWorld();
        var king = world.CreateEntity();
        world.AddTag(king, "King");

        var ks = KingStateComponent.Default;
        ks.Phase         = KingsCourtPhase.Payout;
        ks.ReactionState = KingReactionState.Pleased;
        world.AddComponent(king, ks);
        // Intentionally no KingRelationshipComponent.

        AddRunSummary(world);

        // Should complete without throwing.
        world.Update(0.016f);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  TierBonusPercent property
    // -------------------------------------------------------------------------

    [Fact]
    public void TierBonusPercent_IsTwentyForBeloved()
    {
        var rel = KingRelationshipComponent.Default;
        rel.Tier = KingRelationshipTier.Beloved;
        Assert.Equal(20f, rel.TierBonusPercent, precision: 3);
    }

    [Fact]
    public void TierBonusPercent_IsTenForRespected()
    {
        var rel = KingRelationshipComponent.Default;
        rel.Tier = KingRelationshipTier.Respected;
        Assert.Equal(10f, rel.TierBonusPercent, precision: 3);
    }

    [Fact]
    public void TierBonusPercent_IsZeroForKnown()
    {
        var rel = KingRelationshipComponent.Default;
        rel.Tier = KingRelationshipTier.Known;
        Assert.Equal(0f, rel.TierBonusPercent, precision: 3);
    }

    [Fact]
    public void TierBonusPercent_IsNegativeTenForSuspected()
    {
        var rel = KingRelationshipComponent.Default;
        rel.Tier = KingRelationshipTier.Suspected;
        Assert.Equal(-10f, rel.TierBonusPercent, precision: 3);
    }

    [Fact]
    public void TierBonusPercent_IsNegativeTwentyFiveForDespised()
    {
        var rel = KingRelationshipComponent.Default;
        rel.Tier = KingRelationshipTier.Despised;
        Assert.Equal(-25f, rel.TierBonusPercent, precision: 3);
    }
}
