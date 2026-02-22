using System.Diagnostics;
using Microsoft.Xna.Framework;
using REB.Engine.ECS;
using REB.Engine.Rendering.Components;

namespace REB.Engine.Rendering.Systems;

/// <summary>
/// Tracks per-frame timing and exposes rolling-average statistics.
/// <para>
/// A minimal visual indicator is drawn via <see cref="DebugDraw"/> at floor level
/// near the world origin: a green bar when at or under the target frame budget,
/// red when over. Text HUD will be wired in Epic 9 (UI system).
/// </para>
/// </summary>
public sealed class PerformanceOverlaySystem : GameSystem
{
    // -------------------------------------------------------------------------
    //  Configuration
    // -------------------------------------------------------------------------

    /// <summary>Target frame budget in milliseconds (default 16.67 ms = 60 fps).</summary>
    public float TargetFrameMs { get; set; } = 16.67f;

    /// <summary>Number of frames averaged for the rolling statistic.</summary>
    public int SampleWindow { get; set; } = 60;

    // -------------------------------------------------------------------------
    //  Public telemetry
    // -------------------------------------------------------------------------

    /// <summary>Raw duration of the most recent frame in milliseconds.</summary>
    public float LastFrameMs  { get; private set; }

    /// <summary>Rolling average frame duration over <see cref="SampleWindow"/> frames.</summary>
    public float AverageFrameMs { get; private set; }

    /// <summary>Approximate rolling frames-per-second.</summary>
    public float AverageFps => AverageFrameMs > 0f ? 1000f / AverageFrameMs : 0f;

    /// <summary>Total frames elapsed since system initialization.</summary>
    public int FrameCount { get; private set; }

    // -------------------------------------------------------------------------
    //  Private state
    // -------------------------------------------------------------------------

    private readonly Stopwatch _frameTimer = Stopwatch.StartNew();
    private float _accumMs;
    private int   _samples;

    // -------------------------------------------------------------------------
    //  Update
    // -------------------------------------------------------------------------

    public override void Update(float deltaTime)
    {
        float elapsed = (float)_frameTimer.Elapsed.TotalMilliseconds;
        _frameTimer.Restart();

        LastFrameMs = elapsed;
        _accumMs   += elapsed;
        _samples++;
        FrameCount++;

        if (_samples >= SampleWindow)
        {
            AverageFrameMs = _accumMs / _samples;
            _accumMs       = 0f;
            _samples       = 0;
        }
    }

    // -------------------------------------------------------------------------
    //  Draw — simple debug bar at world origin
    // -------------------------------------------------------------------------

    public override void Draw(GameTime gameTime)
    {
        // Draw a bar along the X axis: length proportional to frame time.
        // Green  → at or under budget.
        // Yellow → up to 2× budget.
        // Red    → over 2× budget.
        float ratio = AverageFrameMs / TargetFrameMs;
        var   color = ratio <= 1f ? Color.LimeGreen
                    : ratio <= 2f ? Color.Yellow
                    :               Color.Red;

        const float BarMaxLength = 8f;
        const float BarY         = 0.05f;
        const float BarZ         = -1f;

        float barLength = MathF.Min(ratio, 3f) * (BarMaxLength / 3f);

        // Background (budget reference bar)
        DebugDraw.DrawLine(
            new Vector3(0f,          BarY, BarZ),
            new Vector3(BarMaxLength, BarY, BarZ),
            Color.DarkGray);

        // Frame-time bar
        DebugDraw.DrawLine(
            new Vector3(0f,        BarY + 0.02f, BarZ),
            new Vector3(barLength, BarY + 0.02f, BarZ),
            color);
    }
}
