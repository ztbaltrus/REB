using Microsoft.Xna.Framework;
using REB.Engine.Combat.Components;
using REB.Engine.ECS;
using REB.Engine.Enemy.Components;
using REB.Engine.Rendering.Components;

namespace REB.Engine.Enemy.Systems;

/// <summary>
/// Drives enemy movement and attack decisions according to the state set by <see cref="AggroSystem"/>.
/// <para>Per-frame pipeline:</para>
/// <list type="number">
///   <item><b>Idle</b> — decrement IdleTimer; transition to Patrol when it elapses.</item>
///   <item><b>Patrol</b> — step through four cardinal waypoints around SpawnPosition at WalkSpeed.</item>
///   <item><b>Chase</b> — steer toward ChaseTarget at RunSpeed; enter Attack when within AttackRange.</item>
///   <item><b>Attack</b> — set <see cref="DamageComponent.AttackPressed"/>; revert to Chase if
///                          the target moves out of range.</item>
/// </list>
/// </summary>
[RunAfter(typeof(AggroSystem))]
public sealed class EnemyAISystem : GameSystem
{
    // Four cardinal patrol offsets (N, E, S, W) around the spawn point.
    private static readonly Vector3[] PatrolOffsets =
    [
        new( 0f, 0f,  3f),
        new( 3f, 0f,  0f),
        new( 0f, 0f, -3f),
        new(-3f, 0f,  0f),
    ];

    private const float WaypointReach = 0.4f;

    public override void Update(float deltaTime)
    {
        foreach (var enemy in World.Query<EnemyAIComponent, TransformComponent>())
        {
            ref var ai = ref World.GetComponent<EnemyAIComponent>(enemy);
            ref var tf = ref World.GetComponent<TransformComponent>(enemy);

            switch (ai.State)
            {
                case EnemyAIState.Idle:   UpdateIdle(ref ai, deltaTime);               break;
                case EnemyAIState.Patrol: UpdatePatrol(ref ai, ref tf, deltaTime);     break;
                case EnemyAIState.Chase:  UpdateChase(enemy, ref ai, ref tf, deltaTime); break;
                case EnemyAIState.Attack: UpdateAttack(enemy, ref ai, ref tf);         break;
            }
        }
    }

    // =========================================================================
    //  State handlers
    // =========================================================================

    private static void UpdateIdle(ref EnemyAIComponent ai, float dt)
    {
        ai.IdleTimer -= dt;
        if (ai.IdleTimer <= 0f)
        {
            ai.State     = EnemyAIState.Patrol;
            ai.IdleTimer = 2f;  // pre-load for the next idle visit
        }
    }

    private static void UpdatePatrol(
        ref EnemyAIComponent ai, ref TransformComponent tf, float dt)
    {
        var waypoint = ai.SpawnPosition + PatrolOffsets[ai.PatrolIndex % PatrolOffsets.Length];
        var delta    = waypoint - tf.Position;
        delta.Y      = 0f;

        if (delta.Length() < WaypointReach)
        {
            ai.PatrolIndex = (ai.PatrolIndex + 1) % PatrolOffsets.Length;
            return;
        }

        MoveToward(ref tf, delta, ai.WalkSpeed, dt);
    }

    private void UpdateChase(
        Entity enemy, ref EnemyAIComponent ai, ref TransformComponent tf, float dt)
    {
        if (!World.IsAlive(ai.ChaseTarget)) { ai.State = EnemyAIState.Patrol; return; }

        var targetPos = World.GetComponent<TransformComponent>(ai.ChaseTarget).Position;
        var delta     = targetPos - tf.Position;
        delta.Y       = 0f;

        if (delta.Length() <= ai.AttackRange) { ai.State = EnemyAIState.Attack; return; }

        MoveToward(ref tf, delta, ai.RunSpeed, dt);
    }

    private void UpdateAttack(
        Entity enemy, ref EnemyAIComponent ai, ref TransformComponent tf)
    {
        if (!World.IsAlive(ai.ChaseTarget)) { ai.State = EnemyAIState.Patrol; return; }

        float dist = Vector3.Distance(
            tf.Position,
            World.GetComponent<TransformComponent>(ai.ChaseTarget).Position);

        if (dist > ai.AttackRange * 1.2f) { ai.State = EnemyAIState.Chase; return; }

        // Signal the DamageComponent to fire an attack next CombatSystem pass.
        if (World.HasComponent<DamageComponent>(enemy))
        {
            ref var dmg = ref World.GetComponent<DamageComponent>(enemy);
            if (dmg.AttackTimer <= 0f)
                dmg.AttackPressed = true;
        }
    }

    // =========================================================================
    //  Movement helper
    // =========================================================================

    private static void MoveToward(
        ref TransformComponent tf, Vector3 delta, float speed, float dt)
    {
        float len = delta.Length();
        if (len < 1e-6f) return;

        var dir = delta / len;
        tf.Position += dir * speed * dt;
        tf.Rotation  = Quaternion.CreateFromAxisAngle(Vector3.Up, MathF.Atan2(dir.X, dir.Z));
    }
}
