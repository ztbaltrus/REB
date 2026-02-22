using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using REB.Engine.ECS;
using REB.Engine.Rendering.Components;

namespace REB.Engine.Rendering.Systems;

/// <summary>
/// Basic forward render system.
/// Each frame it:
///   1. Finds the active camera and computes view/projection matrices.
///   2. Iterates all entities with a <see cref="MeshRendererComponent"/> and draws their model.
///   3. Flushes the <see cref="DebugDraw"/> overlay.
/// </summary>
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
        // 1 — Find the active camera.
        Matrix view       = Matrix.Identity;
        Matrix projection = Matrix.Identity;
        bool   cameraFound = false;

        foreach (var entity in World.Query<CameraComponent, TransformComponent>())
        {
            ref var cam       = ref World.GetComponent<CameraComponent>(entity);
            ref var transform = ref World.GetComponent<TransformComponent>(entity);

            if (!cam.IsActive) continue;

            transform.Recompute();

            float aspect = _device.Viewport.AspectRatio;

            view = Matrix.CreateLookAt(
                transform.Position,
                transform.Position + transform.Forward,
                transform.Up);

            projection = Matrix.CreatePerspectiveFieldOfView(
                cam.FieldOfView,
                aspect,
                cam.NearPlane,
                cam.FarPlane);

            cam.View       = view;
            cam.Projection = projection;
            cameraFound = true;
            break;
        }

        if (!cameraFound) return;

        // 2 — Draw all visible mesh renderers.
        foreach (var entity in World.Query<MeshRendererComponent, TransformComponent>())
        {
            ref var renderer  = ref World.GetComponent<MeshRendererComponent>(entity);
            ref var transform = ref World.GetComponent<TransformComponent>(entity);

            if (!renderer.Visible || renderer.Model == null) continue;

            transform.Recompute();

            foreach (ModelMesh mesh in renderer.Model.Meshes)
            {
                foreach (var effect in mesh.Effects)
                {
                    if (effect is BasicEffect basic)
                    {
                        basic.World      = transform.WorldMatrix;
                        basic.View       = view;
                        basic.Projection = projection;
                        basic.DiffuseColor = renderer.Tint.ToVector3();
                        basic.EnableDefaultLighting();
                    }
                }
                mesh.Draw();
            }
        }

        // 3 — Flush debug geometry.
        DebugDraw.Flush(view, projection);
    }

    public override void OnShutdown()
    {
        DebugDraw.Shutdown();
    }
}
