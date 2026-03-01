using REB.Engine.ECS;

namespace REB.Engine.Tavern.Components;

/// <summary>
/// Computed upgrade bonuses written to each Player entity every frame by
/// <see cref="Systems.GearUpgradeSystem"/>.
/// Other systems (CarrySystem, CombatSystem) read this component to apply bonuses
/// at the point of use.
/// </summary>
public struct GearBonusComponent : IComponent
{
    // ── Harness bonuses ──────────────────────────────────────────────────────

    /// <summary>
    /// Additive carry walk-speed bonus (0.1 per harness speed tier).
    /// Use: <c>effectiveSpeed = baseSpeed * (1 + CarrySpeedBonus)</c>.
    /// </summary>
    public float CarrySpeedBonus;

    /// <summary>
    /// Probability reduction for accidental drops [0, 1].
    /// 0.3 per grip tier (max 0.6 = 60 % less likely to drop).
    /// </summary>
    public float DropResistanceBonus;

    // ── Combat bonuses ───────────────────────────────────────────────────────

    /// <summary>
    /// Multiplicative damage factor (1.0 = no bonus; +0.15 per weapon tier).
    /// <see cref="Systems.GearUpgradeSystem"/> writes
    /// <c>DamageComponent.Damage = BaseDamage * DamageMultiplier</c>.
    /// </summary>
    public float DamageMultiplier;

    /// <summary>Flat max-health addition (20 HP per armor tier).</summary>
    public float MaxHealthBonus;

    // ── Snapshot of base values (captured on first encounter) ────────────────

    /// <summary>Original <c>HealthComponent.MaxHealth</c> before any armor upgrades.</summary>
    public float BaseMaxHealth;

    /// <summary>Whether <see cref="BaseMaxHealth"/> has been initialized from the entity's health component.</summary>
    public bool BaseMaxHealthInitialized;

    /// <summary>Original <c>DamageComponent.Damage</c> before any weapon upgrades.</summary>
    public float BaseDamage;

    /// <summary>Whether <see cref="BaseDamage"/> has been initialized from the entity's damage component.</summary>
    public bool BaseDamageInitialized;

    // ── Tool unlocks ─────────────────────────────────────────────────────────

    /// <summary>True if the Lockpick upgrade has been purchased.</summary>
    public bool HasLockpick;

    /// <summary>True if the Grappling Hook upgrade has been purchased.</summary>
    public bool HasGrapplingHook;

    // ── Speed bonuses ────────────────────────────────────────────────────────

    /// <summary>Additive sprint-speed bonus (0.1 per sprint tier).</summary>
    public float SprintSpeedBonus;

    public static GearBonusComponent Default => new() { DamageMultiplier = 1.0f };
}
