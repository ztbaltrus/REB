using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace REB.Engine.Rendering;

/// <summary>
/// Immediate-mode debug drawing utility. Accumulates lines/primitives during Update
/// and flushes them to the GPU as a single batch during Draw.
/// Call <see cref="Initialize"/> once with the <see cref="GraphicsDevice"/>, then
/// draw shapes anywhere in your systems, and call <see cref="Flush"/> at the end of
/// each frame with the active view/projection matrices.
/// </summary>
public static class DebugDraw
{
    private const int MaxLines = 8192;

    private static GraphicsDevice? _device;
    private static BasicEffect?    _effect;
    private static VertexPositionColor[] _vertices = new VertexPositionColor[MaxLines * 2];
    private static int _lineCount;

    public static bool Enabled { get; set; } = true;

    // -------------------------------------------------------------------------
    //  Lifecycle
    // -------------------------------------------------------------------------

    public static void Initialize(GraphicsDevice device)
    {
        _device = device;
        _effect = new BasicEffect(device)
        {
            VertexColorEnabled = true,
            LightingEnabled    = false,
            TextureEnabled     = false,
        };
    }

    public static void Shutdown()
    {
        _effect?.Dispose();
        _effect = null;
    }

    // -------------------------------------------------------------------------
    //  Primitives
    // -------------------------------------------------------------------------

    public static void DrawLine(Vector3 start, Vector3 end, Color color)
    {
        if (!Enabled || _lineCount >= MaxLines) return;
        int i = _lineCount * 2;
        _vertices[i]     = new VertexPositionColor(start, color);
        _vertices[i + 1] = new VertexPositionColor(end,   color);
        _lineCount++;
    }

    public static void DrawRay(Vector3 origin, Vector3 direction, float length, Color color) =>
        DrawLine(origin, origin + direction * length, color);

    public static void DrawBox(BoundingBox box, Color color)
    {
        Vector3 min = box.Min, max = box.Max;

        // Bottom face
        DrawLine(new(min.X, min.Y, min.Z), new(max.X, min.Y, min.Z), color);
        DrawLine(new(max.X, min.Y, min.Z), new(max.X, min.Y, max.Z), color);
        DrawLine(new(max.X, min.Y, max.Z), new(min.X, min.Y, max.Z), color);
        DrawLine(new(min.X, min.Y, max.Z), new(min.X, min.Y, min.Z), color);

        // Top face
        DrawLine(new(min.X, max.Y, min.Z), new(max.X, max.Y, min.Z), color);
        DrawLine(new(max.X, max.Y, min.Z), new(max.X, max.Y, max.Z), color);
        DrawLine(new(max.X, max.Y, max.Z), new(min.X, max.Y, max.Z), color);
        DrawLine(new(min.X, max.Y, max.Z), new(min.X, max.Y, min.Z), color);

        // Vertical edges
        DrawLine(new(min.X, min.Y, min.Z), new(min.X, max.Y, min.Z), color);
        DrawLine(new(max.X, min.Y, min.Z), new(max.X, max.Y, min.Z), color);
        DrawLine(new(max.X, min.Y, max.Z), new(max.X, max.Y, max.Z), color);
        DrawLine(new(min.X, min.Y, max.Z), new(min.X, max.Y, max.Z), color);
    }

    public static void DrawSphere(Vector3 center, float radius, Color color, int segments = 16)
    {
        float step = MathHelper.TwoPi / segments;

        for (int i = 0; i < segments; i++)
        {
            float a0 = i * step, a1 = (i + 1) * step;

            // XY plane
            DrawLine(
                center + new Vector3(MathF.Cos(a0) * radius, MathF.Sin(a0) * radius, 0),
                center + new Vector3(MathF.Cos(a1) * radius, MathF.Sin(a1) * radius, 0),
                color);

            // XZ plane
            DrawLine(
                center + new Vector3(MathF.Cos(a0) * radius, 0, MathF.Sin(a0) * radius),
                center + new Vector3(MathF.Cos(a1) * radius, 0, MathF.Sin(a1) * radius),
                color);

            // YZ plane
            DrawLine(
                center + new Vector3(0, MathF.Cos(a0) * radius, MathF.Sin(a0) * radius),
                center + new Vector3(0, MathF.Cos(a1) * radius, MathF.Sin(a1) * radius),
                color);
        }
    }

    /// <summary>Draws a flat XZ grid centered at <paramref name="origin"/>.</summary>
    public static void DrawGrid(Vector3 origin, int halfExtent, float cellSize, Color color)
    {
        float extent = halfExtent * cellSize;
        for (int i = -halfExtent; i <= halfExtent; i++)
        {
            float offset = i * cellSize;
            DrawLine(
                origin + new Vector3(-extent, 0, offset),
                origin + new Vector3( extent, 0, offset),
                color);
            DrawLine(
                origin + new Vector3(offset, 0, -extent),
                origin + new Vector3(offset, 0,  extent),
                color);
        }
    }

    // -------------------------------------------------------------------------
    //  Flush
    // -------------------------------------------------------------------------

    /// <summary>
    /// Submits all queued debug geometry to the GPU. Call once per frame from your render system,
    /// after the scene is drawn but before Present.
    /// </summary>
    public static void Flush(Matrix view, Matrix projection)
    {
        if (!Enabled || _lineCount == 0 || _effect == null || _device == null) return;

        _effect.View       = view;
        _effect.Projection = projection;
        _effect.World      = Matrix.Identity;

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _device.DrawUserPrimitives(
                PrimitiveType.LineList,
                _vertices,
                0,
                _lineCount);
        }

        _lineCount = 0;
    }
}
