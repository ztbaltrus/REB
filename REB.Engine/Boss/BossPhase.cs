namespace REB.Engine.Boss;

/// <summary>
/// Stages of a boss encounter. Phase transitions trigger new attack patterns and
/// stat multipliers applied by <see cref="Systems.BossSystem"/>.
/// </summary>
public enum BossPhase
{
    /// <summary>Opening phase — standard attack pattern at full health.</summary>
    Phase1,

    /// <summary>Enraged at ≤ 60 % health — faster attacks, higher damage.</summary>
    Phase2,

    /// <summary>Desperate at ≤ 25 % health — maximum aggression and AOE patterns.</summary>
    Phase3,

    /// <summary>Boss is dead. BossSystem publishes a <see cref="BossDefeatedEvent"/> and loot is spawned.</summary>
    Defeated,
}
