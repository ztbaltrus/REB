using Microsoft.Xna.Framework;
using REB.Engine.ECS;
using REB.Engine.Physics.Components;
using REB.Engine.Physics.Systems;
using REB.Engine.Player;
using REB.Engine.Player.Components;
using REB.Engine.Player.Systems;
using REB.Engine.Rendering.Components;
using Xunit;

namespace REB.Tests.Players;

// ---------------------------------------------------------------------------
//  PlayerControllerSystem tests
//
//  InputSystem polls real hardware (needs SDL3 at runtime) so these tests
//  register PhysicsSystem + PlayerControllerSystem only.
//  PlayerControllerSystem gracefully skips input processing when InputSystem
//  is absent but still updates IsGrounded from PhysicsSystem.CollisionEvents.
// ---------------------------------------------------------------------------

public sealed class PlayerControllerTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    /// <summary>Headless world: PhysicsSystem + PlayerControllerSystem only.</summary>
    private static (World world, PlayerControllerSystem ctrl) BuildWorld()
    {
        var world = new World();
        world.RegisterSystem(new PhysicsSystem());
        var ctrl = new PlayerControllerSystem();
        world.RegisterSystem(ctrl);
        return (world, ctrl);
    }

    private static Entity AddPlayer(World world, Vector3 position, int slot = 0)
    {
        var entity = world.CreateEntity();
        world.AddTag(entity, "Player");
        world.AddTag(entity, $"Player{slot + 1}");

        world.AddComponent(entity, new TransformComponent
        {
            Position    = position,
            Rotation    = Quaternion.Identity,
            Scale       = Vector3.One,
            WorldMatrix = Matrix.Identity,
        });
        world.AddComponent(entity, new RigidBodyComponent
        {
            Velocity    = Vector3.Zero,
            Mass        = 75f,
            UseGravity  = false,
            LinearDrag  = 0f,
            IsKinematic = false,
        });
        world.AddComponent(entity, PlayerInputComponent.Keyboard);
        world.AddComponent(entity, CharacterControllerComponent.Default);
        return entity;
    }

    // -------------------------------------------------------------------------
    //  Default state (no Update needed)
    // -------------------------------------------------------------------------

    [Fact]
    public void Player_StartsWithIdleState()
    {
        var (world, _) = BuildWorld();
        var player = AddPlayer(world, Vector3.Zero);

        var ctrl = world.GetComponent<CharacterControllerComponent>(player);
        Assert.Equal(PlayerState.Idle, ctrl.State);
        world.Dispose();
    }

    [Fact]
    public void DefaultView_IsThirdPerson()
    {
        var (world, _) = BuildWorld();
        var player = AddPlayer(world, Vector3.Zero);

        var ctrl = world.GetComponent<CharacterControllerComponent>(player);
        Assert.True(ctrl.ThirdPersonView);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Grounded detection (requires PhysicsSystem to generate collision events)
    // -------------------------------------------------------------------------

    [Fact]
    public void IsGrounded_False_Initially()
    {
        var (world, _) = BuildWorld();
        var player = AddPlayer(world, Vector3.Zero);

        // No floor entity → no upward collision event → not grounded.
        world.Update(0.016f);

        var ctrl = world.GetComponent<CharacterControllerComponent>(player);
        Assert.False(ctrl.IsGrounded);
        world.Dispose();
    }

    [Fact]
    public void IsGrounded_True_AfterLandingOnFloor()
    {
        var (world, _) = BuildWorld();

        // Static box floor: top face at Y = 0 (centre Y = -0.5, half-height = 0.5).
        var floor = world.CreateEntity();
        world.AddComponent(floor, new TransformComponent
        {
            Position    = new Vector3(0f, -0.5f, 0f),
            Rotation    = Quaternion.Identity,
            Scale       = Vector3.One,
            WorldMatrix = Matrix.Identity,
        });
        world.AddComponent(floor, new ColliderComponent
        {
            Shape       = REB.Engine.Physics.ColliderShape.Box,
            HalfExtents = new Vector3(5f, 0.5f, 5f),
            Layer       = REB.Engine.Physics.CollisionLayer.Terrain,
            LayerMask   = REB.Engine.Physics.CollisionLayer.All,
            IsStatic    = true,
        });

        // Player with gravity, positioned slightly overlapping the floor top face.
        var player = AddPlayer(world, new Vector3(0f, 0.3f, 0f));
        ref var rb = ref world.GetComponent<RigidBodyComponent>(player);
        rb.UseGravity = true;
        // Give player a capsule collider so physics can resolve against the floor.
        world.AddComponent(player, ColliderComponent.Capsule(
            radius:     0.4f,
            halfHeight: 0.85f,
            layer:      REB.Engine.Physics.CollisionLayer.Player,
            mask:       REB.Engine.Physics.CollisionLayer.Terrain | REB.Engine.Physics.CollisionLayer.Default));

        world.Update(0.016f);

        var ctrl = world.GetComponent<CharacterControllerComponent>(player);
        Assert.True(ctrl.IsGrounded,
            "Expected IsGrounded=true when a collision with an upward normal is generated.");
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Multiple players
    // -------------------------------------------------------------------------

    [Fact]
    public void TwoPlayers_UpdateWithoutError()
    {
        var (world, _) = BuildWorld();
        AddPlayer(world, new Vector3(0f, 0f, 0f), slot: 0);
        AddPlayer(world, new Vector3(3f, 0f, 0f), slot: 1);

        // Should not throw even without InputSystem.
        world.Update(0.016f);
        world.Update(0.016f);
        world.Dispose();
    }
}
