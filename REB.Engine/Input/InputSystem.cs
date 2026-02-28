using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using REB.Engine.ECS;

namespace REB.Engine.Input;

/// <summary>
/// Polls keyboard, mouse, and up to four gamepads every frame and exposes clean
/// "pressed / held / released" helpers that other systems can query via
/// <c>World.GetSystem&lt;InputSystem&gt;()</c>.
/// <para>
/// When constructed with a <see cref="GameWindow"/> the cursor is locked to the
/// window centre each frame, so <see cref="MouseDelta"/> never saturates at the
/// window edge. Pass <c>null</c> (or use the default constructor) to disable
/// locking (e.g. in menus or headless tests).
/// </para>
/// </summary>
public sealed class InputSystem : GameSystem
{
    private readonly GameWindow? _window;

    private KeyboardState _kb,     _prevKb;
    private MouseState    _mouse,  _prevMouse;
    private readonly GamePadState[] _pad     = new GamePadState[4];
    private readonly GamePadState[] _prevPad = new GamePadState[4];

    public InputSystem(GameWindow? window = null)
    {
        _window = window;
    }

    // -------------------------------------------------------------------------
    //  Update
    // -------------------------------------------------------------------------

    public override void Update(float deltaTime)
    {
        _prevKb    = _kb;
        _prevMouse = _mouse;
        for (int i = 0; i < 4; i++) _prevPad[i] = _pad[i];

        _kb    = Keyboard.GetState();
        _mouse = Mouse.GetState();
        for (int i = 0; i < 4; i++)
            _pad[i] = GamePad.GetState((PlayerIndex)i);

        // ── Cursor lock ───────────────────────────────────────────────────────
        // Warp the OS cursor back to the window centre after reading the delta.
        // Then replace _mouse with a synthetic state at the centre so that the
        // NEXT frame's _prevMouse baseline is the centre — not the window edge.
        if (_window != null)
        {
            int cx = _window.ClientBounds.Width  / 2;
            int cy = _window.ClientBounds.Height / 2;
            Mouse.SetPosition(cx, cy);
            _mouse = new MouseState(cx, cy,
                _mouse.ScrollWheelValue,
                _mouse.LeftButton, _mouse.MiddleButton, _mouse.RightButton,
                _mouse.XButton1,   _mouse.XButton2);
        }
    }

    // =========================================================================
    //  Keyboard
    // =========================================================================

    public bool IsKeyDown(Keys key)     => _kb.IsKeyDown(key);
    public bool IsKeyUp(Keys key)       => _kb.IsKeyUp(key);
    public bool IsKeyPressed(Keys key)  => _kb.IsKeyDown(key)  && _prevKb.IsKeyUp(key);
    public bool IsKeyReleased(Keys key) => _kb.IsKeyUp(key)    && _prevKb.IsKeyDown(key);

    /// <summary>All keys currently held down.</summary>
    public Keys[] HeldKeys => _kb.GetPressedKeys();

    // =========================================================================
    //  Mouse
    // =========================================================================

    public Point     MousePosition  => new(_mouse.X, _mouse.Y);
    public int       ScrollDelta    => _mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
    public Vector2   MouseDelta     => new(_mouse.X - _prevMouse.X, _mouse.Y - _prevMouse.Y);

    public bool IsLeftButtonDown()     => _mouse.LeftButton   == ButtonState.Pressed;
    public bool IsLeftButtonPressed()  => _mouse.LeftButton   == ButtonState.Pressed
                                       && _prevMouse.LeftButton == ButtonState.Released;
    public bool IsLeftButtonReleased() => _mouse.LeftButton   == ButtonState.Released
                                       && _prevMouse.LeftButton == ButtonState.Pressed;

    public bool IsRightButtonDown()     => _mouse.RightButton   == ButtonState.Pressed;
    public bool IsRightButtonPressed()  => _mouse.RightButton   == ButtonState.Pressed
                                        && _prevMouse.RightButton == ButtonState.Released;
    public bool IsRightButtonReleased() => _mouse.RightButton   == ButtonState.Released
                                        && _prevMouse.RightButton == ButtonState.Pressed;

    public bool IsMiddleButtonDown()     => _mouse.MiddleButton   == ButtonState.Pressed;
    public bool IsMiddleButtonPressed()  => _mouse.MiddleButton   == ButtonState.Pressed
                                         && _prevMouse.MiddleButton == ButtonState.Released;
    public bool IsMiddleButtonReleased() => _mouse.MiddleButton   == ButtonState.Released
                                         && _prevMouse.MiddleButton == ButtonState.Pressed;

    // =========================================================================
    //  Gamepad
    // =========================================================================

    public bool IsConnected(PlayerIndex player) => _pad[(int)player].IsConnected;

    public bool IsButtonDown(PlayerIndex player,     Buttons button) =>
        _pad[(int)player].IsButtonDown(button);

    public bool IsButtonPressed(PlayerIndex player,  Buttons button) =>
        _pad[(int)player].IsButtonDown(button) &&
        _prevPad[(int)player].IsButtonUp(button);

    public bool IsButtonReleased(PlayerIndex player, Buttons button) =>
        _pad[(int)player].IsButtonUp(button) &&
        _prevPad[(int)player].IsButtonDown(button);

    /// <summary>Left thumbstick axis, normalized [-1, 1].</summary>
    public Vector2 LeftStick(PlayerIndex player)  => _pad[(int)player].ThumbSticks.Left;

    /// <summary>Right thumbstick axis, normalized [-1, 1].</summary>
    public Vector2 RightStick(PlayerIndex player) => _pad[(int)player].ThumbSticks.Right;

    /// <summary>Left trigger value [0, 1].</summary>
    public float LeftTrigger(PlayerIndex player)  => _pad[(int)player].Triggers.Left;

    /// <summary>Right trigger value [0, 1].</summary>
    public float RightTrigger(PlayerIndex player) => _pad[(int)player].Triggers.Right;

    // =========================================================================
    //  Raw state access
    // =========================================================================

    public KeyboardState  KeyboardState              => _kb;
    public MouseState     MouseState                 => _mouse;
    public GamePadState   GetGamePadState(PlayerIndex p) => _pad[(int)p];
}
