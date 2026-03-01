using REB.Engine.ECS;

namespace REB.Engine.Boss.Components;

/// <summary>
/// Attaches a boss encounter to an entity.
/// Phase progression and enrage boosts are managed by <see cref="Systems.BossSystem"/>.
/// Pair with <see cref="REB.Engine.Combat.Components.HealthComponent"/> and
/// <see cref="REB.Engine.Enemy.Components.EnemyAIComponent"/>.
/// </summary>
public struct BossComponent : IComponent
{
    /// <summary>Current boss encounter phase.</summary>
    public BossPhase Phase;

    /// <summary>Health fraction (0–1) at which the boss transitions Phase1 → Phase2.</summary>
    public float Phase2Threshold;

    /// <summary>Health fraction (0–1) at which the boss transitions Phase2 → Phase3.</summary>
    public float Phase3Threshold;

    // ── Single-frame transition flags (cleared at the top of each BossSystem tick) ──

    /// <summary>True only on the frame the boss enters Phase2.</summary>
    public bool Phase2Triggered;

    /// <summary>True only on the frame the boss enters Phase3.</summary>
    public bool Phase3Triggered;

    /// <summary>True only on the frame the boss is defeated.</summary>
    public bool DefeatedThisFrame;

    // ── Enrage modifiers ───────────────────────────────────────────────────────

    /// <summary>Multiplier applied to DamageComponent.Damage when entering Phase2.</summary>
    public float EnrageDamageMultiplier;

    /// <summary>Multiplier applied to EnemyAIComponent run/walk speed when entering Phase2.</summary>
    public float EnrageSpeedMultiplier;

    // ── Loot ──────────────────────────────────────────────────────────────────

    /// <summary>Number of high-value items spawned at the defeat position.</summary>
    public int LootDropCount;

    /// <summary>Seed for deterministic boss loot table generation.</summary>
    public int LootSeed;

    public static BossComponent Default => new()
    {
        Phase                  = BossPhase.Phase1,
        Phase2Threshold        = 0.60f,
        Phase3Threshold        = 0.25f,
        Phase2Triggered        = false,
        Phase3Triggered        = false,
        DefeatedThisFrame      = false,
        EnrageDamageMultiplier = 1.5f,
        EnrageSpeedMultiplier  = 1.3f,
        LootDropCount          = 3,
        LootSeed               = 42,
    };
}
