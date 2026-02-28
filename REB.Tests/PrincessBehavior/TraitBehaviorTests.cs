using REB.Engine.ECS;
using REB.Engine.Player.Princess;
using REB.Engine.Player.Princess.Components;
using REB.Engine.Player.Princess.Systems;
using Xunit;

namespace REB.Tests.PrincessBehavior;

// ---------------------------------------------------------------------------
//  TraitBehaviorSystem tests
//
//  CarrySystem is NOT registered (RunAfter constraint silently ignored).
//  PrincessStateComponent and PrincessTraitComponent are set up manually.
// ---------------------------------------------------------------------------

public sealed class TraitBehaviorTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static World BuildWorld()
    {
        var world = new World();
        world.RegisterSystem(new TraitBehaviorSystem());
        return world;
    }

    private static Entity AddPrincess(
        World world,
        PrincessPersonality personality,
        float baseDecayRate = 2f)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "Princess");
        world.AddComponent(e, PrincessStateComponent.Default);
        world.AddComponent(e, PrincessTraitComponent.ForPersonality(personality) with
        {
            BaseDecayRate = baseDecayRate
        });
        return e;
    }

    // -------------------------------------------------------------------------
    //  Cooperative
    // -------------------------------------------------------------------------

    [Fact]
    public void Cooperative_LowersDecayRate()
    {
        var world    = BuildWorld();
        var princess = AddPrincess(world, PrincessPersonality.Cooperative);

        world.Update(0.016f);

        var ps = world.GetComponent<PrincessStateComponent>(princess);
        Assert.Equal(2f * 0.7f, ps.MoodDecayRate, 4);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Stubborn
    // -------------------------------------------------------------------------

    [Fact]
    public void Stubborn_RaisesDecayRate()
    {
        var world    = BuildWorld();
        var princess = AddPrincess(world, PrincessPersonality.Stubborn);

        world.Update(0.016f);

        var ps = world.GetComponent<PrincessStateComponent>(princess);
        Assert.Equal(2f * 1.3f, ps.MoodDecayRate, 4);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Excited
    // -------------------------------------------------------------------------

    [Fact]
    public void Excited_OscillatesDecayRate()
    {
        var world    = BuildWorld();
        var princess = AddPrincess(world, PrincessPersonality.Excited);

        // Sample at 0.5-second intervals so the phase advances by π/2 each step,
        // hitting sin(π/2)=1, sin(π)=0, sin(3π/2)=−1 in successive frames.
        world.Update(0.5f);
        float rate1 = world.GetComponent<PrincessStateComponent>(princess).MoodDecayRate;

        world.Update(0.5f);
        float rate2 = world.GetComponent<PrincessStateComponent>(princess).MoodDecayRate;

        // Rates should both be in the [0.7, 1.3] × BaseDecayRate range.
        Assert.InRange(rate1, 2f * 0.7f, 2f * 1.3f);
        Assert.InRange(rate2, 2f * 0.7f, 2f * 1.3f);

        // And they should differ at some point (oscillation is active).
        world.Update(0.5f);
        float rate3 = world.GetComponent<PrincessStateComponent>(princess).MoodDecayRate;
        Assert.True(rate1 != rate2 || rate2 != rate3,
            "Excited decay rate should oscillate across frames.");
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Scared
    // -------------------------------------------------------------------------

    [Fact]
    public void Scared_LowDecayRate_WithStableCarrier()
    {
        var world    = BuildWorld();
        var princess = AddPrincess(world, PrincessPersonality.Scared);
        var carrier  = world.CreateEntity();

        // Same carrier across two frames — stable.
        ref var ps = ref world.GetComponent<PrincessStateComponent>(princess);
        ps.CarrierEntity = carrier;
        world.Update(0.016f);  // LastCarrierEntity = carrier after this frame

        ps.CarrierEntity = carrier;  // still same carrier
        world.Update(0.016f);

        var psResult = world.GetComponent<PrincessStateComponent>(princess);
        Assert.Equal(2f * 0.9f, psResult.MoodDecayRate, 4);
        world.Dispose();
    }

    [Fact]
    public void Scared_HighDecayRate_OnCarrierChange()
    {
        var world    = BuildWorld();
        var princess = AddPrincess(world, PrincessPersonality.Scared);
        var carrierA = world.CreateEntity();
        var carrierB = world.CreateEntity();

        // Frame 1: carried by A → LastCarrierEntity becomes A.
        ref var ps = ref world.GetComponent<PrincessStateComponent>(princess);
        ps.CarrierEntity = carrierA;
        world.Update(0.016f);

        // Frame 2: hand off to B → carrierChanged = true.
        ps.CarrierEntity = carrierB;
        world.Update(0.016f);

        var psResult = world.GetComponent<PrincessStateComponent>(princess);
        Assert.Equal(2f * 1.8f, psResult.MoodDecayRate, 4);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  No trait component
    // -------------------------------------------------------------------------

    [Fact]
    public void NoPrincessTrait_DecayRateUnchanged()
    {
        var world    = BuildWorld();
        var princess = world.CreateEntity();
        world.AddTag(princess, "Princess");
        world.AddComponent(princess, PrincessStateComponent.Default);
        // No PrincessTraitComponent added.

        world.Update(0.016f);

        var ps = world.GetComponent<PrincessStateComponent>(princess);
        Assert.Equal(2f, ps.MoodDecayRate, 4);  // Default stays at 2f.
        world.Dispose();
    }
}
