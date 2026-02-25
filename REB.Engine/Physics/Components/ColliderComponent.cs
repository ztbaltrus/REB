using Microsoft.Xna.Framework;
using REB.Engine.ECS;

namespace REB.Engine.Physics.Components;

/// <summary>
/// Collision primitive attached to an entity.
/// Must be paired with a <see cref="REB.Engine.Rendering.Components.TransformComponent"/>
/// so the physics system can resolve world-space position.
/// </summary>
public struct ColliderComponent : IComponent
{
    /// <summary>Primitive shape used for intersection tests.</summary>
    public ColliderShape Shape;

    /// <summary>
    /// Shape dimensions (world units):
    /// <list type="bullet">
    ///   <item><c>Box</c>: half-extents along X, Y, Z.</item>
    ///   <item><c>Sphere</c>: radius in X; Y and Z are ignored.</item>
    ///   <item><c>Capsule</c>: radius in X, cylindrical half-height in Y.</item>
    /// </list>
    /// </summary>
    public Vector3 HalfExtents;

    /// <summary>The collision layer this entity belongs to.</summary>
    public CollisionLayer Layer;

    /// <summary>Bitmask of layers this collider should test against.</summary>
    public CollisionLayer LayerMask;

    /// <summary>
    /// When true this collider fires <see cref="REB.Engine.Physics.CollisionEvent"/>s
    /// but does not push other bodies apart.
    /// </summary>
    public bool IsTrigger;

    /// <summary>
    /// When true the collider never moves (treated as infinite mass).
    /// Static bodies are excluded from gravity integration and dynamic resolution.
    /// </summary>
    public bool IsStatic;

    // -------------------------------------------------------------------------
    //  Derived helpers
    // -------------------------------------------------------------------------

    /// <summary>Sphere/capsule radius shorthand (<c>HalfExtents.X</c>).</summary>
    public float Radius { get => HalfExtents.X; set => HalfExtents.X = value; }

    /// <summary>Capsule cylindrical half-height shorthand (<c>HalfExtents.Y</c>).</summary>
    public float CapsuleHalfHeight { get => HalfExtents.Y; set => HalfExtents.Y = value; }

    // -------------------------------------------------------------------------
    //  Factory presets
    // -------------------------------------------------------------------------

    public static ColliderComponent Box(
        Vector3        halfExtents,
        CollisionLayer layer    = CollisionLayer.Default,
        CollisionLayer mask     = CollisionLayer.All,
        bool           isStatic = false) => new()
    {
        Shape       = ColliderShape.Box,
        HalfExtents = halfExtents,
        Layer       = layer,
        LayerMask   = mask,
        IsStatic    = isStatic,
    };

    public static ColliderComponent Sphere(
        float          radius,
        CollisionLayer layer    = CollisionLayer.Default,
        CollisionLayer mask     = CollisionLayer.All,
        bool           isStatic = false) => new()
    {
        Shape       = ColliderShape.Sphere,
        HalfExtents = new Vector3(radius, radius, radius),
        Layer       = layer,
        LayerMask   = mask,
        IsStatic    = isStatic,
    };

    public static ColliderComponent Capsule(
        float          radius,
        float          halfHeight,
        CollisionLayer layer    = CollisionLayer.Default,
        CollisionLayer mask     = CollisionLayer.All,
        bool           isStatic = false) => new()
    {
        Shape       = ColliderShape.Capsule,
        HalfExtents = new Vector3(radius, halfHeight, radius),
        Layer       = layer,
        LayerMask   = mask,
        IsStatic    = isStatic,
    };
}
