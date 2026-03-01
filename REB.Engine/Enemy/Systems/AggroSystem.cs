using Microsoft.Xna.Framework;
using REB.Engine.ECS;
using REB.Engine.Enemy.Components;
using REB.Engine.Physics.Systems;
using REB.Engine.Rendering.Components;

namespace REB.Engine.Enemy.Systems;

/// <summary>
/// Proximity-based aggro detection for enemy entities.
/// <para>
/// Each frame, enemies in Idle or Patrol state scan for the nearest player or
/// princess within <see cref="EnemyAIComponent.SightRange"/>. On detection the
/// enemy transitions to Chase. If the active target exceeds
/// <see cref="EnemyAIComponent.LeashRange"/> the enemy reverts to Patrol.
/// </para>
/// <para>
/// Line-of-sight is approximated by distance only — full raycasting can be wired
/// in once a nav-mesh is available in a later story.
/// </para>
/// </summary>
[RunAfter(typeof(PhysicsSystem))]
public sealed class AggroSystem : GameSystem
{
    public override void Update(float deltaTime)
    {
        var targets = GatherTargets();

        foreach (var enemy in World.Query<EnemyAIComponent, TransformComponent>())
        {
            ref var ai  = ref World.GetComponent<EnemyAIComponent>(enemy);
            var     pos = World.GetComponent<TransformComponent>(enemy).Position;

            switch (ai.State)
            {
                case EnemyAIState.Idle:
                case EnemyAIState.Patrol:
                    TryAcquireTarget(ref ai, pos, targets);
                    break;

                case EnemyAIState.Chase:
                case EnemyAIState.Attack:
                    if (!World.IsAlive(ai.ChaseTarget))
                    {
                        ReturnToPatrol(ref ai);
                        break;
                    }
                    var targetPos = World.GetComponent<TransformComponent>(ai.ChaseTarget).Position;
                    if (Vector3.Distance(pos, targetPos) > ai.LeashRange)
                        ReturnToPatrol(ref ai);
                    break;
            }
        }
    }

    // =========================================================================
    //  Helpers
    // =========================================================================

    private List<(Entity entity, Vector3 pos)> GatherTargets()
    {
        var list = new List<(Entity, Vector3)>();

        foreach (var e in World.GetEntitiesWithTag("Player"))
            if (World.HasComponent<TransformComponent>(e))
                list.Add((e, World.GetComponent<TransformComponent>(e).Position));

        foreach (var e in World.GetEntitiesWithTag("Princess"))
            if (World.HasComponent<TransformComponent>(e))
                list.Add((e, World.GetComponent<TransformComponent>(e).Position));

        return list;
    }

    private static void TryAcquireTarget(
        ref EnemyAIComponent ai,
        Vector3 enemyPos,
        List<(Entity entity, Vector3 pos)> targets)
    {
        Entity nearest     = Entity.Null;
        float  nearestDist = float.MaxValue;

        foreach (var (entity, pos) in targets)
        {
            float d = Vector3.Distance(enemyPos, pos);
            if (d < nearestDist) { nearestDist = d; nearest = entity; }
        }

        if (nearest != Entity.Null && nearestDist <= ai.SightRange)
        {
            ai.ChaseTarget = nearest;
            ai.State       = EnemyAIState.Chase;
        }
    }

    private static void ReturnToPatrol(ref EnemyAIComponent ai)
    {
        ai.ChaseTarget = Entity.Null;
        ai.State       = EnemyAIState.Patrol;
        ai.PatrolIndex = 0;
    }
}
