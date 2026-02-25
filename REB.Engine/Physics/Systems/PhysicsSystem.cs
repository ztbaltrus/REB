using Microsoft.Xna.Framework;
using REB.Engine.ECS;
using REB.Engine.Physics.Components;
using REB.Engine.Rendering.Components;

namespace REB.Engine.Physics.Systems;

/// <summary>
/// Simulates rigid-body dynamics and resolves collisions each frame.
/// <para>Per-frame pipeline:</para>
/// <list type="number">
///   <item>Integrate forces (gravity + accumulated) into velocity.</item>
///   <item>Integrate velocity into position (semi-implicit Euler).</item>
///   <item>Broad-phase: separate static and dynamic colliders; test dynamic vs all.</item>
///   <item>Narrow-phase: shape-specific intersection (AABB, sphere, box-sphere).</item>
///   <item>Resolution: minimum-translation-vector pushes bodies apart; cancels inward velocity.</item>
///   <item>Publish <see cref="CollisionEvents"/> for consuming systems.</item>
/// </list>
/// </summary>
public sealed class PhysicsSystem : GameSystem
{
    // -------------------------------------------------------------------------
    //  Constants
    // -------------------------------------------------------------------------

    private const float Gravity = 9.81f;

    // -------------------------------------------------------------------------
    //  Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// All collision contacts generated this frame.
    /// Reset at the start of each <see cref="Update"/> call.
    /// </summary>
    public IReadOnlyList<CollisionEvent> CollisionEvents => _events;

    private readonly List<CollisionEvent> _events = new();

    // -------------------------------------------------------------------------
    //  Update
    // -------------------------------------------------------------------------

    public override void Update(float deltaTime)
    {
        _events.Clear();
        IntegrateBodies(deltaTime);
        DetectAndResolveCollisions();
    }

    // =========================================================================
    //  Integration
    // =========================================================================

    private void IntegrateBodies(float dt)
    {
        foreach (var entity in World.Query<RigidBodyComponent, TransformComponent>())
        {
            ref var body      = ref World.GetComponent<RigidBodyComponent>(entity);
            ref var transform = ref World.GetComponent<TransformComponent>(entity);

            if (body.IsKinematic) continue;

            // Gravity
            if (body.UseGravity)
                body.Velocity.Y -= Gravity * dt;

            // Accumulated forces (F = ma → a = F/m)
            if (body.AccumulatedForce != Vector3.Zero)
            {
                body.Velocity        += body.AccumulatedForce / body.Mass * dt;
                body.AccumulatedForce = Vector3.Zero;
            }

            // Linear drag (exponential decay approximation)
            float dragFactor = 1f - MathHelper.Clamp(body.LinearDrag * dt, 0f, 1f);
            body.Velocity *= dragFactor;

            // Integrate position
            transform.Position += body.Velocity * dt;
        }
    }

    // =========================================================================
    //  Collision detection and resolution
    // =========================================================================

    private void DetectAndResolveCollisions()
    {
        // Separate static and dynamic colliders for an efficient sweep.
        // Static vs static pairs are skipped entirely.
        var statics  = new List<ColliderEntry>();
        var dynamics = new List<ColliderEntry>();

        foreach (var entity in World.Query<ColliderComponent, TransformComponent>())
        {
            var col       = World.GetComponent<ColliderComponent>(entity);
            var transform = World.GetComponent<TransformComponent>(entity);

            var entry = new ColliderEntry(entity, col, transform);
            if (col.IsStatic) statics.Add(entry);
            else              dynamics.Add(entry);
        }

        // Dynamic vs static
        foreach (var d in dynamics)
        foreach (var s in statics)
        {
            if (!LayersInteract(d.Collider, s.Collider)) continue;
            if (BroadPhase(d, s) && NarrowPhase(d, s, out var normal, out var depth))
                Dispatch(d, s, normal, depth);
        }

        // Dynamic vs dynamic
        for (int i = 0; i < dynamics.Count; i++)
        for (int j = i + 1; j < dynamics.Count; j++)
        {
            var a = dynamics[i];
            var b = dynamics[j];
            if (!LayersInteract(a.Collider, b.Collider)) continue;
            if (BroadPhase(a, b) && NarrowPhase(a, b, out var normal, out var depth))
                Dispatch(a, b, normal, depth);
        }
    }

