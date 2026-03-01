using REB.Engine.ECS;
using REB.Engine.KingsCourt;
using REB.Engine.KingsCourt.Components;
using REB.Engine.Tavern;
using REB.Engine.Tavern.Components;
using REB.Engine.Tavern.Systems;
using Xunit;

namespace REB.Tests.Tavern;

// ---------------------------------------------------------------------------
//  TavernSceneSystem tests
//
//  KingsCourtSceneSystem is not registered; [RunAfter] is silently skipped.
//  Tests set KingStateComponent.Phase = Dismissed to trigger tavern opening.
// ---------------------------------------------------------------------------

public sealed class TavernSceneTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static World BuildWorld()
    {
        var world = new World();
        world.RegisterSystem(new TavernSceneSystem());
        return world;
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

    private static Entity AddKingDismissed(World world)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "King");
        var ks = KingStateComponent.Default;
        ks.Phase = KingsCourtPhase.Dismissed;
        world.AddComponent(e, ks);
        return e;
    }

    private static Entity AddKingInPhase(World world, KingsCourtPhase phase)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "King");
        var ks = KingStateComponent.Default;
        ks.Phase = phase;
        world.AddComponent(e, ks);
        return e;
    }

    private static TavernStateComponent GetTavernState(World world, Entity tavern) =>
        world.GetComponent<TavernStateComponent>(tavern);

    // -------------------------------------------------------------------------
    //  Scene activation
    // -------------------------------------------------------------------------

    [Fact]
    public void Tavern_Opens_WhenKingIsDismissed()
    {
        var world  = BuildWorld();
        var tavern = AddTavern(world);
        AddKingDismissed(world);

        world.Update(0.016f);

        var ts = GetTavernState(world, tavern);
        Assert.Equal(TavernPhase.Open, ts.Phase);
        Assert.True(ts.SceneActive);
        world.Dispose();
    }

    [Fact]
    public void Tavern_DoesNotOpen_WhenKingIsNotDismissed()
    {
        var world  = BuildWorld();
        var tavern = AddTavern(world);
        AddKingInPhase(world, KingsCourtPhase.Review);

        world.Update(0.016f);

        var ts = GetTavernState(world, tavern);
        Assert.Equal(TavernPhase.Inactive, ts.Phase);
        Assert.False(ts.SceneActive);
        world.Dispose();
    }

    [Fact]
    public void Tavern_DoesNotOpen_WhenNoKingEntity()
    {
        var world  = BuildWorld();
        var tavern = AddTavern(world);

        world.Update(0.016f);

        Assert.Equal(TavernPhase.Inactive, GetTavernState(world, tavern).Phase);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  King phase reset on tavern open
    // -------------------------------------------------------------------------

    [Fact]
    public void KingPhase_ResetToInactive_WhenTavernOpens()
    {
        var world = BuildWorld();
        AddTavern(world);
        var king = AddKingDismissed(world);

        world.Update(0.016f);

        // King's Phase should have been reset to prevent re-triggering.
        Assert.Equal(KingsCourtPhase.Inactive,
            world.GetComponent<KingStateComponent>(king).Phase);
        world.Dispose();
    }

    [Fact]
    public void Tavern_DoesNotReopenNextFrame_AfterKingReset()
    {
        var world  = BuildWorld();
        var tavern = AddTavern(world, openDuration: 60f);
        AddKingDismissed(world);

        world.Update(0.016f);   // opens tavern, resets King to Inactive
        Assert.True(GetTavernState(world, tavern).SceneActive);

        // Manually close it to test re-open guard.
        ref var ts = ref world.GetComponent<TavernStateComponent>(tavern);
        ts.SceneActive = false;
        ts.Phase       = TavernPhase.Inactive;

        world.Update(0.016f);   // King now Inactive — should not re-open

        Assert.False(GetTavernState(world, tavern).SceneActive);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Auto-close after duration
    // -------------------------------------------------------------------------

    [Fact]
    public void Tavern_AutoCloses_AfterOpenDuration()
    {
        var world  = BuildWorld();
        // Use a short duration so we can elapse it cheaply.
        var tavern = AddTavern(world, openDuration: 0.1f);
        AddKingDismissed(world);

        world.Update(0.016f);   // opens tavern
        Assert.True(GetTavernState(world, tavern).SceneActive);

        world.Update(1f);       // 1s >> 0.1s → should auto-close

        var ts = GetTavernState(world, tavern);
        Assert.Equal(TavernPhase.Inactive, ts.Phase);
        Assert.False(ts.SceneActive);
        world.Dispose();
    }

    [Fact]
    public void Tavern_StaysOpen_BeforeDurationElapses()
    {
        var world  = BuildWorld();
        var tavern = AddTavern(world, openDuration: 60f);
        AddKingDismissed(world);

        world.Update(0.016f);   // opens tavern

        world.Update(5f);       // still within 60s window

        Assert.True(GetTavernState(world, tavern).SceneActive);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Phase timer resets on close
    // -------------------------------------------------------------------------

    [Fact]
    public void PhaseTimer_ResetsToZero_WhenTavernCloses()
    {
        var world  = BuildWorld();
        var tavern = AddTavern(world, openDuration: 0.1f);
        AddKingDismissed(world);

        world.Update(0.016f);   // open
        world.Update(1f);       // close

        Assert.Equal(0f, GetTavernState(world, tavern).PhaseTimer, precision: 3);
        world.Dispose();
    }
}
