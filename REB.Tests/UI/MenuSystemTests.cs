using REB.Engine.ECS;
using REB.Engine.UI;
using REB.Engine.UI.Components;
using REB.Engine.UI.Systems;
using Xunit;

namespace REB.Tests.UI;

// ---------------------------------------------------------------------------
//  MenuSystem tests
//
//  MenuSystem is a self-contained FSM. No other systems are needed.
// ---------------------------------------------------------------------------

public sealed class MenuSystemTests
{
    private static (World world, MenuSystem menuSystem) BuildWorld()
    {
        var world      = new World();
        var menuSystem = new MenuSystem();
        world.RegisterSystem(menuSystem);
        return (world, menuSystem);
    }

    // -------------------------------------------------------------------------
    //  Initial state
    // -------------------------------------------------------------------------

    [Fact]
    public void InitialState_IsMainMenu()
    {
        var (world, menu) = BuildWorld();

        Assert.Equal(MenuState.MainMenu, menu.CurrentState);
        world.Dispose();
    }

    [Fact]
    public void NavigationEvents_EmptyBeforeFirstRequest()
    {
        var (world, menu) = BuildWorld();

        world.Update(0.016f);

        Assert.Empty(menu.NavigationEvents);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Successful navigation
    // -------------------------------------------------------------------------

    [Fact]
    public void RequestNavigation_ChangesCurrentState()
    {
        var (world, menu) = BuildWorld();

        menu.RequestNavigation(MenuState.RoleSelection);
        world.Update(0.016f);

        Assert.Equal(MenuState.RoleSelection, menu.CurrentState);
        world.Dispose();
    }

    [Fact]
    public void RequestNavigation_PublishesNavigationEvent()
    {
        var (world, menu) = BuildWorld();

        menu.RequestNavigation(MenuState.HUD);
        world.Update(0.016f);

        Assert.Single(menu.NavigationEvents);
        world.Dispose();
    }

    [Fact]
    public void NavigationEvent_HasCorrectFromAndTo()
    {
        var (world, menu) = BuildWorld();

        menu.RequestNavigation(MenuState.Settings);
        world.Update(0.016f);

        var ev = menu.NavigationEvents[0];
        Assert.Equal(MenuState.MainMenu, ev.From);
        Assert.Equal(MenuState.Settings, ev.To);
        world.Dispose();
    }

    [Fact]
    public void PreviousState_UpdatedOnNavigation()
    {
        var (world, menu) = BuildWorld();

        menu.RequestNavigation(MenuState.RoleSelection);
        world.Update(0.016f);

        Assert.Equal(MenuState.MainMenu, menu.PreviousState);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Same-state navigation is ignored
    // -------------------------------------------------------------------------

    [Fact]
    public void RequestNavigation_ToSameState_FiresNoEvent()
    {
        var (world, menu) = BuildWorld();

        // Already in MainMenu.
        menu.RequestNavigation(MenuState.MainMenu);
        world.Update(0.016f);

        Assert.Empty(menu.NavigationEvents);
        world.Dispose();
    }

    [Fact]
    public void RequestNavigation_ToSameState_DoesNotChangePrevious()
    {
        var (world, menu) = BuildWorld();

        menu.RequestNavigation(MenuState.Paused);
        world.Update(0.016f);   // MainMenu → Paused

        menu.RequestNavigation(MenuState.Paused);
        world.Update(0.016f);   // same state — no change

        Assert.Equal(MenuState.MainMenu, menu.PreviousState);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Events cleared each frame
    // -------------------------------------------------------------------------

    [Fact]
    public void NavigationEvents_ClearedEachFrame()
    {
        var (world, menu) = BuildWorld();

        menu.RequestNavigation(MenuState.HUD);
        world.Update(0.016f);
        Assert.Single(menu.NavigationEvents);

        world.Update(0.016f);  // no new request
        Assert.Empty(menu.NavigationEvents);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Chained navigation across frames
    // -------------------------------------------------------------------------

    [Fact]
    public void ChainedNavigation_WorksAcrossFrames()
    {
        var (world, menu) = BuildWorld();

        menu.RequestNavigation(MenuState.RoleSelection);
        world.Update(0.016f);

        menu.RequestNavigation(MenuState.HUD);
        world.Update(0.016f);

        Assert.Equal(MenuState.HUD, menu.CurrentState);
        Assert.Equal(MenuState.RoleSelection, menu.PreviousState);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Multiple requests same frame — last wins
    // -------------------------------------------------------------------------

    [Fact]
    public void MultipleRequestsSameFrame_LastWins()
    {
        var (world, menu) = BuildWorld();

        menu.RequestNavigation(MenuState.RoleSelection);
        menu.RequestNavigation(MenuState.Settings);   // overwrites
        world.Update(0.016f);

        Assert.Equal(MenuState.Settings, menu.CurrentState);
        world.Dispose();
    }

    [Fact]
    public void MultipleRequestsSameFrame_OnlyOneEventFired()
    {
        var (world, menu) = BuildWorld();

        menu.RequestNavigation(MenuState.RoleSelection);
        menu.RequestNavigation(MenuState.Settings);
        world.Update(0.016f);

        Assert.Single(menu.NavigationEvents);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  MenuManager entity sync
    // -------------------------------------------------------------------------

    [Fact]
    public void MenuManagerEntity_ReflectsCurrentState()
    {
        var (world, menu) = BuildWorld();

        // Spawn the singleton.
        var e = world.CreateEntity();
        world.AddTag(e, "MenuManager");
        world.AddComponent(e, MenuManagerComponent.Default);

        menu.RequestNavigation(MenuState.RunSummary);
        world.Update(0.016f);

        var mm = world.GetComponent<MenuManagerComponent>(e);
        Assert.Equal(MenuState.RunSummary, mm.CurrentState);
        world.Dispose();
    }

    [Fact]
    public void MenuManagerEntity_IsTransitioning_TrueOnNavigationFrame()
    {
        var (world, menu) = BuildWorld();

        var e = world.CreateEntity();
        world.AddTag(e, "MenuManager");
        world.AddComponent(e, MenuManagerComponent.Default);

        menu.RequestNavigation(MenuState.Paused);
        world.Update(0.016f);

        Assert.True(world.GetComponent<MenuManagerComponent>(e).IsTransitioning);
        world.Dispose();
    }

    [Fact]
    public void MenuManagerEntity_IsTransitioning_FalseOnIdleFrame()
    {
        var (world, menu) = BuildWorld();

        var e = world.CreateEntity();
        world.AddTag(e, "MenuManager");
        world.AddComponent(e, MenuManagerComponent.Default);

        menu.RequestNavigation(MenuState.Paused);
        world.Update(0.016f);   // transition frame

        world.Update(0.016f);   // idle frame
        Assert.False(world.GetComponent<MenuManagerComponent>(e).IsTransitioning);
        world.Dispose();
    }
}
