using Microsoft.Xna.Framework;
using REB.Engine.ECS;

namespace REB.Engine.Rendering.Components;

/// <summary>
/// Defines a perspective camera. Pair with a <see cref="TransformComponent"/> to place it in the world.
/// The render system uses the first entity where <see cref="IsActive"/> is true.
/// </summary>
public struct CameraComponent : IComponent
{
    /// <summary>Vertical field of view in radians.</summary>
    public float FieldOfView;

    /// <summary>Near clip plane distance.</summary>
    public float NearPlane;

    /// <summary>Far clip plane distance.</summary>
    public float FarPlane;

    /// <summary>When true this camera is used as the active view for rendering.</summary>
    public bool IsActive;

    // -------------------------------------------------------------------------
    //  Cached matrices — updated by the render system each frame.
    // -------------------------------------------------------------------------

    public Matrix View;
    public Matrix Projection;

    public static CameraComponent Default => new()
    {
        FieldOfView = MathHelper.PiOver4,
        NearPlane   = 0.1f,
        FarPlane    = 1000f,
        IsActive    = true,
        View        = Matrix.Identity,
        Projection  = Matrix.Identity,
    };
}
