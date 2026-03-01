using REB.Engine.ECS;

namespace REB.Engine.Combat.Components;

/// <summary>
/// Tracks the hit-points of any entity that can take damage.
/// Damage is applied by <see cref="Systems.CombatSystem"/> and
/// <see cref="REB.Engine.Hazards.Systems.TrapTriggerSystem"/>.
/// Death is handled by <see cref="Systems.DeathSystem"/>.
/// </summary>
public struct HealthComponent : IComponent
{
    /// <summary>Maximum hit-points (used for phase calculations and UI bars).</summary>
    public float MaxHealth;

    /// <summary>Current hit-points. Clamped to [0, MaxHealth] by all damage-dealing systems.</summary>
    public float CurrentHealth;

    /// <summary>Set when CurrentHealth reaches 0; read by DeathSystem to despawn the entity.</summary>
    public bool IsDead;

    /// <summary>When true this entity is immune to all damage.</summary>
    public bool IsInvulnerable;

    /// <summary>Current health as a fraction of MaxHealth in [0, 1].</summary>
    public float HealthFraction => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;

    public static HealthComponent Default => new()
    {
        MaxHealth      = 100f,
        CurrentHealth  = 100f,
        IsDead         = false,
        IsInvulnerable = false,
    };

    /// <summary>Creates a HealthComponent at full health with the given maximum.</summary>
    public static HealthComponent For(float max) => new()
    {
        MaxHealth      = max,
        CurrentHealth  = max,
        IsDead         = false,
        IsInvulnerable = false,
    };
}