    // -------------------------------------------------------------------------
    //  Layer filter
    // -------------------------------------------------------------------------

    private static bool LayersInteract(in ColliderComponent a, in ColliderComponent b) =>
        (a.Layer & b.LayerMask) != 0 || (b.Layer & a.LayerMask) != 0;

    // -------------------------------------------------------------------------
    //  Broad phase — AABB overlap
    // -------------------------------------------------------------------------

    private static bool BroadPhase(in ColliderEntry a, in ColliderEntry b)
    {
        // Use AABB derived from HalfExtents for all shapes (conservative for spheres/capsules)
        var aMin = a.Position - a.Collider.HalfExtents;
        var aMax = a.Position + a.Collider.HalfExtents;
        var bMin = b.Position - b.Collider.HalfExtents;
        var bMax = b.Position + b.Collider.HalfExtents;

        return aMax.X >= bMin.X && aMin.X <= bMax.X
            && aMax.Y >= bMin.Y && aMin.Y <= bMax.Y
            && aMax.Z >= bMin.Z && aMin.Z <= bMax.Z;
    }

    // -------------------------------------------------------------------------
    //  Narrow phase — shape dispatch
    // -------------------------------------------------------------------------

    private static bool NarrowPhase(
        in ColliderEntry a, in ColliderEntry b,
        out Vector3 normal, out float depth)
    {
        var sa = a.Collider.Shape;
        var sb = b.Collider.Shape;

        if (sa == ColliderShape.Box && sb == ColliderShape.Box)
            return TestBoxBox(a, b, out normal, out depth);

        if (sa == ColliderShape.Sphere && sb == ColliderShape.Sphere)
            return TestSphereSphere(a, b, out normal, out depth);

        if (sa == ColliderShape.Box && sb == ColliderShape.Sphere)
            return TestBoxSphere(a, b, out normal, out depth);

        if (sa == ColliderShape.Sphere && sb == ColliderShape.Box)
        {
            bool hit = TestBoxSphere(b, a, out normal, out depth);
            normal = -normal;
            return hit;
        }

        // Capsule or unknown combinations: fall back to conservative box test
        return TestBoxBox(a, b, out normal, out depth);
    }

    // Box vs Box (SAT on 3 axes)
    private static bool TestBoxBox(
        in ColliderEntry a, in ColliderEntry b,
        out Vector3 normal, out float depth)
    {
        var delta = b.Position - a.Position;
        var sumHE = a.Collider.HalfExtents + b.Collider.HalfExtents;
        var pen   = sumHE - new Vector3(MathF.Abs(delta.X), MathF.Abs(delta.Y), MathF.Abs(delta.Z));

        if (pen.X <= 0f || pen.Y <= 0f || pen.Z <= 0f)
        {
            normal = Vector3.Zero;
            depth  = 0f;
            return false;
        }

        // Minimum penetration axis
        if (pen.X <= pen.Y && pen.X <= pen.Z)
        {
            normal = new Vector3(delta.X < 0f ? -1f : 1f, 0f, 0f);
            depth  = pen.X;
        }
        else if (pen.Y <= pen.X && pen.Y <= pen.Z)
        {
            normal = new Vector3(0f, delta.Y < 0f ? -1f : 1f, 0f);
            depth  = pen.Y;
        }
        else
        {
            normal = new Vector3(0f, 0f, delta.Z < 0f ? -1f : 1f);
            depth  = pen.Z;
        }

        return true;
    }

    // Sphere vs Sphere
    private static bool TestSphereSphere(
        in ColliderEntry a, in ColliderEntry b,
        out Vector3 normal, out float depth)
    {
        var   delta    = b.Position - a.Position;
        float distSq   = delta.LengthSquared();
        float radSum   = a.Collider.Radius + b.Collider.Radius;

        if (distSq >= radSum * radSum)
        {
            normal = Vector3.UnitY;
            depth  = 0f;
            return false;
        }

        float dist = MathF.Sqrt(distSq);
        normal = dist > 1e-6f ? delta / dist : Vector3.UnitY;
        depth  = radSum - dist;
        return true;
    }

