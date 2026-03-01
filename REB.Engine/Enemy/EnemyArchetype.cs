namespace REB.Engine.Enemy;

/// <summary>
/// Defines the combat role and base stat profile of an enemy entity.
/// Resolved by <see cref="Systems.EnemyAISystem"/> to select movement speed,
/// damage, health, and attack range values via <see cref="Components.EnemyAIComponent"/> factories.
/// </summary>
public enum EnemyArchetype
{
    /// <summary>Balanced melee combatant. Patrols hallways and chases on line-of-sight.</summary>
    Guard,

    /// <summary>Ranged attacker. Keeps distance and fires from range.</summary>
    Archer,

    /// <summary>Slow, high-health melee tank. Applies heavy knockback on hit.</summary>
    Brute,
}
