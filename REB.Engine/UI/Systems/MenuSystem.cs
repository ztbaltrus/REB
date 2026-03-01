using REB.Engine.ECS;
using REB.Engine.UI.Components;

namespace REB.Engine.UI.Systems;

/// <summary>
/// Finite-state machine managing the active menu / screen.
/// <para>
/// Call <see cref="RequestNavigation"/> each frame to queue a state change.
/// Navigation events are published in <see cref="NavigationEvents"/> for one frame,
/// then cleared. The <see cref="MenuManagerComponent"/> on the "MenuManager" entity
/// is kept in sync each frame (upsert).
/// </para>
/// </summary>
public sealed class MenuSystem : GameSystem
{
    private MenuState  _currentState  = MenuState.MainMenu;
    private MenuState  _previousState = MenuState.MainMenu;
    private MenuState? _pending;

    private readonly List<MenuNavigationEvent> _navEvents = new();

    /// <summary>Navigation events published this frame. Cleared at the start of each update.</summary>
    public IReadOnlyList<MenuNavigationEvent> NavigationEvents => _navEvents;

    /// <summary>The screen / mode currently active.</summary>
    public MenuState CurrentState  => _currentState;

    /// <summary>The screen that was active before the most recent transition.</summary>
    public MenuState PreviousState => _previousState;

    /// <summary>
    /// Queues a transition to <paramref name="to"/>.
    /// If <paramref name="to"/> equals the current state the request is silently ignored.
    /// Only one pending navigation is stored per frame; the last call wins.
    /// </summary>
    public void RequestNavigation(MenuState to) => _pending = to;

    public override void Update(float deltaTime)
    {
        _navEvents.Clear();

        if (_pending.HasValue)
        {
            var to = _pending.Value;
            _pending = null;

            if (to != _currentState)
            {
                _navEvents.Add(new MenuNavigationEvent(_currentState, to));
                _previousState = _currentState;
                _currentState  = to;
            }
        }

        // Upsert MenuManagerComponent on the singleton entity.
        foreach (var e in World.GetEntitiesWithTag("MenuManager"))
        {
            World.SetComponent(e, new MenuManagerComponent
            {
                CurrentState    = _currentState,
                PreviousState   = _previousState,
                IsTransitioning = _navEvents.Count > 0,
            });
            break;
        }
    }
}
