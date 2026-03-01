using Microsoft.Xna.Framework;
using REB.Engine.Combat.Components;
using REB.Engine.ECS;
using REB.Engine.Hazards;
using REB.Engine.Hazards.Components;
using REB.Engine.Hazards.Systems;
using REB.Engine.Rendering.Components;
using Xunit;

namespace REB.Tests.Hazards;

// ---------------------------------------------------------------------------
//  TrapTriggerSystem tests
//
//  Only TrapTriggerSystem is registered. PhysicsSystem RunAfter is ignored.
// ---------------------------------------------------------------------------

public sealed class TrapTriggerTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static World BuildWorld()
    {
        var world = new World();
        world.RegisterSystem(new TrapTriggerSystem());
        return world;
    }

    private static TransformComponent MakeTransform(Vector3 pos) => new()
    {
        Position = pos, Rotation = Quaternion.Identity,
        Scale = Vector3.One, WorldMatrix = Matrix.Identity,
    };

    private static Entity AddHazard(World world, Vector3 pos, HazardComponent hazard)
    {
        var e = world.CreateEntity();
        world.AddComponent(e, MakeTransform(pos));
        world.AddComponent(e, hazard);
        return e;
    }

    private static Entity AddPlayer(World world, Vector3 pos, float hp = 100f)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "Player");
        world.AddComponent(e, MakeTransform(pos));
        world.AddComponent(e, HealthComponent.For(hp));
        return e;
    }

    private static Entity AddPrincess(World world, Vector3 pos, float hp = 100f)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "Princess");
        world.AddComponent(e, MakeTransform(pos));
        world.AddComponent(e, HealthComponent.For(hp));
        return e;
    }

    // -------------------------------------------------------------------------
    //  SpikeTrap — trigger
    // -------------------------------------------------------------------------

    [Fact]
    public void SpikeTrap_Triggers_WhenPlayerStepsInRadius()
    {
        var world  = BuildWorld();
        var hazard = AddHazard(world, Vector3.Zero, HazardComponent.SpikeTrap);
        AddPlayer(world, new Vector3(0.3f, 0f, 0f));  // within TriggerRadius = 0.6

        world.Update(0.016f);

        var hz = world.GetComponent<HazardComponent>(hazard);
        Assert.Equal(HazardState.Triggered, hz.State);
        world.Dispose();
    }

    [Fact]
    public void SpikeTrap_DoesNotTrigger_WhenPlayerOutsideRadius()
    {
        var world  = BuildWorld();
        var hazard = AddHazard(world, Vector3.Zero, HazardComponent.SpikeTrap);
        AddPlayer(world, new Vector3(2f, 0f, 0f));  // outside radius

        world.Update(0.016f);

        var hz = world.GetComponent<HazardComponent>(hazard);
        Assert.Equal(HazardState.Armed, hz.State);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  SpikeTrap — damage
    // -------------------------------------------------------------------------

    [Fact]
    public void SpikeTrap_DamagesPlayer_WhenTriggered()
    {
        var world  = BuildWorld();
        AddHazard(world, Vector3.Zero, HazardComponent.SpikeTrap);
        var player = AddPlayer(world, new Vector3(0.3f, 0f, 0f));

        world.Update(0.016f);  // trigger fires

        var hp = world.GetComponent<HealthComponent>(player);
        Assert.True(hp.CurrentHealth < 100f,
            $"Player should take damage from spike trap, but health is {hp.CurrentHealth}.");
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  SpikeTrap — state machine (Armed → Triggered → Resetting → Armed)
    // -------------------------------------------------------------------------

    [Fact]
    public void SpikeTrap_Cycles_ArmedTriggeredResettingArmed()
    {
        var world  = BuildWorld();
        var hazard = AddHazard(world, Vector3.Zero, HazardComponent.SpikeTrap);  // TriggeredDuration=0.5, ResetTime=3
        AddPlayer(world, new Vector3(0.3f, 0f, 0f));

        world.Update(0.016f);   // → Triggered
        Assert.Equal(HazardState.Triggered,
            world.GetComponent<HazardComponent>(hazard).State);

        world.Update(1f);       // TriggeredDuration elapses → Resetting
        Assert.Equal(HazardState.Resetting,
            world.GetComponent<HazardComponent>(hazard).State);

        world.Update(4f);       // ResetTime elapses → Armed
        Assert.Equal(HazardState.Armed,
            world.GetComponent<HazardComponent>(hazard).State);

        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Princess receives double damage
    // -------------------------------------------------------------------------

    [Fact]
    public void Princess_ReceivesDoubleDamage_FromSpikeTrap()
    {
        var world  = BuildWorld();
        // Use a tiny trigger so both player and princess can stand at the same spot.
        var spike = HazardComponent.SpikeTrap;
        spike.TriggerRadius = 2f;

        AddHazard(world, Vector3.Zero, spike);

        var player   = AddPlayer(world, new Vector3(0.5f, 0f, 0f));
        var princess = AddPrincess(world, new Vector3(0.5f, 0f, 0f));

        world.Update(0.016f);  // trigger; both get hit once

        var playerHp   = world.GetComponent<HealthComponent>(player).CurrentHealth;
        var princessHp = world.GetComponent<HealthComponent>(princess).CurrentHealth;

        float playerDmg   = 100f - playerHp;
        float princessDmg = 100f - princessHp;

        Assert.True(princessDmg > playerDmg,
            $"Princess should take more damage than player. " +
            $"Player took {playerDmg}, princess took {princessDmg}.");
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  SwingingBlade — oscillation phase advances
    // -------------------------------------------------------------------------

    [Fact]
    public void SwingingBlade_OscillationPhase_AdvancesEachFrame()
    {
        var world  = BuildWorld();
        var hazard = AddHazard(world, Vector3.Zero, HazardComponent.SwingingBlade);

        float phaseBefore = world.GetComponent<HazardComponent>(hazard).OscillationPhase;
        world.Update(0.1f);
        float phaseAfter = world.GetComponent<HazardComponent>(hazard).OscillationPhase;

        Assert.True(phaseAfter > phaseBefore,
            $"OscillationPhase should increase. Before={phaseBefore}, After={phaseAfter}.");
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Invulnerable target — no damage
    // -------------------------------------------------------------------------

    [Fact]
    public void InvulnerablePlayer_NotDamagedBySpikeTrap()
    {
        var world  = BuildWorld();
        AddHazard(world, Vector3.Zero, HazardComponent.SpikeTrap);
        var player = AddPlayer(world, new Vector3(0.3f, 0f, 0f));

        ref var hp = ref world.GetComponent<HealthComponent>(player);
        hp.IsInvulnerable = true;

        world.Update(0.016f);

        Assert.Equal(100f, world.GetComponent<HealthComponent>(player).CurrentHealth);
        world.Dispose();
    }
}
