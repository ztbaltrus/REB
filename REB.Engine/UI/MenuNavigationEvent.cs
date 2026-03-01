namespace REB.Engine.UI;

/// <summary>
/// Published by <see cref="Systems.MenuSystem"/> whenever the active menu state changes.
/// Consumed within the same frame by UI rendering and audio systems.
/// </summary>
public readonly record struct MenuNavigationEvent(MenuState From, MenuState To);
