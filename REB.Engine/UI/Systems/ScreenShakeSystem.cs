using REB.Engine.ECS;
using REB.Engine.UI.Components;

namespace REB.Engine.UI.Systems;

/// <summary>
/// Drives the camera-shake effect stored in <see cref="ScreenShakeComponent"/>.
/// <para>
/// Call <see cref="Trigger"/> to start a shake. Each frame the system ticks
/// <c>TimeRemaining</c> down, computes a sin-wave offset that fades linearly to
/// zero, and writes <c>OffsetX</c> / <c>OffsetY</c> onto the singleton entity
/// tagged "ScreenShake".
/// </para>
/// </summary>
public sealed class ScreenShakeSystem : GameSystem
{
    // Accumulated shake request for this frame.
    private float _pendingIntensity;
    private float _pendingDuration;
    private bool  _hasPending;

    // Current shake state (mirrors ScreenShakeComponent for the no-entity path).
    private float _intensity;
    private float _duration;
    private float _timeRemaining;
    private float _phase;          // advances over time to produce sin-wave

    /// <summary>Current horizontal offset in pixels (positive = right).</summary>
    public float OffsetX { get; private set; }

    /// <summary>Current vertical offset in pixels (positive = down).</summary>
    public float OffsetY { get; private set; }

    /// <summary>True while a shake effect is actively decaying.</summary>
    public bool IsShaking => _timeRemaining > 0f;

    /// <summary>
    /// Starts a screen shake. If a shake is already active the highest-intensity
    /// request wins for intensity; duration extends to whichever is longer.
    /// </summary>
    public void Trigger(float intensity, float duration)
    {
        if (!_hasPending)
        {
            _pendingIntensity = intensity;
            _pendingDuration  = duration;
        }
        else
        {
            _pendingIntensity = MathF.Max(_pendingIntensity, intensity);
            _pendingDuration  = MathF.Max(_pendingDuration,  duration);
        }
        _hasPending = true;
    }

    public override void Update(float deltaTime)
    {
        // Apply pending shake request.
        if (_hasPending)
        {
            if (_pendingIntensity >= _intensity)
                _intensity = _pendingIntensity;

            _timeRemaining = MathF.Max(_timeRemaining, _pendingDuration);
            _duration      = MathF.Max(_duration,      _pendingDuration);

            _pendingIntensity = 0f;
            _pendingDuration  = 0f;
            _hasPending       = false;
        }

        // Tick shake.
        if (_timeRemaining > 0f)
        {
            _timeRemaining -= deltaTime;
            _phase         += deltaTime * 60f;   // 60 oscillations per second

            float fade   = MathF.Max(0f, _timeRemaining / (_duration > 0f ? _duration : 1f));
            float offset = _intensity * fade * MathF.Sin(_phase);

            OffsetX = offset;
            OffsetY = offset * 0.6f;   // slight asymmetry between axes

            if (_timeRemaining <= 0f)
            {
                _timeRemaining = 0f;
                _intensity     = 0f;
                _duration      = 0f;
                _phase         = 0f;
                OffsetX        = 0f;
                OffsetY        = 0f;
            }
        }

        // Write to singleton entity if present.
        foreach (var e in World.GetEntitiesWithTag("ScreenShake"))
        {
            if (!World.HasComponent<ScreenShakeComponent>(e)) break;

            ref var sc = ref World.GetComponent<ScreenShakeComponent>(e);
            sc.TimeRemaining = _timeRemaining;
            sc.Intensity     = _intensity;
            sc.Duration      = _duration;
            sc.OffsetX       = OffsetX;
            sc.OffsetY       = OffsetY;
            break;
        }
    }
}
