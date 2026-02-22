using Microsoft.Xna.Framework;
using REB.Engine.ECS;

namespace REB.Engine.Rendering.Components;

/// <summary>
/// Position, rotation, and scale of an entity in 3D world space.
/// The <c>WorldMatrix</c> field caches the combined transform; it is recomputed
/// by the rendering system before each draw.
/// </summary>
public struct TransformComponent : IComponent
{
    public Vector3    Position;
    public Quaternion Rotation;
    public Vector3    Scale;

    /// <summary>Cached world-space transform matrix. Updated each frame by the render pipeline.</summary>
    public Matrix WorldMatrix;

    public static TransformComponent Default => new()
    {
        Position    = Vector3.Zero,
        Rotation    = Quaternion.Identity,
        Scale       = Vector3.One,
        WorldMatrix = Matrix.Identity,
    };

    /// <summary>Recomputes <see cref="WorldMatrix"/> from the current Position/Rotation/Scale.</summary>
    public void Recompute() =>
        WorldMatrix = Matrix.CreateScale(Scale)
                    * Matrix.CreateFromQuaternion(Rotation)
                    * Matrix.CreateTranslation(Position);

    /// <summary>Unit vector pointing forward in local space, transformed to world space.</summary>
    public Vector3 Forward => Vector3.Transform(Vector3.Forward, Rotation);

    /// <summary>Unit vector pointing up in local space, transformed to world space.</summary>
    public Vector3 Up => Vector3.Transform(Vector3.Up, Rotation);

    /// <summary>Unit vector pointing right in local space, transformed to world space.</summary>
    public Vector3 Right => Vector3.Transform(Vector3.Right, Rotation);
}
