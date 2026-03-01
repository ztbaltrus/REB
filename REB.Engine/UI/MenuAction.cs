namespace REB.Engine.UI;

/// <summary>
/// Player-initiated actions that the <see cref="Systems.MenuSystem"/> reacts to.
/// </summary>
public enum MenuAction
{
    None,
    Select,
    Back,
    NavigateUp,
    NavigateDown,
    NavigateLeft,
    NavigateRight,
    Start,
    Pause,
    Confirm,
    Cancel,
}
