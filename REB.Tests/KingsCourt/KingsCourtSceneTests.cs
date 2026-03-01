using REB.Engine.ECS;
using REB.Engine.KingsCourt;
using REB.Engine.KingsCourt.Components;
using REB.Engine.KingsCourt.Systems;
using Xunit;

namespace REB.Tests.KingsCourt;

// ---------------------------------------------------------------------------
//  KingsCourtSceneSystem tests
// ---------------------------------------------------------------------------

public sealed class KingsCourtSceneTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static (World world, KingsCourtSceneSystem scene) BuildWorld()
    {
        var world = new World();
        var scene = new KingsCourtSceneSystem();
        world.RegisterSystem(scene);
        return (world, scene);
    }

    private static Entity AddKing(World world)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "King");
        world.AddComponent(e, KingStateComponent.Default);
        return e;
    }

    private static Entity AddRunSummary(World world, bool complete,
        float loot = 500f, int items = 10,
        float health = 90f, float goodwill = 80f,
        bool delivered = true, int drops = 0, bool boss = true)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "RunSummary");
        world.AddComponent(e, new RunSummaryComponent
        {
            LootGoldValue          = loot,
            LootItemCount          = items,
            PrincessHealth         = health,
            PrincessGoodwill       = goodwill,
            PrincessDeliveredSafely = delivered,
            PrincessDropCount      = drops,
            BossDefeated           = boss,
            IsComplete             = complete,
        });
        return e;
    }

    // -------------------------------------------------------------------------
    //  Scene activation
    // -------------------------------------------------------------------------

    [Fact]
    public void Scene_StartsArriving_WhenRunSummaryIsComplete()
    {
        var (world, _) = BuildWorld();
        AddKing(world);
        AddRunSummary(world, complete: true);

        world.Update(0.016f);

        var ks = FindKingState(world);
        Assert.Equal(KingsCourtPhase.Arriving, ks.Phase);
        Assert.True(ks.SceneActive);
        world.Dispose();
    }

    [Fact]
    public void Scene_DoesNotStart_WhenRunSummaryIncomplete()
    {
        var (world, _) = BuildWorld();
        AddKing(world);
        AddRunSummary(world, complete: false);

        world.Update(0.016f);

        var ks = FindKingState(world);
        Assert.Equal(KingsCourtPhase.Inactive, ks.Phase);
        Assert.False(ks.SceneActive);
        world.Dispose();
    }

    [Fact]
    public void Scene_DoesNotStart_WhenNoRunSummaryEntity()
    {
        var (world, _) = BuildWorld();
        AddKing(world);

        world.Update(0.016f);

        var ks = FindKingState(world);
        Assert.False(ks.SceneActive);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Phase transitions
    // -------------------------------------------------------------------------

    [Fact]
    public void Phase_AdvancesToReview_AfterArrivingDuration()
    {
        var (world, _) = BuildWorld();
        var king = AddKing(world);
        SetShortDurations(world, king);
        AddRunSummary(world, complete: true);

        world.Update(0.016f);  // starts: Arriving
        world.Update(1f);      // ArrivingDuration(0.1) elapses → Review

        var ks = FindKingState(world);
        Assert.Equal(KingsCourtPhase.Review, ks.Phase);
        world.Dispose();
    }

    [Fact]
    public void Phase_AdvancesToNegotiation_AfterReviewDuration()
    {
        var (world, _) = BuildWorld();
        var king = AddKing(world);
        SetShortDurations(world, king);
        AddRunSummary(world, complete: true);

        world.Update(0.016f);
        world.Update(1f);  // → Review
        world.Update(1f);  // ReviewDuration elapses → Negotiation

        Assert.Equal(KingsCourtPhase.Negotiation, FindKingState(world).Phase);
        world.Dispose();
    }

    [Fact]
    public void Phase_AdvancesToPayout_AfterNegotiationDuration()
    {
        var (world, _) = BuildWorld();
        var king = AddKing(world);
        SetShortDurations(world, king);
        AddRunSummary(world, complete: true);

        world.Update(0.016f);
        world.Update(1f);   // → Review
        world.Update(1f);   // → Negotiation
        world.Update(1f);   // → Payout

        Assert.Equal(KingsCourtPhase.Payout, FindKingState(world).Phase);
        world.Dispose();
    }

    [Fact]
    public void Phase_AdvancesToDismissed_AfterPayoutDuration()
    {
        var (world, _) = BuildWorld();
        var king = AddKing(world);
        SetShortDurations(world, king);
        AddRunSummary(world, complete: true);

        world.Update(0.016f);
        world.Update(1f);
        world.Update(1f);
        world.Update(1f);
        world.Update(1f);  // → Dismissed

        Assert.Equal(KingsCourtPhase.Dismissed, FindKingState(world).Phase);
        world.Dispose();
    }

    [Fact]
    public void SceneActive_ClearedAfterDismissed()
    {
        var (world, _) = BuildWorld();
        var king = AddKing(world);
        SetShortDurations(world, king);
        AddRunSummary(world, complete: true);

        // Run long enough to pass all phases.
        for (int i = 0; i < 10; i++) world.Update(1f);

        Assert.False(FindKingState(world).SceneActive);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  King reaction
    // -------------------------------------------------------------------------

    [Fact]
    public void KingReaction_Pleased_OnExcellentRun()
    {
        var (world, _) = BuildWorld();
        var king = AddKing(world);
        SetShortDurations(world, king);
        AddRunSummary(world, complete: true,
            loot: 1000f, items: 20, health: 100f, goodwill: 100f,
            delivered: true, drops: 0, boss: true);

        world.Update(0.016f);

        Assert.Equal(KingReactionState.Pleased, FindKingState(world).ReactionState);
        world.Dispose();
    }

    [Fact]
    public void KingReaction_Furious_OnTerribleRun()
    {
        var (world, _) = BuildWorld();
        var king = AddKing(world);
        SetShortDurations(world, king);
        // Low loot, damaged princess, not delivered, 5 drops.
        AddRunSummary(world, complete: true,
            loot: 20f, items: 2, health: 10f, goodwill: 5f,
            delivered: false, drops: 5, boss: false);

        world.Update(0.016f);

        Assert.Equal(KingReactionState.Furious, FindKingState(world).ReactionState);
        world.Dispose();
    }

    [Fact]
    public void KingReaction_Neutral_OnDecentRun()
    {
        var (world, _) = BuildWorld();
        var king = AddKing(world);
        SetShortDurations(world, king);
        AddRunSummary(world, complete: true,
            loot: 300f, items: 8, health: 60f, goodwill: 50f,
            delivered: true, drops: 1, boss: false);

        world.Update(0.016f);

        Assert.Equal(KingReactionState.Neutral, FindKingState(world).ReactionState);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Dialogue events
    // -------------------------------------------------------------------------

    [Fact]
    public void DialogueEvents_FiredOnSceneStart()
    {
        var (world, scene) = BuildWorld();
        var king = AddKing(world);
        SetShortDurations(world, king);
        AddRunSummary(world, complete: true);

        world.Update(0.016f);  // starts scene — Arriving fires dialogue

        Assert.NotEmpty(scene.DialogueEvents);
        world.Dispose();
    }

    [Fact]
    public void DialogueEvents_ClearedEachFrame()
    {
        var (world, scene) = BuildWorld();
        var king = AddKing(world);
        SetShortDurations(world, king);
        AddRunSummary(world, complete: true);

        world.Update(0.016f);
        Assert.NotEmpty(scene.DialogueEvents);

        world.Update(0.016f);  // No phase transition — no new events.
        Assert.Empty(scene.DialogueEvents);
        world.Dispose();
    }

    [Fact]
    public void DialogueEvents_FiredOnReviewEntry()
    {
        var (world, scene) = BuildWorld();
        var king = AddKing(world);
        SetShortDurations(world, king);
        AddRunSummary(world, complete: true);

        world.Update(0.016f);  // Arriving starts + Arriving dialogue
        world.Update(1f);      // → Review: fires review dialogue

        Assert.NotEmpty(scene.DialogueEvents);
        Assert.All(scene.DialogueEvents, e => Assert.Equal(KingsCourtPhase.Review, e.Phase));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static KingStateComponent FindKingState(World world)
    {
        foreach (var e in world.GetEntitiesWithTag("King"))
            return world.GetComponent<KingStateComponent>(e);
        return default;
    }

    /// <summary>Sets all phase durations to 0.1 s so tests can advance phases cheaply.</summary>
    private static void SetShortDurations(World world, Entity king)
    {
        ref var ks = ref world.GetComponent<KingStateComponent>(king);
        ks.ArrivingDuration    = 0.1f;
        ks.ReviewDuration      = 0.1f;
        ks.NegotiationDuration = 0.1f;
        ks.PayoutDuration      = 0.1f;
    }
}
