using Microsoft.Xna.Framework;
using REB.Engine.Combat.Components;
using REB.Engine.ECS;
using REB.Engine.Enemy;
using REB.Engine.Enemy.Components;
using REB.Engine.Enemy.Systems;
using REB.Engine.Rendering.Components;
using Xunit;

namespace REB.Tests.EnemyAI;

// ---------------------------------------------------------------------------
//  AggroSystem + EnemyAISystem tests
//
//  PhysicsSystem is not registered; RunAfter constraints on missing systems
//  are silently skipped by the topological sort.
// ---------------------------------------------------------------------------

public sealed class EnemyAITests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static (World world, AggroSystem aggro, EnemyAISystem ai) BuildWorld()
    {
        var world = new World();
        var aggro = new AggroSystem();
        var ai    = new EnemyAISystem();
        world.RegisterSystem(aggro);
        world.RegisterSystem(ai);
        return (world, aggro, ai);
    }

    private static TransformComponent MakeTransform(Vector3 pos) => new()
    {
        Position    = pos,
        Rotation    = Quaternion.Identity,
        Scale       = Vector3.One,
        WorldMatrix = Matrix.Identity,
    };

    private static Entity AddEnemy(World world, Vector3 pos, EnemyArchetype archetype = EnemyArchetype.Guard)
    {
        var e = world.CreateEntity();
        world.AddComponent(e, MakeTransform(pos));

        var aiComp = archetype switch
        {
            EnemyArchetype.Archer => EnemyAIComponent.Archer(pos),
            EnemyArchetype.Brute  => EnemyAIComponent.Brute(pos),
            _                     => EnemyAIComponent.Guard(pos),
        };
        world.AddComponent(e, aiComp);
        return e;
    }

    private static Entity AddPlayer(World world, Vector3 pos)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "Player");
        world.AddComponent(e, MakeTransform(pos));
        return e;
    }

    // -------------------------------------------------------------------------
    //  Idle → Patrol transition
    // -------------------------------------------------------------------------

    [Fact]
    public void Enemy_TransitionsToPatrol_AfterIdleTimerExpires()
    {
        var (world, _, _) = BuildWorld();
        var enemy = AddEnemy(world, Vector3.Zero);

        // Guard IdleTimer = 2s. Run for 3 seconds with no targets nearby.
        world.Update(3f);

        var ai = world.GetComponent<EnemyAIComponent>(enemy);
        Assert.Equal(EnemyAIState.Patrol, ai.State);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Aggro — sight range
    // -------------------------------------------------------------------------

    [Fact]
    public void AggroSystem_SetsChaseState_WhenPlayerWithinSightRange()
    {
        var (world, _, _) = BuildWorld();
        var enemy = AddEnemy(world, Vector3.Zero);  // Guard SightRange = 8

        // Player at 5 units — within sight.
        AddPlayer(world, new Vector3(5f, 0f, 0f));

        world.Update(0.016f);

        var ai = world.GetComponent<EnemyAIComponent>(enemy);
        Assert.Equal(EnemyAIState.Chase, ai.State);
        world.Dispose();
    }

    [Fact]
    public void AggroSystem_NoAggro_WhenPlayerBeyondSightRange()
    {
        var (world, _, _) = BuildWorld();
        var enemy = AddEnemy(world, Vector3.Zero);  // Guard SightRange = 8

        // Player at 12 units — beyond Guard sight.
        AddPlayer(world, new Vector3(12f, 0f, 0f));

        world.Update(0.016f);

        var ai = world.GetComponent<EnemyAIComponent>(enemy);
        Assert.Equal(EnemyAIState.Idle, ai.State);
        world.Dispose();
    }

    [Fact]
    public void AggroSystem_StoresChaseTarget()
    {
        var (world, _, _) = BuildWorld();
        AddEnemy(world, Vector3.Zero);
        var player = AddPlayer(world, new Vector3(4f, 0f, 0f));

        world.Update(0.016f);

        // Only one enemy; grab it.
        foreach (var e in world.Query<EnemyAIComponent>())
        {
            var ai = world.GetComponent<EnemyAIComponent>(e);
            Assert.Equal(player, ai.ChaseTarget);
        }
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Leash — return to patrol
    // -------------------------------------------------------------------------

    [Fact]
    public void AggroSystem_ReturnsToPatrol_WhenTargetExceedsLeashRange()
    {
        var (world, _, _) = BuildWorld();
        var enemy = AddEnemy(world, Vector3.Zero);  // Guard LeashRange = 14
        var player = AddPlayer(world, new Vector3(5f, 0f, 0f));

        world.Update(0.016f);  // aggro acquired
        Assert.Equal(EnemyAIState.Chase,
            world.GetComponent<EnemyAIComponent>(enemy).State);

        // Move player far away.
        ref var tf = ref world.GetComponent<TransformComponent>(player);
        tf.Position = new Vector3(20f, 0f, 0f);

        world.Update(0.016f);  // leash triggers

        var ai = world.GetComponent<EnemyAIComponent>(enemy);
        Assert.Equal(EnemyAIState.Patrol, ai.State);
        Assert.Equal(Entity.Null, ai.ChaseTarget);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Movement — patrol
    // -------------------------------------------------------------------------

    [Fact]
    public void EnemyAISystem_MovesEnemy_DuringPatrol()
    {
        var (world, _, _) = BuildWorld();
        var startPos = new Vector3(10f, 0f, 10f);
        var enemy    = AddEnemy(world, startPos);

        // Skip past idle timer.
        world.Update(3f);

        var ai = world.GetComponent<EnemyAIComponent>(enemy);
        Assert.Equal(EnemyAIState.Patrol, ai.State);

        var posBefore = world.GetComponent<TransformComponent>(enemy).Position;
        world.Update(0.5f);
        var posAfter  = world.GetComponent<TransformComponent>(enemy).Position;

        Assert.NotEqual(posBefore, posAfter);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Attack state — DamageComponent.AttackPressed
    // -------------------------------------------------------------------------

    [Fact]
    public void EnemyAISystem_SetsAttackPressed_WhenInAttackRange()
    {
        var (world, _, _) = BuildWorld();

        // Place enemy right next to a player (within Guard AttackRange = 1.8).
        var spawnPos = new Vector3(0f, 0f, 0f);
        var enemy    = AddEnemy(world, spawnPos);

        // Attach a DamageComponent so EnemyAISystem can signal an attack.
        world.AddComponent(enemy, DamageComponent.MeleeDefault);

        // Manually set Chase state targeting a close player.
        var player = AddPlayer(world, new Vector3(1f, 0f, 0f));
        ref var ai = ref world.GetComponent<EnemyAIComponent>(enemy);
        ai.State       = EnemyAIState.Chase;
        ai.ChaseTarget = player;

        world.Update(0.016f);  // Chase → Attack (dist = 1, AttackRange = 1.8)

        ai = world.GetComponent<EnemyAIComponent>(enemy);
        Assert.Equal(EnemyAIState.Attack, ai.State);

        world.Update(0.016f);  // Attack: sets AttackPressed

        var dmg = world.GetComponent<DamageComponent>(enemy);
        Assert.True(dmg.AttackPressed,
            "EnemyAISystem should set AttackPressed when in Attack state and cooldown is ready.");
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Archetype factories
    // -------------------------------------------------------------------------

    [Fact]
    public void ArcherArchetype_HasGreaterSightRange_ThanGuard()
    {
        var guard  = EnemyAIComponent.Guard(Vector3.Zero);
        var archer = EnemyAIComponent.Archer(Vector3.Zero);
        Assert.True(archer.SightRange > guard.SightRange);
    }

    [Fact]
    public void BruteArchetype_HasGreaterAttackRange_ThanGuard()
    {
        var guard = EnemyAIComponent.Guard(Vector3.Zero);
        var brute = EnemyAIComponent.Brute(Vector3.Zero);
        Assert.True(brute.AttackRange > guard.AttackRange);
    }
}
