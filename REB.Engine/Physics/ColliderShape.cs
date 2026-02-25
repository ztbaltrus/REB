namespace REB.Engine.Physics;

/// <summary>Primitive shape used by a <see cref="Components.ColliderComponent"/>.</summary>
public enum ColliderShape : byte
{
    /// <summary>Axis-aligned box; half-extents along X, Y, Z.</summary>
    Box     = 0,

    /// <summary>Sphere; radius stored in <c>HalfExtents.X</c>.</summary>
    Sphere  = 1,

    /// <summary>
    /// Capsule (cylinder capped with hemispheres); radius in <c>HalfExtents.X</c>,
    /// half-height of the cylindrical section in <c>HalfExtents.Y</c>.
    /// Falls back to box for broad-phase; capsule narrow-phase added in Phase 3.
    /// </summary>
    Capsule = 2,
}
