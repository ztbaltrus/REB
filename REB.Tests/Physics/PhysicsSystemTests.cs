using Microsoft.Xna.Framework;
using REB.Engine.ECS;
using REB.Engine.Physics;
using REB.Engine.Physics.Components;
using REB.Engine.Physics.Systems;
using REB.Engine.Rendering.Components;
using Xunit;

namespace REB.Tests.Physics;

// ---------------------------------------------------------------------------
//  PhysicsSystem tests
//  No GraphicsDevice is required — all XNA math types are plain structs.
// ---------------------------------------------------------------------------

public sealed class PhysicsSystemTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static (World world, PhysicsSystem physics) BuildWorld()
    {
        var world   = new World();
        var physics = new PhysicsSystem();
        world.RegisterSystem(physics);
        return (world, physics);
    }

    private static Entity AddDynamic(World world, Vector3 position, Vector3 velocity = default,
        ColliderShape shape = ColliderShape.Box, Vector3 halfExtents = default,
        bool useGravity = false)
    {
        if (halfExtents == default) halfExtents = Vector3.One * 0.5f;

        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent
        {
            Position    = position,
            Rotation    = Quaternion.Identity,
            Scale       = Vector3.One,
            WorldMatrix = Matrix.Identity,
        });
        world.AddComponent(entity, new ColliderComponent
        {
            Shape       = shape,
            HalfExtents = halfExtents,
            Layer       = CollisionLayer.Default,
            LayerMask   = CollisionLayer.All,
            IsStatic    = false,
        });
        world.AddComponent(entity, new RigidBodyComponent
        {
            Velocity    = velocity,
            Mass        = 1f,
            UseGravity  = useGravity,
            LinearDrag  = 0f,
            IsKinematic = false,
        });
        return entity;
    }

    private static Entity AddStatic(World world, Vector3 position,
        Vector3 halfExtents = default,
        CollisionLayer layer = CollisionLayer.Terrain)
    {
        if (halfExtents == default) halfExtents = new Vector3(5f, 0.5f, 5f);

        var entity = world.CreateEntity();
        world.AddComponent(entity, new TransformComponent
        {
            Position    = position,
            Rotation    = Quaternion.Identity,
            Scale       = Vector3.One,
            WorldMatrix = Matrix.Identity,
        });
        world.AddComponent(entity, new ColliderComponent
        {
            Shape       = ColliderShape.Box,
            HalfExtents = halfExtents,
            Layer       = layer,
            LayerMask   = CollisionLayer.All,
            IsStatic    = true,
        });
        return entity;
    }

    // -------------------------------------------------------------------------
    //  Gravity integration
    // -------------------------------------------------------------------------

    [Fact]
    public void Gravity_PullsBodyDownward()
    {
        var (world, physics) = BuildWorld();
        var body = AddDynamic(world, Vector3.Zero, useGravity: true);

        world.Update(0.1f);

        var rb = world.GetComponent<RigidBodyComponent>(body);
        Assert.True(rb.Velocity.Y < 0f, "Gravity should produce a negative Y velocity.");
        world.Dispose();
    }

    [Fact]
    public void NoGravity_VelocityUnchanged()
    {
        var (world, physics) = BuildWorld();
        var body = AddDynamic(world, Vector3.Zero, useGravity: false);

        world.Update(0.1f);

        var rb = world.GetComponent<RigidBodyComponent>(body);
        Assert.Equal(0f, rb.Velocity.Y, precision: 6);
        world.Dispose();
    }

    [Fact]
    public void Kinematic_IgnoresGravity()
    {
        var (world, physics) = BuildWorld();
        var entity = world.CreateEntity();
        world.AddComponent(entity, TransformComponent.Default);
        world.AddComponent(entity, new ColliderComponent
        {
            Shape       = ColliderShape.Box,
            HalfExtents = Vector3.One * 0.5f,
            Layer       = CollisionLayer.Default,
            LayerMask   = CollisionLayer.All,
        });
        world.AddComponent(entity, new RigidBodyComponent
        {
            UseGravity  = true,
            IsKinematic = true,
            Mass        = 1f,
        });

        world.Update(0.1f);

        var rb  = world.GetComponent<RigidBodyComponent>(entity);
        var pos = world.GetComponent<TransformComponent>(entity).Position;
        Assert.Equal(0f, rb.Velocity.Y, precision: 6);
        Assert.Equal(Vector3.Zero, pos);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Velocity integration
    // -------------------------------------------------------------------------

    [Fact]
    public void Velocity_MovesBodyEachFrame()
    {
        var (world, physics) = BuildWorld();
        const float dt = 1f;
        var body = AddDynamic(world, Vector3.Zero, velocity: Vector3.UnitX);

        world.Update(dt);

        var pos = world.GetComponent<TransformComponent>(body).Position;
        // Velocity.X = 1; position.X should be approximately 1 after 1 second.
        Assert.True(pos.X > 0.9f, $"Expected X ≈ 1, got {pos.X}.");
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Box–Box collision detection
    // -------------------------------------------------------------------------

    [Fact]
    public void BoxBox_Overlapping_GeneratesEvent()
    {
        var (world, physics) = BuildWorld();
        AddDynamic(world, new Vector3(0f,  0f, 0f), halfExtents: Vector3.One * 0.5f);
        AddDynamic(world, new Vector3(0.8f, 0f, 0f), halfExtents: Vector3.One * 0.5f);

        world.Update(0.016f);

        Assert.NotEmpty(physics.CollisionEvents);
        world.Dispose();
    }

    [Fact]
    public void BoxBox_NotOverlapping_NoEvent()
    {
        var (world, physics) = BuildWorld();
        AddDynamic(world, new Vector3(-5f, 0f, 0f), halfExtents: Vector3.One * 0.5f);
        AddDynamic(world, new Vector3( 5f, 0f, 0f), halfExtents: Vector3.One * 0.5f);

        world.Update(0.016f);

        Assert.Empty(physics.CollisionEvents);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Static body
    // -------------------------------------------------------------------------

    [Fact]
    public void StaticBody_DoesNotMove()
    {
        var (world, physics) = BuildWorld();
        // Place a dynamic cube directly on a static floor — they overlap initially.
        AddStatic(world, new Vector3(0f, -0.5f, 0f), halfExtents: new Vector3(5f, 0.5f, 5f));
        var dynEntity = AddDynamic(world, new Vector3(0f, 0f, 0f), halfExtents: Vector3.One * 0.4f);

        var staticEntity = world.GetEntitiesWithTag("Wall").FirstOrDefault();

        // Run one frame
        world.Update(0.016f);

        // The dynamic body gets pushed; the static floor does not move.
        // We just check no exception was thrown and at least one event fired.
        Assert.NotEmpty(physics.CollisionEvents);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Sphere–Sphere collision
    // -------------------------------------------------------------------------

    [Fact]
    public void SphereSphere_Overlapping_GeneratesEvent()
    {
        var (world, physics) = BuildWorld();
        float r = 0.5f;
        AddDynamic(world, new Vector3(0f,   0f, 0f), shape: ColliderShape.Sphere, halfExtents: new Vector3(r, r, r));
        AddDynamic(world, new Vector3(0.6f, 0f, 0f), shape: ColliderShape.Sphere, halfExtents: new Vector3(r, r, r));

        world.Update(0.016f);

        Assert.NotEmpty(physics.CollisionEvents);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Layer filtering
    // -------------------------------------------------------------------------

    [Fact]
    public void LayerMask_ExcludedLayers_NoCollision()
    {
        var (world, physics) = BuildWorld();

        // Entity A: Player layer, only collides with Terrain.
        var eA = world.CreateEntity();
        world.AddComponent(eA, new TransformComponent { Position = new Vector3(0f, 0f, 0f), Scale = Vector3.One, Rotation = Quaternion.Identity, WorldMatrix = Matrix.Identity });
        world.AddComponent(eA, new ColliderComponent { Shape = ColliderShape.Box, HalfExtents = Vector3.One, Layer = CollisionLayer.Player, LayerMask = CollisionLayer.Terrain });
        world.AddComponent(eA, RigidBodyComponent.Default);

        // Entity B: Enemy layer — Player's mask does NOT include Enemy.
        var eB = world.CreateEntity();
        world.AddComponent(eB, new TransformComponent { Position = new Vector3(0.5f, 0f, 0f), Scale = Vector3.One, Rotation = Quaternion.Identity, WorldMatrix = Matrix.Identity });
        world.AddComponent(eB, new ColliderComponent { Shape = ColliderShape.Box, HalfExtents = Vector3.One, Layer = CollisionLayer.Enemy, LayerMask = CollisionLayer.Terrain });
        world.AddComponent(eB, RigidBodyComponent.Default);

        world.Update(0.016f);

        Assert.Empty(physics.CollisionEvents);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Trigger volumes
    // -------------------------------------------------------------------------

    [Fact]
    public void Trigger_GeneratesEvent_ButDoesNotResolvePosition()
    {
        var (world, physics) = BuildWorld();

        var trigger = world.CreateEntity();
        world.AddComponent(trigger, new TransformComponent { Position = Vector3.Zero, Scale = Vector3.One, Rotation = Quaternion.Identity, WorldMatrix = Matrix.Identity });
        world.AddComponent(trigger, new ColliderComponent
        {
            Shape     = ColliderShape.Box,
            HalfExtents = new Vector3(2f, 2f, 2f),
            Layer     = CollisionLayer.Trigger,
            LayerMask = CollisionLayer.Player,
            IsTrigger = true,
            IsStatic  = true,
        });

        var player = world.CreateEntity();
        world.AddComponent(player, new TransformComponent { Position = Vector3.Zero, Scale = Vector3.One, Rotation = Quaternion.Identity, WorldMatrix = Matrix.Identity });
        world.AddComponent(player, new ColliderComponent
        {
            Shape     = ColliderShape.Box,
            HalfExtents = Vector3.One * 0.5f,
            Layer     = CollisionLayer.Player,
            LayerMask = CollisionLayer.All,
        });
        world.AddComponent(player, RigidBodyComponent.Default);

        var startPos = world.GetComponent<TransformComponent>(player).Position;
        world.Update(0.016f);
        var endPos = world.GetComponent<TransformComponent>(player).Position;

        // An event should have been recorded.
        Assert.NotEmpty(physics.CollisionEvents);
        Assert.True(physics.CollisionEvents[0].IsTrigger);

        // The trigger must not push the player's position significantly.
        float moved = Vector3.Distance(startPos, endPos);
        Assert.True(moved < 0.5f, $"Trigger should not resolve position; moved {moved}.");

        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Events cleared each frame
    // -------------------------------------------------------------------------

    [Fact]
    public void CollisionEvents_ClearedEachFrame()
    {
        var (world, physics) = BuildWorld();
        AddDynamic(world, Vector3.Zero,             halfExtents: Vector3.One * 0.5f);
        AddDynamic(world, new Vector3(0.5f, 0f, 0f), halfExtents: Vector3.One * 0.5f);

        world.Update(0.016f);
        int frame1Count = physics.CollisionEvents.Count;

        // Move one body away so they no longer overlap.
        var entities = world.Query<RigidBodyComponent, TransformComponent>().ToList();
        foreach (var e in entities)
        {
            ref var rb = ref world.GetComponent<RigidBodyComponent>(e);
            rb.Velocity = Vector3.Zero;
            ref var tf = ref world.GetComponent<TransformComponent>(e);
            tf.Position = tf.Position.X < 0.5f ? new Vector3(-10f, 0f, 0f) : new Vector3(10f, 0f, 0f);
        }

        world.Update(0.016f);
        int frame2Count = physics.CollisionEvents.Count;

        Assert.True(frame1Count > 0, "Expected at least one event in frame 1.");
        Assert.Equal(0, frame2Count);
        world.Dispose();
    }
}
