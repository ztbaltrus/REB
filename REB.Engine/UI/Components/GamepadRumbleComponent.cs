using REB.Engine.ECS;

namespace REB.Engine.UI.Components;

/// <summary>
/// One-shot gamepad rumble descriptor.
/// Attach to any entity; <see cref="Systems.HitFeedbackSystem"/> creates these
/// automatically for each <see cref="HitFeedbackEvent"/>.
/// The runtime audio/input layer reads TimeRemaining &gt; 0 to drive the motor.
/// </summary>
public struct GamepadRumbleComponent : IComponent
{
    /// <summary>Low-frequency (heavy) motor strength in [0, 1].</summary>
    public float LowFrequency;

    /// <summary>High-frequency (light) motor strength in [0, 1].</summary>
    public float HighFrequency;

    /// <summary>Total rumble duration in seconds.</summary>
    public float Duration;

    /// <summary>Seconds remaining (ticked down by HitFeedbackSystem).</summary>
    public float TimeRemaining;

    /// <summary>Slot index (0–3) of the player whose controller should rumble.</summary>
    public int PlayerId;

    public static GamepadRumbleComponent Default => default;
}
