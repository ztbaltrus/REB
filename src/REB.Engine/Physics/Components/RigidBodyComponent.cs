using Microsoft.Xna.Framework;
using REB.Engine.ECS;

namespace REB.Engine.Physics.Components;

/// <summary>
/// Dynamics data for a physics-simulated entity.
/// Attach alongside a <see cref="ColliderComponent"/> and a
/// <see cref="REB.Engine.Rendering.Components.TransformComponent"/> to make
/// the entity respond to gravity and collision forces.
/// </summary>
public struct RigidBodyComponent : IComponent
{
    /// <summary>Current linear velocity in world units per second.</summary>
    public Vector3 Velocity;

    /// <summary>Mass in kilograms. Affects how much force changes velocity.</summary>
    public float Mass;

    /// <summary>When true, gravity accelerates this body downward each frame.</summary>
    public bool UseGravity;

    /// <summary>
    /// Linear drag coefficient (fraction of velocity lost per second).
    /// 0 = no drag, values near 1 bring the body to a quick stop.
    /// </summary>
    public float LinearDrag;

    /// <summary>
    /// When true the body is driven externally (animation, player input).
    /// Gravity and collision impulses are not applied.
    /// </summary>
    public bool IsKinematic;

    /// <summary>
    /// World-space force accumulated this frame via <see cref="AddForce"/>.
    /// Applied during integration then zeroed.
    /// </summary>
    public Vector3 AccumulatedForce;

    // -------------------------------------------------------------------------
    //  Presets
    // -------------------------------------------------------------------------

    public static RigidBodyComponent Default => new()
    {
        Velocity         = Vector3.Zero,
        Mass             = 1f,
        UseGravity       = true,
        LinearDrag       = 0.05f,
        IsKinematic      = false,
        AccumulatedForce = Vector3.Zero,
    };

    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    /// <summary>Schedules a world-space force (F = ma) applied this frame.</summary>
    public void AddForce(Vector3 force) => AccumulatedForce += force;

    /// <summary>Applies a world-space impulse directly to velocity (ignores mass).</summary>
    public void AddImpulse(Vector3 impulse) => Velocity += impulse;
}