    // Box vs Sphere — closest point on AABB to sphere center
    private static bool TestBoxSphere(
        in ColliderEntry box, in ColliderEntry sphere,
        out Vector3 normal, out float depth)
    {
        var closest = Vector3.Clamp(
            sphere.Position,
            box.Position - box.Collider.HalfExtents,
            box.Position + box.Collider.HalfExtents);

        var   delta  = sphere.Position - closest;
        float distSq = delta.LengthSquared();
        float r      = sphere.Collider.Radius;

        if (distSq >= r * r)
        {
            normal = Vector3.UnitY;
            depth  = 0f;
            return false;
        }

        float dist = MathF.Sqrt(distSq);
        normal = dist > 1e-6f ? delta / dist : Vector3.UnitY;
        depth  = r - dist;
        return true;
    }

    // -------------------------------------------------------------------------
    //  Event dispatch and resolution
    // -------------------------------------------------------------------------

    private void Dispatch(
        in ColliderEntry a, in ColliderEntry b,
        Vector3 normal, float depth)
    {
        bool isTrigger = a.Collider.IsTrigger || b.Collider.IsTrigger;
        _events.Add(new CollisionEvent(a.Entity, b.Entity, normal, depth, isTrigger));

        if (!isTrigger)
            Resolve(a, b, normal, depth);
    }

    private void Resolve(
        in ColliderEntry a, in ColliderEntry b,
        Vector3 normal, float depth)
    {
        bool aHasBody = World.HasComponent<RigidBodyComponent>(a.Entity);
        bool bHasBody = World.HasComponent<RigidBodyComponent>(b.Entity);

        Vector3 correction = normal * depth;

        // A is static or kinematic — push B
        if (a.Collider.IsStatic || (aHasBody && World.GetComponent<RigidBodyComponent>(a.Entity).IsKinematic))
        {
            ref var tb = ref World.GetComponent<TransformComponent>(b.Entity);
            tb.Position += correction;

            if (bHasBody)
            {
                ref var rb = ref World.GetComponent<RigidBodyComponent>(b.Entity);
                CancelInward(ref rb.Velocity, normal);
            }
            return;
        }

        // B is static or kinematic — push A
        if (b.Collider.IsStatic || (bHasBody && World.GetComponent<RigidBodyComponent>(b.Entity).IsKinematic))
        {
            ref var ta = ref World.GetComponent<TransformComponent>(a.Entity);
            ta.Position -= correction;

            if (aHasBody)
            {
                ref var ra = ref World.GetComponent<RigidBodyComponent>(a.Entity);
                CancelInward(ref ra.Velocity, -normal);
            }
            return;
        }

        // Both dynamic — split correction proportionally by inverse mass
        float mA = aHasBody ? World.GetComponent<RigidBodyComponent>(a.Entity).Mass : 1f;
        float mB = bHasBody ? World.GetComponent<RigidBodyComponent>(b.Entity).Mass : 1f;
        float invTotal = 1f / (mA + mB);

        ref var ta2 = ref World.GetComponent<TransformComponent>(a.Entity);
        ref var tb2 = ref World.GetComponent<TransformComponent>(b.Entity);
        ta2.Position -= correction * (mB * invTotal);
        tb2.Position += correction * (mA * invTotal);

        if (aHasBody)
        {
            ref var ra2 = ref World.GetComponent<RigidBodyComponent>(a.Entity);
            CancelInward(ref ra2.Velocity, -normal);
        }
        if (bHasBody)
        {
            ref var rb2 = ref World.GetComponent<RigidBodyComponent>(b.Entity);
            CancelInward(ref rb2.Velocity, normal);
        }
    }

    /// <summary>
    /// Zeroes any velocity component pointing against <paramref name="normal"/>
    /// (prevents a body from tunneling through a surface after being pushed out).
    /// </summary>
    private static void CancelInward(ref Vector3 velocity, Vector3 normal)
    {
        float vn = Vector3.Dot(velocity, normal);
        if (vn < 0f) velocity -= normal * vn;
    }

    // =========================================================================
    //  Helper record
    // =========================================================================

    /// <summary>Snapshot of a collider's state for the current frame.</summary>
    private readonly record struct ColliderEntry(
        Entity Entity, ColliderComponent Collider, TransformComponent Transform)
    {
        /// <summary>World-space center of this collider.</summary>
        public Vector3 Position => Transform.Position;
    }
}
