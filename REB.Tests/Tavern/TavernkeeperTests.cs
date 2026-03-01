using REB.Engine.ECS;
using REB.Engine.KingsCourt;
using REB.Engine.KingsCourt.Components;
using REB.Engine.Tavern.Components;
using REB.Engine.Tavern.Systems;
using Xunit;

namespace REB.Tests.Tavern;

// ---------------------------------------------------------------------------
//  TavernkeeperSystem tests
//
//  TavernSceneSystem is registered so it can open the tavern (detected by
//  TavernkeeperSystem via the _wasOpen flag).
// ---------------------------------------------------------------------------

public sealed class TavernkeeperTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static (World world, TavernkeeperSystem keeperSystem) BuildWorld()
    {
        var world        = new World();
        var tavernScene  = new TavernSceneSystem();
        var keeperSystem = new TavernkeeperSystem();
        world.RegisterSystem(tavernScene);
        world.RegisterSystem(keeperSystem);
        return (world, keeperSystem);
    }

    private static Entity AddTavern(World world, float openDuration = 60f)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "Tavern");
        var ts = TavernStateComponent.Default;
        ts.OpenDuration = openDuration;
        world.AddComponent(e, ts);
        return e;
    }

    private static Entity AddTavernkeeper(World world)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "Tavernkeeper");
        world.AddComponent(e, TavernkeeperNPCComponent.Default);
        return e;
    }

    private static Entity AddKingDismissed(World world,
        KingReactionState reaction = KingReactionState.Pleased)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "King");
        var ks = KingStateComponent.Default;
        ks.Phase         = KingsCourtPhase.Dismissed;
        ks.ReactionState = reaction;
        world.AddComponent(e, ks);
        world.AddComponent(e, KingRelationshipComponent.Default);
        return e;
    }

    private static TavernkeeperNPCComponent GetNPC(World world, Entity tk) =>
        world.GetComponent<TavernkeeperNPCComponent>(tk);

    // -------------------------------------------------------------------------
    //  Dialogue fires when tavern opens
    // -------------------------------------------------------------------------

    [Fact]
    public void DialogueEvents_FiredWhenTavernOpens()
    {
        var (world, keeper) = BuildWorld();
        AddTavern(world);
        AddTavernkeeper(world);
        AddKingDismissed(world);

        world.Update(0.016f);   // TavernScene opens → keeper fires welcome + tip

        Assert.NotEmpty(keeper.DialogueEvents);
        world.Dispose();
    }

    [Fact]
    public void WelcomeDialogue_AlwaysFired_OnOpen()
    {
        var (world, keeper) = BuildWorld();
        AddTavern(world);
        AddTavernkeeper(world);
        AddKingDismissed(world);

        world.Update(0.016f);

        Assert.Contains(keeper.DialogueEvents, e => e.LineKey == "tavernkeeper.welcome");
        world.Dispose();
    }

    [Fact]
    public void TipDialogue_AlwaysFired_OnOpen()
    {
        var (world, keeper) = BuildWorld();
        AddTavern(world);
        AddTavernkeeper(world);
        AddKingDismissed(world);

        world.Update(0.016f);

        Assert.Contains(keeper.DialogueEvents,
            e => e.LineKey.StartsWith("tavernkeeper.tip."));
        world.Dispose();
    }

    [Fact]
    public void DialogueEvents_NotFired_WhenTavernNotOpen()
    {
        var (world, keeper) = BuildWorld();
        AddTavern(world);
        AddTavernkeeper(world);
        // King NOT dismissed — tavern stays Inactive.
        var king = world.CreateEntity();
        world.AddTag(king, "King");
        world.AddComponent(king, KingStateComponent.Default);

        world.Update(0.016f);

        Assert.Empty(keeper.DialogueEvents);
        world.Dispose();
    }

    [Fact]
    public void DialogueEvents_NotFiredAgain_NextFrame()
    {
        var (world, keeper) = BuildWorld();
        AddTavern(world);
        AddTavernkeeper(world);
        AddKingDismissed(world);

        world.Update(0.016f);   // opens → fires
        Assert.NotEmpty(keeper.DialogueEvents);

        world.Update(0.016f);   // already open — no new events
        Assert.Empty(keeper.DialogueEvents);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Consecutive pleased counter
    // -------------------------------------------------------------------------

    [Fact]
    public void ConsecutivePleasedRuns_IncrementedOnPleasedReaction()
    {
        var (world, _) = BuildWorld();
        AddTavern(world);
        var tk = AddTavernkeeper(world);
        AddKingDismissed(world, reaction: KingReactionState.Pleased);

        world.Update(0.016f);

        Assert.Equal(1, GetNPC(world, tk).ConsecutivePleasedRuns);
        world.Dispose();
    }

    [Fact]
    public void ConsecutivePleasedRuns_ResetOnNonPleasedReaction()
    {
        var (world, _) = BuildWorld();
        AddTavern(world);
        var tk = AddTavernkeeper(world);

        // Pre-seed with 2 consecutive pleased.
        ref var npc = ref world.GetComponent<TavernkeeperNPCComponent>(tk);
        npc.ConsecutivePleasedRuns = 2;

        AddKingDismissed(world, reaction: KingReactionState.Furious);

        world.Update(0.016f);

        Assert.Equal(0, GetNPC(world, tk).ConsecutivePleasedRuns);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Service unlocks
    // -------------------------------------------------------------------------

    [Fact]
    public void Medic_Unlocked_AfterThreeConsecutivePleasedRuns()
    {
        var (world, _) = BuildWorld();
        AddTavern(world);
        var tk = AddTavernkeeper(world);

        // Pre-seed with 2 consecutive pleased.
        ref var npc = ref world.GetComponent<TavernkeeperNPCComponent>(tk);
        npc.ConsecutivePleasedRuns = 2;

        AddKingDismissed(world, reaction: KingReactionState.Pleased);

        world.Update(0.016f);   // 2+1 = 3 → unlock Medic

        Assert.True(GetNPC(world, tk).MedicUnlocked);
        world.Dispose();
    }

    [Fact]
    public void Medic_NotUnlocked_BeforeThreeConsecutive()
    {
        var (world, _) = BuildWorld();
        AddTavern(world);
        var tk = AddTavernkeeper(world);

        // Only 1 consecutive so far.
        ref var npc = ref world.GetComponent<TavernkeeperNPCComponent>(tk);
        npc.ConsecutivePleasedRuns = 1;

        AddKingDismissed(world, reaction: KingReactionState.Pleased);

        world.Update(0.016f);   // 1+1 = 2 — not yet

        Assert.False(GetNPC(world, tk).MedicUnlocked);
        world.Dispose();
    }

    [Fact]
    public void Fence_Unlocked_AfterFiveRuns()
    {
        var (world, _) = BuildWorld();
        AddTavern(world);
        var tk = AddTavernkeeper(world);
        var king = AddKingDismissed(world);

        // Set 5 total runs in the relationship component.
        ref var rel = ref world.GetComponent<KingRelationshipComponent>(king);
        rel.TotalRunCount = 5;

        world.Update(0.016f);

        Assert.True(GetNPC(world, tk).FenceUnlocked);
        world.Dispose();
    }

    [Fact]
    public void Fence_NotUnlocked_BeforeFiveRuns()
    {
        var (world, _) = BuildWorld();
        AddTavern(world);
        var tk = AddTavernkeeper(world);
        var king = AddKingDismissed(world);

        ref var rel = ref world.GetComponent<KingRelationshipComponent>(king);
        rel.TotalRunCount = 3;

        world.Update(0.016f);

        Assert.False(GetNPC(world, tk).FenceUnlocked);
        world.Dispose();
    }

    [Fact]
    public void Scout_Unlocked_WhenRelationshipScoreIsRespected()
    {
        var (world, _) = BuildWorld();
        AddTavern(world);
        var tk = AddTavernkeeper(world);
        var king = AddKingDismissed(world);

        ref var rel = ref world.GetComponent<KingRelationshipComponent>(king);
        rel.Score = 65f;    // ≥ 60 → Respected

        world.Update(0.016f);

        Assert.True(GetNPC(world, tk).ScoutUnlocked);
        world.Dispose();
    }

    [Fact]
    public void Scout_NotUnlocked_WhenScoreBelowRespected()
    {
        var (world, _) = BuildWorld();
        AddTavern(world);
        var tk = AddTavernkeeper(world);
        var king = AddKingDismissed(world);

        ref var rel = ref world.GetComponent<KingRelationshipComponent>(king);
        rel.Score = 45f;    // < 60

        world.Update(0.016f);

        Assert.False(GetNPC(world, tk).ScoutUnlocked);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Tip line key stored on component
    // -------------------------------------------------------------------------

    [Fact]
    public void LastTipLineKey_SetAfterTavernOpens()
    {
        var (world, _) = BuildWorld();
        AddTavern(world);
        var tk = AddTavernkeeper(world);
        AddKingDismissed(world);

        world.Update(0.016f);

        Assert.NotEmpty(GetNPC(world, tk).LastTipLineKey);
        world.Dispose();
    }
}
