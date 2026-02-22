using Microsoft.Xna.Framework;
using REB.Engine.ECS;
using REB.Engine.Input;
using REB.Engine.Rendering.Components;

namespace REB.Engine.Rendering.Systems;

/// <summary>
/// Collects all <see cref="LightComponent"/> entities each frame and exposes
/// aggregated data for the <see cref="RenderSystem"/>.
/// <para>
/// BasicEffect supports one ambient color and up to three directional lights.
/// Point and spot lights are approximated as directional contributions from the
/// three nearest sources to the active camera position.
/// </para>
/// </summary>
[RunAfter(typeof(InputSystem))]
public sealed class LightingSystem : GameSystem
{
    // -------------------------------------------------------------------------
    //  Exposed state (read by RenderSystem every frame)
    // -------------------------------------------------------------------------

    /// <summary>Accumulated ambient light color, clamped to [0, 1] per channel.</summary>
    public Vector3 AmbientColor { get; private set; }

    /// <summary>First BasicEffect directional light slot.</summary>
    public DirectionalLightData Light0 { get; private set; }

    /// <summary>Second BasicEffect directional light slot.</summary>
    public DirectionalLightData Light1 { get; private set; }

    /// <summary>Third BasicEffect directional light slot.</summary>
    public DirectionalLightData Light2 { get; private set; }

    // -------------------------------------------------------------------------
    //  Update
    // -------------------------------------------------------------------------

    public override void Update(float deltaTime)
    {
        // Find the active camera position for point-light approximation.
        var cameraPos = Vector3.Zero;
        foreach (var camEnt in World.Query<CameraComponent, TransformComponent>())
        {
            ref var cam = ref World.GetComponent<CameraComponent>(camEnt);
            if (!cam.IsActive) continue;
            cameraPos = World.GetComponent<TransformComponent>(camEnt).Position;
            break;
        }

        // Collect all active lights.
        var ambient      = Vector3.Zero;
        var directionals = new List<(Vector3 Dir, Vector3 Color)>(8);
        var points       = new List<(Vector3 Pos, float Range, Vector3 Color)>(16);

        foreach (var entity in World.Query<LightComponent>())
        {
            ref var light = ref World.GetComponent<LightComponent>(entity);
            if (!light.IsActive) continue;

            Vector3 scaled = light.Color * light.Intensity;

            switch (light.Type)
            {
                case LightType.Ambient:
                    ambient += scaled;
                    break;

                case LightType.Directional:
                    if (World.HasComponent<TransformComponent>(entity))
                    {
                        var dir = World.GetComponent<TransformComponent>(entity).Forward;
                        directionals.Add((dir, scaled));
                    }
                    break;

                case LightType.Point:
                case LightType.Spot:
                    if (World.HasComponent<TransformComponent>(entity))
                    {
                        var pos = World.GetComponent<TransformComponent>(entity).Position;
                        points.Add((pos, MathF.Max(light.Range, 0.001f), scaled));
                    }
                    break;
            }
        }

        // Approximate point lights as directional, sorted by proximity to camera.
        points.Sort((x, y) =>
            Vector3.DistanceSquared(x.Pos, cameraPos)
                .CompareTo(Vector3.DistanceSquared(y.Pos, cameraPos)));

        foreach (var (pos, range, color) in points)
        {
            if (directionals.Count >= 3) break;
            float dist  = Vector3.Distance(pos, cameraPos);
            float atten = 1f - MathHelper.Clamp(dist / range, 0f, 1f);
            if (atten <= 0f) continue;

            // Point toward the camera from the light source
            var dir = dist > 1e-4f
                ? Vector3.Normalize(pos - cameraPos)
                : -Vector3.UnitY;

            directionals.Add((dir, color * atten));
        }

        AmbientColor = Vector3.Min(ambient, Vector3.One);
        Light0 = directionals.Count > 0 ? new DirectionalLightData(directionals[0].Dir, directionals[0].Color) : default;
        Light1 = directionals.Count > 1 ? new DirectionalLightData(directionals[1].Dir, directionals[1].Color) : default;
        Light2 = directionals.Count > 2 ? new DirectionalLightData(directionals[2].Dir, directionals[2].Color) : default;
    }

    // -------------------------------------------------------------------------
    //  Data carrier
    // -------------------------------------------------------------------------

    /// <summary>Direction and diffuse color for a single BasicEffect directional light slot.</summary>
    public readonly record struct DirectionalLightData(Vector3 Direction, Vector3 DiffuseColor)
    {
        /// <summary>True when this slot has a non-zero contribution.</summary>
        public bool IsActive => DiffuseColor.LengthSquared() > 0f;
    }
}
