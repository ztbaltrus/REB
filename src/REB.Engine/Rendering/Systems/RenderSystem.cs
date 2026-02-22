using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using REB.Engine.ECS;
using REB.Engine.Physics.Systems;
using REB.Engine.Rendering.Components;
using REB.Engine.Spatial.Systems;

namespace REB.Engine.Rendering.Systems;

/// <summary>
/// Forward render system.  Each frame it:
/// <list type="number">
///   <item>Locates the active camera and builds view / projection matrices.</item>
///   <item>Retrieves lighting data from <see cref="LightingSystem"/> if registered.</item>
///   <item>Iterates all visible <see cref="MeshRendererComponent"/> entities,
///         applying frustum culling and optional LOD via <see cref="LodComponent"/>.</item>
///   <item>Applies per-entity lighting via BasicEffect.</item>
///   <item>Flushes the <see cref="DebugDraw"/> overlay last.</item>
/// </list>
/// </summary>
[RunAfter(typeof(PhysicsSystem))]
[RunAfter(typeof(LightingSystem))]
[RunAfter(typeof(SpatialSystem))]
public sealed class RenderSystem : GameSystem
{
    private readonly GraphicsDevice _device;

    public RenderSystem(GraphicsDevice device)
    {
        _device = device;
    }

    protected override void OnInitialize()
    {
        DebugDraw.Initialize(_device);
    }

    public override void Draw(GameTime gameTime)
    {
        // ------------------------------------------------------------------
        // 1. Active camera
        // ------------------------------------------------------------------
        Matrix view        = Matrix.Identity;
        Matrix projection  = Matrix.Identity;
        Vector3 cameraPos  = Vector3.Zero;
        bool    cameraFound = false;

        foreach (var entity in World.Query<CameraComponent, TransformComponent>())
        {
            ref var cam       = ref World.GetComponent<CameraComponent>(entity);
            ref var transform = ref World.GetComponent<TransformComponent>(entity);

            if (!cam.IsActive) continue;

            transform.Recompute();
            cameraPos = transform.Position;

            view = Matrix.CreateLookAt(
                transform.Position,
                transform.Position + transform.Forward,
                transform.Up);

            projection = Matrix.CreatePerspectiveFieldOfView(
                cam.FieldOfView,
                _device.Viewport.AspectRatio,
                cam.NearPlane,
                cam.FarPlane);

            cam.View       = view;
            cam.Projection = projection;
            cameraFound    = true;
            break;
        }

        if (!cameraFound) return;

        // ------------------------------------------------------------------
        // 2. Lighting (optional — falls back to BasicEffect defaults)
        // ------------------------------------------------------------------
        World.TryGetSystem<LightingSystem>(out var lighting);

        // ------------------------------------------------------------------
        // 3. View frustum for culling
        // ------------------------------------------------------------------
        var frustum = new BoundingFrustum(view * projection);

        // ------------------------------------------------------------------
        // 4. Draw visible meshes
        // ------------------------------------------------------------------
        foreach (var entity in World.Query<MeshRendererComponent, TransformComponent>())
        {
            ref var renderer  = ref World.GetComponent<MeshRendererComponent>(entity);
            ref var transform = ref World.GetComponent<TransformComponent>(entity);

            if (!renderer.Visible || renderer.Model == null) continue;

            // Frustum cull when a bounding radius is provided.
            if (renderer.BoundingRadius > 0f)
            {
                var sphere = new BoundingSphere(transform.Position, renderer.BoundingRadius);
                if (frustum.Contains(sphere) == ContainmentType.Disjoint) continue;
            }

            // LOD / distance cull when an LodComponent is present.
            if (World.TryGetComponent<LodComponent>(entity, out var lod))
            {
                float dist = Vector3.Distance(transform.Position, cameraPos);

                if (lod.CullDistance > 0f && dist > lod.CullDistance) continue;

                ref var lodRef = ref World.GetComponent<LodComponent>(entity);
                lodRef.IsLowDetail = dist > lod.LodSwitchDistance;
            }

            transform.Recompute();

            foreach (ModelMesh mesh in renderer.Model.Meshes)
            {
                foreach (var effect in mesh.Effects)
                {
                    if (effect is not BasicEffect basic) continue;

                    basic.World        = transform.WorldMatrix;
                    basic.View         = view;
                    basic.Projection   = projection;
                    basic.DiffuseColor = renderer.Tint.ToVector3();

                    ApplyLighting(basic, lighting);
                }
                mesh.Draw();
            }
        }

        // ------------------------------------------------------------------
        // 5. Debug geometry overlay
        // ------------------------------------------------------------------
        DebugDraw.Flush(view, projection);
    }

    public override void OnShutdown()
    {
        DebugDraw.Shutdown();
    }

    // -------------------------------------------------------------------------
    //  Lighting helper
    // -------------------------------------------------------------------------

    private static void ApplyLighting(BasicEffect basic, LightingSystem? lighting)
    {
        if (lighting == null)
        {
            basic.EnableDefaultLighting();
            return;
        }

        basic.LightingEnabled   = true;
        basic.AmbientLightColor = lighting.AmbientColor;

        basic.DirectionalLight0.Enabled      = lighting.Light0.IsActive;
        basic.DirectionalLight0.Direction    = lighting.Light0.Direction;
        basic.DirectionalLight0.DiffuseColor = lighting.Light0.DiffuseColor;

        basic.DirectionalLight1.Enabled      = lighting.Light1.IsActive;
        basic.DirectionalLight1.Direction    = lighting.Light1.Direction;
        basic.DirectionalLight1.DiffuseColor = lighting.Light1.DiffuseColor;

        basic.DirectionalLight2.Enabled      = lighting.Light2.IsActive;
        basic.DirectionalLight2.Direction    = lighting.Light2.Direction;
        basic.DirectionalLight2.DiffuseColor = lighting.Light2.DiffuseColor;
    }
}
