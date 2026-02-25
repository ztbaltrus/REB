using REB.Engine.ECS;

namespace REB.Engine.Rendering.Components;

/// <summary>
/// Controls level-of-detail behaviour for a rendered entity.
/// The <see cref="Systems.RenderSystem"/> uses these distances to select mesh
/// detail and to cull the entity entirely when beyond <see cref="CullDistance"/>.
/// </summary>
public struct LodComponent : IComponent
{
    /// <summary>
    /// Beyond this camera distance the entity is not drawn at all.
    /// Set to 0 to disable distance culling (entity always drawn if in frustum).
    /// </summary>
    public float CullDistance;

    /// <summary>
    /// Switch from high-detail to low-detail rendering beyond this distance.
    /// The model's last mesh is used as the LOD0 fallback.
    /// </summary>
    public float LodSwitchDistance;

    /// <summary>
    /// Set by the render system each frame.
    /// True when the entity is currently rendering at low detail.
    /// </summary>
    public bool IsLowDetail;

    public static LodComponent Default => new()
    {
        CullDistance     = 200f,
        LodSwitchDistance = 60f,
    };
}
