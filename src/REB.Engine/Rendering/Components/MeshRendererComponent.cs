using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using REB.Engine.ECS;

namespace REB.Engine.Rendering.Components;

/// <summary>
/// Attaches a loaded XNA <see cref="Model"/> to an entity for rendering.
/// Requires a <see cref="TransformComponent"/> on the same entity.
/// </summary>
public struct MeshRendererComponent : IComponent
{
    /// <summary>The model to draw. May be null while the asset is still loading.</summary>
    public Model? Model;

    /// <summary>Multiplicative tint applied to all mesh effects.</summary>
    public Color Tint;

    /// <summary>When false the entity is skipped by the render system (but not removed).</summary>
    public bool Visible;

    /// <summary>Draw-order hint. Lower values draw first.</summary>
    public int RenderOrder;

    /// <summary>
    /// Conservative bounding sphere radius used for frustum culling (world units).
    /// Set to the largest diagonal half-extent of the model.
    /// A value of 0 disables per-entity frustum culling (entity is always drawn when Visible).
    /// </summary>
    public float BoundingRadius;

    public static MeshRendererComponent Default => new()
    {
        Model          = null,
        Tint           = Color.White,
        Visible        = true,
        RenderOrder    = 0,
        BoundingRadius = 0f,
    };
}
