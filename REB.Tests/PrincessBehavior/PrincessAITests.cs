using Microsoft.Xna.Framework;
using REB.Engine.ECS;
using REB.Engine.Physics.Components;
using REB.Engine.Player.Princess;
using REB.Engine.Player.Princess.Components;
using REB.Engine.Player.Princess.Systems;
using REB.Engine.Rendering.Components;
using Xunit;

namespace REB.Tests.PrincessBehavior;

// ---------------------------------------------------------------------------
//  PrincessAISystem tests
//
//  Only PrincessAISystem is registered.
//  PrincessStateComponent / NavAgentComponent / RigidBodyComponent fields
//  are set manually before each Update call.
// ---------------------------------------------------------------------------

public sealed class PrincessAITests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static World BuildWorld()
    {
        var world = new World();
        world.RegisterSystem(new PrincessAISystem());
        return world;
    }

    /// <summary>
    /// Princess with WanderTimer = 5 s so the wander decision does NOT fire
    /// during a single 16-ms test frame.
    /// </summary>
    private static Entity AddPrincess(World world, Vector3 position)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "Princess");

        world.AddComponent(e, new TransformComponent
        {
            Position    = position,
            Rotation    = Quaternion.Identity,
            Scale       = Vector3.One,
            WorldMatrix = Matrix.Identity,
        });

        world.AddComponent(e, PrincessStateComponent.Default);

        world.AddComponent(e, new NavAgentComponent
        {
            CurrentState    = PrincessAIState.Idle,
            MoveSpeed       = 2f,
            WanderInterval  = 5f,
            WanderRadius    = 4f,
            WanderTimer     = 5f,    // positive → won't fire this frame
            IsAuthoritative = true,
        });

        world.AddComponent(e, new RigidBodyComponent
        {
            Velocity    = Vector3.Zero,
            Mass        = 60f,
            UseGravity  = false,
            LinearDrag  = 5f,
            IsKinematic = false,
        });

        return e;
    }

    // -------------------------------------------------------------------------
    //  Carried — AI suspended
    // -------------------------------------------------------------------------

    [Fact]
    public void AI_Suspended_WhenCarried()
    {
        var world   = BuildWorld();
        var princess = AddPrincess(world, Vector3.Zero);

        ref var ps = ref world.GetComponent<PrincessStateComponent>(princess);
        ps.IsBeingCarried = true;

        world.Update(0.016f);

        var nav = world.GetComponent<NavAgentComponent>(princess);
        var rb  = world.GetComponent<RigidBodyComponent>(princess);

        Assert.Equal(PrincessAIState.Carried, nav.CurrentState);
        Assert.Equal(Vector3.Zero, rb.Velocity);
        Assert.True(rb.IsKinematic, "IsKinematic must be true while carried.");
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Furious → SeekingExit
    // -------------------------------------------------------------------------

    [Fact]
    public void AI_SeeksExit_WhenFurious()
    {
        var world   = BuildWorld();
        var princess = AddPrincess(world, Vector3.Zero);

        // Place an Exit entity in the world.
        var exit = world.CreateEntity();
        world.AddTag(exit, "Exit");
        world.AddComponent(exit, new TransformComponent
        {
            Position    = new Vector3(10f, 0f, 0f),
            Rotation    = Quaternion.Identity,
            Scale       = Vector3.One,
            WorldMatrix = Matrix.Identity,
        });

        ref var ps = ref world.GetComponent<PrincessStateComponent>(princess);
        ps.MoodLevel = PrincessMoodLevel.Furious;

        world.Update(0.016f);

        var nav = world.GetComponent<NavAgentComponent>(princess);
        Assert.Equal(PrincessAIState.SeekingExit, nav.CurrentState);
        Assert.Equal(10f, nav.TargetPosition.X, precision: 3);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Wander timer fires → Wandering state
    // -------------------------------------------------------------------------

    [Fact]
    public void AI_Wanders_WhenTimerExpires()
    {
        var world   = BuildWorld();
        var princess = AddPrincess(world, Vector3.Zero);

        // Override WanderTimer so it fires on the next Update.
        ref var nav = ref world.GetComponent<NavAgentComponent>(princess);
        nav.WanderTimer = 0f;

        world.Update(0.016f);

        var navAfter = world.GetComponent<NavAgentComponent>(princess);
        Assert.Equal(PrincessAIState.Wandering, navAfter.CurrentState);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Moving toward a distant target sets a non-zero velocity
    // -------------------------------------------------------------------------

    [Fact]
    public void AI_MovesTowardTarget()
    {
        var world   = BuildWorld();
        var princess = AddPrincess(world, Vector3.Zero);

        ref var nav = ref world.GetComponent<NavAgentComponent>(princess);
        nav.CurrentState   = PrincessAIState.Wandering;
        nav.TargetPosition = new Vector3(10f, 0f, 0f);
        nav.WanderTimer    = 10f;  // suppress another wander decision

        world.Update(0.016f);

        var rb = world.GetComponent<RigidBodyComponent>(princess);
        Assert.True(rb.Velocity.X > 0f,
            $"Expected positive X velocity toward target, got {rb.Velocity.X}.");
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Target within arrival radius → Idle + HasReachedTarget
    // -------------------------------------------------------------------------

    [Fact]
    public void AI_BecomesIdle_WhenTargetReached()
    {
        var world   = BuildWorld();
        var princess = AddPrincess(world, Vector3.Zero);

        ref var nav = ref world.GetComponent<NavAgentComponent>(princess);
        nav.CurrentState   = PrincessAIState.Wandering;
        nav.TargetPosition = new Vector3(0.05f, 0f, 0f);  // inside 0.2 m arrival radius
        nav.WanderTimer    = 10f;

        world.Update(0.016f);

        var navAfter = world.GetComponent<NavAgentComponent>(princess);
        Assert.Equal(PrincessAIState.Idle, navAfter.CurrentState);
        Assert.True(navAfter.HasReachedTarget,
            "HasReachedTarget must be set when the princess arrives at her target.");
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Non-authoritative instance → no AI updates
    // -------------------------------------------------------------------------

    [Fact]
    public void AI_Inactive_WhenNotAuthoritative()
    {
        var world   = BuildWorld();
        var princess = AddPrincess(world, Vector3.Zero);

        ref var nav = ref world.GetComponent<NavAgentComponent>(princess);
        nav.IsAuthoritative = false;
        nav.WanderTimer     = 0f;  // would trigger wander if authoritative

        // Furious would trigger exit-seeking on an authoritative instance.
        ref var ps = ref world.GetComponent<PrincessStateComponent>(princess);
        ps.MoodLevel = PrincessMoodLevel.Furious;

        world.Update(0.016f);

        var navAfter = world.GetComponent<NavAgentComponent>(princess);
        Assert.Equal(PrincessAIState.Idle, navAfter.CurrentState);
        world.Dispose();
    }
}
