using Microsoft.Xna.Framework;
using REB.Engine.ECS;

namespace REB.Engine.Combat.Components;

/// <summary>
/// Stores pending knockback state written by <see cref="Systems.CombatSystem"/>
/// and consumed by <see cref="Systems.HitReactionSystem"/> to push the entity's
/// <see cref="REB.Engine.Physics.Components.RigidBodyComponent"/> velocity.
/// </summary>
public struct HitReactionComponent : IComponent
{
    /// <summary>True on the frame a hit was registered; cleared after the velocity impulse is applied.</summary>
    public bool IsHit;

    /// <summary>Velocity impulse (world-space) to add to the rigid body on the next HitReactionSystem tick.</summary>
    public Vector3 KnockbackVelocity;

    /// <summary>How many seconds the stagger state lasts (set from DamageComponent.KnockbackForce).</summary>
    public float StaggerDuration;

    /// <summary>Seconds remaining in the current stagger animation. Counts down to 0.</summary>
    public float StaggerTimer;

    /// <summary>True while StaggerTimer > 0 (entity is still reeling from a hit).</summary>
    public bool IsStaggered => StaggerTimer > 0f;

    public static HitReactionComponent Default => new()
    {
        IsHit             = false,
        KnockbackVelocity = Vector3.Zero,
        StaggerDuration   = 0.3f,
        StaggerTimer      = 0f,
    };
}
