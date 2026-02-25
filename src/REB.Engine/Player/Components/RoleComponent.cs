using REB.Engine.ECS;

namespace REB.Engine.Player.Components;

/// <summary>
/// Co-op role assigned to a player entity.
/// Role-specific ability activation is handled by
/// <see cref="REB.Engine.Player.Systems.RoleAbilitySystem"/>.
/// </summary>
public struct RoleComponent : IComponent
{
    /// <summary>The chosen co-op role.</summary>
    public PlayerRole Role;

    /// <summary>True when the role ability can be activated (cooldown reached 0).</summary>
    public bool AbilityReady;

    /// <summary>Seconds remaining until the ability is ready again.</summary>
    public float AbilityCooldownRemaining;

    /// <summary>Base cooldown duration in seconds after activating the ability.</summary>
    public float AbilityCooldownDuration;

    public static RoleComponent None => new()
    {
        Role                     = PlayerRole.None,
        AbilityReady             = false,
        AbilityCooldownRemaining = 0f,
        AbilityCooldownDuration  = 10f,
    };
}
