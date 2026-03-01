using REB.Engine.ECS;

namespace REB.Engine.UI.Components;

/// <summary>
/// Singleton component storing the active menu state.
/// Attach to an entity tagged "MenuManager"; mutated by <see cref="Systems.MenuSystem"/>.
/// </summary>
public struct MenuManagerComponent : IComponent
{
    /// <summary>The screen / mode currently displayed.</summary>
    public MenuState CurrentState;

    /// <summary>The screen that was active before the most recent transition.</summary>
    public MenuState PreviousState;

    /// <summary>True for exactly one frame while a navigation transition is in progress.</summary>
    public bool IsTransitioning;

    public static MenuManagerComponent Default => new()
    {
        CurrentState  = MenuState.MainMenu,
        PreviousState = MenuState.MainMenu,
        IsTransitioning = false,
    };
}
