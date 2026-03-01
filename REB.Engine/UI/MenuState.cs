namespace REB.Engine.UI;

/// <summary>
/// High-level menu / scene states managed by <see cref="Systems.MenuSystem"/>.
/// </summary>
public enum MenuState
{
    /// <summary>Top-level title screen.</summary>
    MainMenu,

    /// <summary>Pre-run co-op role assignment screen.</summary>
    RoleSelection,

    /// <summary>Pause overlay displayed over the active run.</summary>
    Paused,

    /// <summary>In-run heads-up display (not a modal screen, but a tracked state).</summary>
    HUD,

    /// <summary>Post-run payout results shown after King's Court resolves.</summary>
    RunSummary,

    /// <summary>Audio / video / control settings screen.</summary>
    Settings,

    /// <summary>Credits roll.</summary>
    Credits,
}
