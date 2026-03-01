using Microsoft.Xna.Framework;
using REB.Engine.Combat.Components;
using REB.Engine.ECS;
using REB.Engine.Physics.Components;

namespace REB.Engine.Combat.Systems;

/// <summary>
/// Applies pending knockback impulses from <see cref="HitReactionComponent"/> to the
/// entity's <see cref="RigidBodyComponent"/>, then counts down the stagger timer.
/// </summary>
[RunAfter(typeof(CombatSystem))]
public sealed class HitReactionSystem : GameSystem
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<HitReactionComponent>())
        {
            ref var hr = ref World.GetComponent<HitReactionComponent>(entity);

            if (hr.IsHit)
            {
                if (World.HasComponent<RigidBodyComponent>(entity))
                {
                    ref var rb = ref World.GetComponent<RigidBodyComponent>(entity);
                    if (!rb.IsKinematic)
                        rb.Velocity += hr.KnockbackVelocity;
                }

                hr.KnockbackVelocity = Vector3.Zero;
                hr.IsHit             = false;
            }

            if (hr.StaggerTimer > 0f)
                hr.StaggerTimer -= deltaTime;
        }
    }
}
