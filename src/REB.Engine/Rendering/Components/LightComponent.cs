using Microsoft.Xna.Framework;
using REB.Engine.ECS;

namespace REB.Engine.Rendering.Components;

/// <summary>Type of light emitted by a <see cref="LightComponent"/>.</summary>
public enum LightType : byte
{
    /// <summary>Flat ambient fill; no direction, no position.</summary>
    Ambient     = 0,

    /// <summary>Parallel rays from infinite distance (sun/moon). Requires a TransformComponent for direction.</summary>
    Directional = 1,

    /// <summary>Omnidirectional point source. Requires a TransformComponent for position.</summary>
    Point       = 2,

    /// <summary>Cone-shaped spot light. Requires a TransformComponent for position and direction.</summary>
    Spot        = 3,
}

/// <summary>
/// Defines a light source in the scene.
/// Must be paired with a <see cref="TransformComponent"/> for
/// positional and directional light types.
/// <para>
/// The <see cref="Systems.LightingSystem"/> reads these each frame and converts
/// them into data the <see cref="Systems.RenderSystem"/> applies to BasicEffect.
/// </para>
/// </summary>
public struct LightComponent : IComponent
{
    /// <summary>Category of light behaviour.</summary>
    public LightType Type;

    /// <summary>Diffuse color of the light (linear RGB, 0–1 per channel).</summary>
    public Vector3 Color;

    /// <summary>Intensity multiplier applied to <see cref="Color"/>.</summary>
    public float Intensity;

    /// <summary>
    /// Maximum world-space radius of influence.
    /// Used for Point and Spot lights; ignored for Ambient and Directional.
    /// </summary>
    public float Range;

    /// <summary>Inner cone half-angle in radians (Spot lights only). Fully-lit inside cone.</summary>
    public float SpotInnerAngle;

    /// <summary>Outer cone half-angle in radians (Spot lights only). Fully-dark outside cone.</summary>
    public float SpotOuterAngle;

    /// <summary>When false, the light is ignored by the lighting system.</summary>
    public bool IsActive;

    // -------------------------------------------------------------------------
    //  Factory presets
    // -------------------------------------------------------------------------

    public static LightComponent DefaultAmbient => new()
    {
        Type      = LightType.Ambient,
        Color     = new Vector3(0.2f, 0.2f, 0.25f),
        Intensity = 1f,
        IsActive  = true,
    };

    public static LightComponent DefaultDirectional => new()
    {
        Type      = LightType.Directional,
        Color     = new Vector3(1f, 0.95f, 0.85f),
        Intensity = 0.8f,
        IsActive  = true,
    };

    public static LightComponent Torch(float range = 8f) => new()
    {
        Type      = LightType.Point,
        Color     = new Vector3(1f, 0.6f, 0.2f),
        Intensity = 1.2f,
        Range     = range,
        IsActive  = true,
    };
}
