using REB.Engine.ECS;

namespace REB.Engine.UI.Components;

/// <summary>
/// Singleton screen-shake state, attached to an entity tagged "ScreenShake".
/// <see cref="Systems.ScreenShakeSystem"/> drives this component; call
/// <c>ScreenShakeSystem.Trigger(intensity, duration)</c> to start a shake.
/// </summary>
public struct ScreenShakeComponent : IComponent
{
    /// <summary>Peak pixel-space offset magnitude.</summary>
    public float Intensity;

    /// <summary>Full shake duration in seconds.</summary>
    public float Duration;

    /// <summary>Seconds remaining in the current shake (counts down to 0).</summary>
    public float TimeRemaining;

    /// <summary>Current horizontal shake offset (updated each frame).</summary>
    public float OffsetX;

    /// <summary>Current vertical shake offset (updated each frame).</summary>
    public float OffsetY;

    public static ScreenShakeComponent Default => default;
}
