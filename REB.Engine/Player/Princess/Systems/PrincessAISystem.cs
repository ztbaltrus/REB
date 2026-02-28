using Microsoft.Xna.Framework;
using REB.Engine.ECS;
using REB.Engine.Physics.Components;
using REB.Engine.Player.Princess.Components;
using REB.Engine.Rendering.Components;

namespace REB.Engine.Player.Princess.Systems;

/// <summary>
/// Runs the princess's autonomous FSM when she is not being carried.
/// Deterministic (no RNG per frame) and safe to skip on non-authoritative clients.
/// <para>
/// States:
/// <list type="bullet">
///   <item>Carried   — AI suspended; velocity zeroed; IsKinematic = true.</item>
///   <item>SeekingExit — Furious: moves toward the nearest Exit-tagged entity.</item>
///   <item>Wandering  — Moves to a deterministic nearby position on a timed interval.</item>
///   <item>Idle       — Stationary; transitions to Wandering when the timer fires.</item>
/// </list>
/// </para>
/// </summary>
[RunAfter(typeof(MoodReactionSystem))]
public sealed class PrincessAISystem : GameSystem
{
    // Distance threshold to consider a target reached.
    private const float ArrivalRadius = 0.2f;

    public override void Update(float deltaTime)
    {
        Entity princess = FindPrincess();
        if (!World.IsAlive(princess)) return;
        if (!World.HasComponent<PrincessStateComponent>(princess)) return;
        if (!World.HasComponent<NavAgentComponent>(princess)) return;

        ref var ps  = ref World.GetComponent<PrincessStateComponent>(princess);
        ref var nav = ref World.GetComponent<NavAgentComponent>(princess);

        // Non-authoritative instances skip all AI logic (server owns princess state).
        if (!nav.IsAuthoritative) return;

        // ── Carried: suspend AI and lock the body ────────────────────────────
        if (ps.IsBeingCarried)
        {
            nav.CurrentState = PrincessAIState.Carried;
            if (World.HasComponent<RigidBodyComponent>(princess))
            {
                ref var rb = ref World.GetComponent<RigidBodyComponent>(princess);
                rb.Velocity    = Vector3.Zero;
                rb.IsKinematic = true;
            }
            return;
        }

        // Re-enable physics when released from carry.
        if (World.HasComponent<RigidBodyComponent>(princess))
        {
            ref var rb = ref World.GetComponent<RigidBodyComponent>(princess);
            rb.IsKinematic = false;
        }

        // ── Furious: seek nearest exit ───────────────────────────────────────
        if (ps.MoodLevel == PrincessMoodLevel.Furious)
        {
            TrySetExitTarget(princess, ref nav);
        }
        else if (nav.CurrentState == PrincessAIState.SeekingExit)
        {
            // Mood improved while seeking — revert to idle.
            nav.CurrentState = PrincessAIState.Idle;
        }

        // ── Wander timer (only when not seeking an exit) ─────────────────────
        if (nav.CurrentState != PrincessAIState.SeekingExit)
        {
            nav.WanderTimer -= deltaTime;
            if (nav.WanderTimer <= 0f)
            {
                StartWander(princess, ref nav);
                nav.WanderTimer = nav.WanderInterval;
            }
        }

        // ── Movement toward current target ───────────────────────────────────
        MoveTowardTarget(princess, ref nav);
    }

    // =========================================================================
    //  FSM helpers
    // =========================================================================

    private void TrySetExitTarget(Entity princess, ref NavAgentComponent nav)
    {
        nav.CurrentState = PrincessAIState.SeekingExit;

        var   princessPos = World.GetComponent<TransformComponent>(princess).Position;
        Entity nearest    = Entity.Null;
        float  nearestDist = float.MaxValue;

        foreach (var exit in World.GetEntitiesWithTag("Exit"))
        {
            if (!World.HasComponent<TransformComponent>(exit)) continue;
            var   exitPos = World.GetComponent<TransformComponent>(exit).Position;
            float dist    = Vector3.Distance(princessPos, exitPos);
            if (dist < nearestDist)
            {
                nearest     = exit;
                nearestDist = dist;
            }
        }

        if (World.IsAlive(nearest))
            nav.TargetPosition = World.GetComponent<TransformComponent>(nearest).Position;
    }

    private void StartWander(Entity princess, ref NavAgentComponent nav)
    {
        // Deterministic offset based on current position — avoids RNG per frame.
        var pos    = World.GetComponent<TransformComponent>(princess).Position;
        var offset = new Vector3(
            MathF.Sin(pos.X + pos.Z) * nav.WanderRadius,
            0f,
            MathF.Cos(pos.X - pos.Z) * nav.WanderRadius);

        nav.TargetPosition   = pos + offset;
        nav.CurrentState     = PrincessAIState.Wandering;
        nav.HasReachedTarget = false;
    }

    private void MoveTowardTarget(Entity princess, ref NavAgentComponent nav)
    {
        if (nav.CurrentState == PrincessAIState.Idle) return;

        var   pos   = World.GetComponent<TransformComponent>(princess).Position;
        var   delta = nav.TargetPosition - pos;
        delta.Y     = 0f;  // horizontal movement only
        float dist  = delta.Length();

        if (dist < ArrivalRadius)
        {
            nav.HasReachedTarget = true;
            nav.CurrentState     = PrincessAIState.Idle;
            if (World.HasComponent<RigidBodyComponent>(princess))
            {
                ref var rbStop = ref World.GetComponent<RigidBodyComponent>(princess);
                rbStop.Velocity = Vector3.Zero;
            }
            return;
        }

        var direction = delta / dist;
        if (World.HasComponent<RigidBodyComponent>(princess))
        {
            ref var rb = ref World.GetComponent<RigidBodyComponent>(princess);
            rb.Velocity = new Vector3(
                direction.X * nav.MoveSpeed,
                rb.Velocity.Y,
                direction.Z * nav.MoveSpeed);
        }
    }

    // =========================================================================
    //  Helper
    // =========================================================================

    private Entity FindPrincess()
    {
        foreach (var e in World.GetEntitiesWithTag("Princess"))
            return e;
        return Entity.Null;
    }
}
