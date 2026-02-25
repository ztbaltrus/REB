using Microsoft.Xna.Framework;
using REB.Engine.ECS;
using REB.Engine.Player;
using REB.Engine.Player.Components;
using REB.Engine.Player.Princess;
using REB.Engine.Player.Princess.Components;
using REB.Engine.Player.Systems;
using REB.Engine.Rendering.Components;
using Xunit;

namespace REB.Tests.Players;

// ---------------------------------------------------------------------------
//  CarrySystem tests
//  CarrySystem reads CarryComponent and PlayerInputComponent flags directly;
//  no InputSystem / PhysicsSystem is required.
//  CarrySystem.Update() pipeline (in order):
//    ProcessHandoff → ProcessPickUp → ProcessDrop → UpdateCarried
// ---------------------------------------------------------------------------

public sealed class CarrySystemTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static (World world, CarrySystem carry) BuildWorld()
    {
        var world = new World();
        var carry = new CarrySystem();
        world.RegisterSystem(carry);
        return (world, carry);
    }

    private static Entity AddCarrier(World world, Vector3 position,
        bool interactPressed = false, bool dropPressed = false)
    {
        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent
        {
            Position    = position,
            Rotation    = Quaternion.Identity,
            Scale       = Vector3.One,
            WorldMatrix = Matrix.Identity,
        });
        world.AddComponent(entity, new PlayerInputComponent
        {
            UseKeyboard     = true,
            InteractPressed = interactPressed,
            DropPressed     = dropPressed,
        });
        world.AddComponent(entity, CarryComponent.Default);
        world.AddComponent(entity, RoleComponent.None);
        return entity;
    }

    private static Entity AddPrincess(World world, Vector3 position)
    {
        var entity = world.CreateEntity();
        world.AddTag(entity, "Princess");
        world.AddComponent(entity, new TransformComponent
        {
            Position    = position,
            Rotation    = Quaternion.Identity,
            Scale       = Vector3.One,
            WorldMatrix = Matrix.Identity,
        });
        world.AddComponent(entity, PrincessStateComponent.Default);
        return entity;
    }

    // -------------------------------------------------------------------------
    //  Pick-up
    // -------------------------------------------------------------------------

    [Fact]
    public void PickUp_InRange_SetsIsCarrying()
    {
        var (world, _) = BuildWorld();
        var princess = AddPrincess(world, Vector3.Zero);
        // CarrySystem runs Handoff first — no carrier yet → handoff no-ops.
        // Then PickUp: carrier in range with Interact pressed → picks up.
        var carrier = AddCarrier(world, Vector3.Zero, interactPressed: true);

        world.Update(0.016f);

        Assert.True(world.GetComponent<CarryComponent>(carrier).IsCarrying);
        Assert.Equal(princess, world.GetComponent<CarryComponent>(carrier).CarriedEntity);
        Assert.True(world.GetComponent<PrincessStateComponent>(princess).IsBeingCarried);
        world.Dispose();
    }

    [Fact]
    public void PickUp_OutOfRange_DoesNotCarry()
    {
        var (world, _) = BuildWorld();
        AddPrincess(world, new Vector3(100f, 0f, 0f));
        var carrier = AddCarrier(world, Vector3.Zero, interactPressed: true);

        world.Update(0.016f);

        Assert.False(world.GetComponent<CarryComponent>(carrier).IsCarrying);
        world.Dispose();
    }

    [Fact]
    public void PickUp_WithoutInteract_DoesNotCarry()
    {
        var (world, _) = BuildWorld();
        AddPrincess(world, Vector3.Zero);
        var carrier = AddCarrier(world, Vector3.Zero, interactPressed: false);

        world.Update(0.016f);

        Assert.False(world.GetComponent<CarryComponent>(carrier).IsCarrying);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Drop
    // -------------------------------------------------------------------------

    [Fact]
    public void Drop_ReleasesCarry()
    {
        var (world, _) = BuildWorld();
        var princess = AddPrincess(world, Vector3.Zero);
        var carrier  = AddCarrier(world, Vector3.Zero, interactPressed: true);

        // Frame 1: pick up.
        world.Update(0.016f);
        Assert.True(world.GetComponent<CarryComponent>(carrier).IsCarrying);

        // Frame 2: clear interact, set drop.
        ref var pinput = ref world.GetComponent<PlayerInputComponent>(carrier);
        pinput.InteractPressed = false;
        pinput.DropPressed     = true;
        world.Update(0.016f);

        Assert.False(world.GetComponent<CarryComponent>(carrier).IsCarrying);
        Assert.False(world.GetComponent<PrincessStateComponent>(princess).IsBeingCarried);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Handoff
    //  Pipeline order is Handoff → PickUp, so on frame 1 (A not yet carrying):
    //    Handoff: no active carrier → skip.
    //    PickUp:  A picks up princess.
    //  On frame 2 (A carrying, InteractPressed=true, B nearby):
    //    Handoff: A hands off to B.
    //    PickUp:  princess.IsBeingCarried=true → skipped.
    // -------------------------------------------------------------------------

    [Fact]
    public void Handoff_TransfersPrincessToReceiver()
    {
        var (world, _) = BuildWorld();
        var princess = AddPrincess(world, Vector3.Zero);
        // A at origin, B 1 m away (within InteractRange = 1.5 m).
        var carrierA = AddCarrier(world, Vector3.Zero,          interactPressed: true);
        var carrierB = AddCarrier(world, new Vector3(1f, 0f, 0f));

        // Frame 1: Handoff sees A not carrying → skip. PickUp: A picks up.
        world.Update(0.016f);
        Assert.True(world.GetComponent<CarryComponent>(carrierA).IsCarrying,
            "A should have picked up the princess on frame 1.");

        // Frame 2: A presses Interact again → handoff to B.
        ref var pinputA = ref world.GetComponent<PlayerInputComponent>(carrierA);
        pinputA.InteractPressed = true;
        world.Update(0.016f);

        Assert.False(world.GetComponent<CarryComponent>(carrierA).IsCarrying,
            "A should have handed off.");
        Assert.True(world.GetComponent<CarryComponent>(carrierB).IsCarrying,
            "B should now be carrying.");
        Assert.Equal(carrierB,
            world.GetComponent<PrincessStateComponent>(princess).CarrierEntity);
        world.Dispose();
    }

    [Fact]
    public void Handoff_OutOfRange_NoTransfer()
    {
        var (world, _) = BuildWorld();
        AddPrincess(world, Vector3.Zero);
        var carrierA = AddCarrier(world, Vector3.Zero, interactPressed: true);
        // B is far away.
        var carrierB = AddCarrier(world, new Vector3(10f, 0f, 0f));

        // Frame 1: A picks up.
        world.Update(0.016f);

        // Frame 2: A presses Interact, B is out of range.
        ref var pinputA = ref world.GetComponent<PlayerInputComponent>(carrierA);
        pinputA.InteractPressed = true;
        world.Update(0.016f);

        Assert.True(world.GetComponent<CarryComponent>(carrierA).IsCarrying,
            "A should still be carrying — B was out of range.");
        Assert.False(world.GetComponent<CarryComponent>(carrierB).IsCarrying);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Position snapping
    // -------------------------------------------------------------------------

    [Fact]
    public void CarriedPrincess_PositionTracksCarrier()
    {
        var (world, _) = BuildWorld();
        var princess = AddPrincess(world, Vector3.Zero);
        var carrier  = AddCarrier(world, Vector3.Zero, interactPressed: true);

        world.Update(0.016f);  // pick up

        // Move carrier, clear interact, update.
        ref var tf = ref world.GetComponent<TransformComponent>(carrier);
        tf.Position = new Vector3(5f, 0f, 0f);
        ref var pinput = ref world.GetComponent<PlayerInputComponent>(carrier);
        pinput.InteractPressed = false;
        world.Update(0.016f);

        var carry      = world.GetComponent<CarryComponent>(carrier);
        var princessTf = world.GetComponent<TransformComponent>(princess);
        Assert.Equal(tf.Position.X, princessTf.Position.X, precision: 4);
        Assert.Equal(tf.Position.Y + carry.CarryOffsetY, princessTf.Position.Y, precision: 4);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Mood / health
    // -------------------------------------------------------------------------

    [Fact]
    public void CarriedPrincess_HealthDecays()
    {
        var (world, _) = BuildWorld();
        var princess = AddPrincess(world, Vector3.Zero);
        var carrier  = AddCarrier(world, Vector3.Zero, interactPressed: true);

        world.Update(0.016f);  // pick up
        ref var pinput = ref world.GetComponent<PlayerInputComponent>(carrier);
        pinput.InteractPressed = false;

        float before = world.GetComponent<PrincessStateComponent>(princess).Health;
        for (int i = 0; i < 10; i++) world.Update(0.1f);
        float after = world.GetComponent<PrincessStateComponent>(princess).Health;

        Assert.True(after < before, "Health should decay while princess is carried.");
        world.Dispose();
    }

    [Fact]
    public void Princess_Struggles_WhenFurious()
    {
        var (world, _) = BuildWorld();
        var princess = AddPrincess(world, Vector3.Zero);
        var carrier  = AddCarrier(world, Vector3.Zero, interactPressed: true);

        world.Update(0.016f);  // pick up

        // Force health below Furious threshold (< 25).
        ref var ps = ref world.GetComponent<PrincessStateComponent>(princess);
        ps.Health = 5f;

        ref var pinput = ref world.GetComponent<PlayerInputComponent>(carrier);
        pinput.InteractPressed = false;

        world.Update(0.5f);  // enough for struggle burst to start

        ps = ref world.GetComponent<PrincessStateComponent>(princess);
        Assert.Equal(PrincessMoodLevel.Furious, ps.MoodLevel);
        Assert.True(ps.IsStruggling, "Princess should be struggling at Furious mood.");
        world.Dispose();
    }

    [Fact]
    public void Negotiator_SlowsMoodDecay()
    {
        // Use separate world instances to avoid any shared state between the two runs.
        float stdHealth, negHealth;

        // ── Standard carrier ──────────────────────────────────────────────────
        {
            var (world, _) = BuildWorld();
            var princess   = AddPrincess(world, Vector3.Zero);
            var carrier    = AddCarrier(world, Vector3.Zero, interactPressed: true);
            // Role is None (default).
            world.Update(0.016f);  // pick up
            ref var pinput = ref world.GetComponent<PlayerInputComponent>(carrier);
            pinput.InteractPressed = false;
            for (int i = 0; i < 5; i++) world.Update(1f);
            stdHealth = world.GetComponent<PrincessStateComponent>(princess).Health;
            world.Dispose();
        }

        // ── Negotiator carrier ────────────────────────────────────────────────
        {
            var (world, _) = BuildWorld();
            var princess   = AddPrincess(world, Vector3.Zero);
            var carrier    = AddCarrier(world, Vector3.Zero, interactPressed: true);
            ref var role   = ref world.GetComponent<RoleComponent>(carrier);
            role.Role      = PlayerRole.Negotiator;
            world.Update(0.016f);  // pick up
            ref var pinput = ref world.GetComponent<PlayerInputComponent>(carrier);
            pinput.InteractPressed = false;
            for (int i = 0; i < 5; i++) world.Update(1f);
            negHealth = world.GetComponent<PrincessStateComponent>(princess).Health;
            world.Dispose();
        }

        Assert.True(negHealth > stdHealth,
            $"Negotiator should halve mood decay. negHealth={negHealth:F3}, stdHealth={stdHealth:F3}");
    }
}
