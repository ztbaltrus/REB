using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using REB.Engine.ECS;

namespace REB.Engine.Player.Components;

/// <summary>
/// Declares which hardware device drives this player entity and caches
/// per-frame action flags that are set by
/// <see cref="REB.Engine.Player.Systems.PlayerControllerSystem"/> and consumed by
/// other systems (e.g. <see cref="REB.Engine.Player.Systems.CarrySystem"/>).
/// </summary>
public struct PlayerInputComponent : IComponent
{
    /// <summary>
    /// When true this player is controlled by keyboard + mouse (slot 0 only).
    /// When false, the <see cref="GamepadSlot"/> gamepad is used instead.
    /// </summary>
    public bool UseKeyboard;

    /// <summary>Which gamepad slot maps to this player (ignored when <see cref="UseKeyboard"/> is true).</summary>
    public PlayerIndex GamepadSlot;

    /// <summary>Additional per-player look sensitivity scale.</summary>
    public float LookSensitivity;

    // -------------------------------------------------------------------------
    //  Per-frame action flags — written by PlayerControllerSystem, read by others.
    // -------------------------------------------------------------------------

    /// <summary>True on the frame the interact button was first pressed.</summary>
    public bool InteractPressed;

    /// <summary>True on the frame the drop button was first pressed.</summary>
    public bool DropPressed;

    /// <summary>True on the frame the camera-view toggle was first pressed.</summary>
    public bool CameraTogglePressed;

    /// <summary>True on the frame the jump button was first pressed.</summary>
    public bool JumpPressed;

    /// <summary>True while the sprint button is held.</summary>
    public bool RunHeld;

    /// <summary>True on the frame the use-item button was first pressed (Q / RightShoulder).</summary>
    public bool UseItemPressed;

    // -------------------------------------------------------------------------
    //  Factory presets
    // -------------------------------------------------------------------------

    /// <summary>Keyboard-and-mouse input for slot 0.</summary>
    public static PlayerInputComponent Keyboard => new()
    {
        UseKeyboard     = true,
        GamepadSlot     = PlayerIndex.One,
        LookSensitivity = 1f,
    };

    /// <summary>Gamepad input for the given slot.</summary>
    public static PlayerInputComponent Gamepad(PlayerIndex slot) => new()
    {
        UseKeyboard     = false,
        GamepadSlot     = slot,
        LookSensitivity = 1f,
    };
}
