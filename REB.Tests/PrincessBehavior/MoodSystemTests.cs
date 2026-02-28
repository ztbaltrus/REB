using REB.Engine.ECS;
using REB.Engine.Player.Princess;
using REB.Engine.Player.Princess.Components;
using REB.Engine.Player.Princess.Systems;
using Xunit;

namespace REB.Tests.PrincessBehavior;

// ---------------------------------------------------------------------------
//  MoodSystem + MoodReactionSystem tests
//
//  TraitBehaviorSystem is NOT registered (RunAfter silently ignored).
//  PrincessStateComponent fields are set manually.
// ---------------------------------------------------------------------------

public sealed class MoodSystemTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static (World world, MoodReactionSystem reaction) BuildWorld()
    {
        var world    = new World();
        var reaction = new MoodReactionSystem();
        world.RegisterSystem(new MoodSystem());
        world.RegisterSystem(reaction);
        return (world, reaction);
    }

    private static Entity AddPrincess(World world, float goodwill = 50f)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "Princess");

        world.AddComponent(e, PrincessStateComponent.Default);

        var gw = PrincessGoodwillComponent.Default;
        gw.Goodwill = goodwill;
        world.AddComponent(e, gw);
        return e;
    }

    // -------------------------------------------------------------------------
    //  Passive decay
    // -------------------------------------------------------------------------

    [Fact]
    public void Goodwill_DecreasesPassively_WhenNotCarried()
    {
        var (world, _) = BuildWorld();
        var princess   = AddPrincess(world, goodwill: 50f);

        // Not carried — only passive decay applies.
        world.Update(1f);

        var gw = world.GetComponent<PrincessGoodwillComponent>(princess);
        Assert.True(gw.Goodwill < 50f,
            $"Expected goodwill < 50 after passive decay, but got {gw.Goodwill}.");
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Calm carry gain
    // -------------------------------------------------------------------------

    [Fact]
    public void Goodwill_Increases_WhenCarriedCalm()
    {
        var (world, _) = BuildWorld();
        var princess   = AddPrincess(world, goodwill: 50f);

        ref var ps = ref world.GetComponent<PrincessStateComponent>(princess);
        ps.IsBeingCarried = true;
        ps.MoodLevel      = PrincessMoodLevel.Calm;
        ps.IsStruggling   = false;

        world.Update(1f);

        var gw = world.GetComponent<PrincessGoodwillComponent>(princess);
        // Net: +1.0/s gain − 0.2/s passive = +0.8 over 1 second.
        Assert.True(gw.Goodwill > 50f,
            $"Expected goodwill > 50 when carried calmly, but got {gw.Goodwill}.");
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Struggle drain
    // -------------------------------------------------------------------------

    [Fact]
    public void Goodwill_Decreases_WhenStruggling()
    {
        var (world, _) = BuildWorld();
        var princess   = AddPrincess(world, goodwill: 50f);

        ref var ps = ref world.GetComponent<PrincessStateComponent>(princess);
        ps.IsBeingCarried = true;
        ps.IsStruggling   = true;

        world.Update(1f);

        var gw = world.GetComponent<PrincessGoodwillComponent>(princess);
        // Net: −3.0/s struggle − 0.2/s passive = −3.2 over 1 second.
        Assert.True(gw.Goodwill < 47f,
            $"Expected goodwill < 47 during struggle, but got {gw.Goodwill}.");
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Drop penalty
    // -------------------------------------------------------------------------

    [Fact]
    public void Goodwill_AppliesDropPenalty_WhenPrincessDropped()
    {
        var (world, _) = BuildWorld();
        var princess   = AddPrincess(world, goodwill: 50f);

        // Frame 1: princess is being carried.
        ref var ps = ref world.GetComponent<PrincessStateComponent>(princess);
        ps.IsBeingCarried = true;
        ps.MoodLevel      = PrincessMoodLevel.Calm;
        world.Update(0.016f);

        float goodwillAfterCarry = world.GetComponent<PrincessGoodwillComponent>(princess).Goodwill;

        // Frame 2: princess dropped.
        ps.IsBeingCarried = false;
        world.Update(0.016f);

        float goodwillAfterDrop = world.GetComponent<PrincessGoodwillComponent>(princess).Goodwill;

        // Expect at least a 4-point penalty (drop penalty is 5, minus rounding from dt decay).
        Assert.True(goodwillAfterCarry - goodwillAfterDrop > 4f,
            $"Expected drop penalty of ~5, but delta was {goodwillAfterCarry - goodwillAfterDrop:F3}.");
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Clamping
    // -------------------------------------------------------------------------

    [Fact]
    public void Goodwill_ClampedAtZero()
    {
        var (world, _) = BuildWorld();
        var princess   = AddPrincess(world, goodwill: 0.1f);

        ref var ps = ref world.GetComponent<PrincessStateComponent>(princess);
        ps.IsStruggling   = true;
        ps.IsBeingCarried = true;

        world.Update(10f);  // 10 seconds of max drain

        var gw = world.GetComponent<PrincessGoodwillComponent>(princess);
        Assert.Equal(0f, gw.Goodwill);
        world.Dispose();
    }

    [Fact]
    public void Goodwill_ClampedAtOneHundred()
    {
        var (world, _) = BuildWorld();
        var princess   = AddPrincess(world, goodwill: 99.9f);

        ref var ps = ref world.GetComponent<PrincessStateComponent>(princess);
        ps.IsBeingCarried = true;
        ps.MoodLevel      = PrincessMoodLevel.Calm;
        ps.IsStruggling   = false;

        world.Update(10f);  // 10 seconds of calm carry

        var gw = world.GetComponent<PrincessGoodwillComponent>(princess);
        Assert.Equal(100f, gw.Goodwill);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Reaction modes (MoodReactionSystem)
    // -------------------------------------------------------------------------

    [Fact]
    public void ReactionMode_Helping_WhenGoodwillHigh()
    {
        var (world, _) = BuildWorld();
        var princess   = AddPrincess(world, goodwill: 80f);

        world.Update(0.016f);

        var gw = world.GetComponent<PrincessGoodwillComponent>(princess);
        Assert.Equal(PrincessReactionMode.Helping, gw.ReactionMode);
        world.Dispose();
    }

    [Fact]
    public void ReactionMode_Hindering_WhenGoodwillLow()
    {
        var (world, _) = BuildWorld();
        var princess   = AddPrincess(world, goodwill: 10f);

        world.Update(0.016f);

        var gw = world.GetComponent<PrincessGoodwillComponent>(princess);
        Assert.Equal(PrincessReactionMode.Hindering, gw.ReactionMode);
        world.Dispose();
    }

    [Fact]
    public void ReactionMode_Neutral_WhenGoodwillMid()
    {
        var (world, _) = BuildWorld();
        var princess   = AddPrincess(world, goodwill: 50f);

        world.Update(0.016f);

        var gw = world.GetComponent<PrincessGoodwillComponent>(princess);
        Assert.Equal(PrincessReactionMode.Neutral, gw.ReactionMode);
        world.Dispose();
    }

    [Fact]
    public void SpeedModifier_AboveOne_WhenHelping()
    {
        var (world, _) = BuildWorld();
        var princess   = AddPrincess(world, goodwill: 80f);

        world.Update(0.016f);

        var gw = world.GetComponent<PrincessGoodwillComponent>(princess);
        Assert.True(gw.CarrierSpeedModifier > 1f,
            $"Expected CarrierSpeedModifier > 1 when helping, got {gw.CarrierSpeedModifier}.");
        world.Dispose();
    }

    [Fact]
    public void SpeedModifier_BelowOne_WhenHindering()
    {
        var (world, _) = BuildWorld();
        var princess   = AddPrincess(world, goodwill: 10f);

        world.Update(0.016f);

        var gw = world.GetComponent<PrincessGoodwillComponent>(princess);
        Assert.True(gw.CarrierSpeedModifier < 1f,
            $"Expected CarrierSpeedModifier < 1 when hindering, got {gw.CarrierSpeedModifier}.");
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Bark events
    // -------------------------------------------------------------------------

    [Fact]
    public void Bark_FiredOnReactionModeTransition()
    {
        var (world, reaction) = BuildWorld();
        // Start at Neutral (goodwill=50) then jump above Helping threshold.
        var princess = AddPrincess(world, goodwill: 50f);
        world.Update(0.016f);  // settles at Neutral; no bark yet

        // Push goodwill to Helping territory.
        ref var gw = ref world.GetComponent<PrincessGoodwillComponent>(princess);
        gw.Goodwill          = 80f;
        gw.DialogueCooldown  = 0f;

        world.Update(0.016f);

        Assert.Single(reaction.Barks);
        Assert.Equal(PrincessReactionMode.Helping, reaction.Barks[0].ReactionMode);
        world.Dispose();
    }

    [Fact]
    public void Barks_ClearedEachFrame()
    {
        var (world, reaction) = BuildWorld();
        var princess = AddPrincess(world, goodwill: 80f);

        world.Update(0.016f);  // Neutral → Helping transition fires bark
        int firstCount = reaction.Barks.Count;

        // Next frame: mode unchanged, no new bark (cooldown blocks it anyway).
        world.Update(0.016f);
        Assert.Empty(reaction.Barks);

        world.Dispose();
    }

    [Fact]
    public void Bark_RespectsCooldown()
    {
        var (world, reaction) = BuildWorld();
        var princess = AddPrincess(world, goodwill: 80f);

        world.Update(0.016f);  // Neutral → Helping
        int after1st = reaction.Barks.Count;

        // Force another mode change but cooldown is now 4 seconds.
        ref var gw = ref world.GetComponent<PrincessGoodwillComponent>(princess);
        gw.Goodwill = 10f;  // would trigger Hindering
        world.Update(0.016f);

        // Should NOT fire because DialogueCooldown > 0.
        Assert.Empty(reaction.Barks);
        world.Dispose();
    }
}
