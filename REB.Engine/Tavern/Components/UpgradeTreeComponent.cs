using REB.Engine.ECS;

namespace REB.Engine.Tavern.Components;

/// <summary>
/// Tracks which Tavern upgrades have been purchased using a 64-bit bitmask.
/// Each <see cref="UpgradeId"/> value (0–63) maps to a bit in
/// <see cref="PurchasedFlags"/>.
/// Placed on the singleton entity tagged <c>"GoldLedger"</c> alongside
/// <see cref="GoldCurrencyComponent"/>.
/// </summary>
public struct UpgradeTreeComponent : IComponent
{
    /// <summary>Bitmask of all purchased upgrades.</summary>
    public ulong PurchasedFlags;

    // =========================================================================
    //  Bit-level helpers
    // =========================================================================

    /// <summary>Returns true if the given upgrade has been purchased.</summary>
    public readonly bool HasUpgrade(UpgradeId id) =>
        id != UpgradeId.None && (PurchasedFlags & (1UL << (int)id)) != 0;

    /// <summary>Marks the upgrade as purchased (idempotent).</summary>
    public void AddUpgrade(UpgradeId id)
    {
        if (id != UpgradeId.None)
            PurchasedFlags |= 1UL << (int)id;
    }

    // =========================================================================
    //  Catalog — static definitions for every upgrade
    // =========================================================================

    public static readonly IReadOnlyDictionary<UpgradeId, UpgradeDefinition> Catalog =
        new Dictionary<UpgradeId, UpgradeDefinition>
        {
            // Gear — Harness
            [UpgradeId.HarnessSpeed1] = new(UpgradeId.HarnessSpeed1, "Swift Harness I",   UpgradeCategory.Gear,       50f),
            [UpgradeId.HarnessSpeed2] = new(UpgradeId.HarnessSpeed2, "Swift Harness II",  UpgradeCategory.Gear,      120f, UpgradeId.HarnessSpeed1),
            [UpgradeId.HarnessGrip1]  = new(UpgradeId.HarnessGrip1,  "Iron Grip I",       UpgradeCategory.Gear,       75f),
            [UpgradeId.HarnessGrip2]  = new(UpgradeId.HarnessGrip2,  "Iron Grip II",      UpgradeCategory.Gear,      150f, UpgradeId.HarnessGrip1),

            // Gear — Combat
            [UpgradeId.WeaponDamage1] = new(UpgradeId.WeaponDamage1, "Sharpened Blade I",  UpgradeCategory.Gear,       80f),
            [UpgradeId.WeaponDamage2] = new(UpgradeId.WeaponDamage2, "Sharpened Blade II", UpgradeCategory.Gear,      160f, UpgradeId.WeaponDamage1),
            [UpgradeId.Armor1]        = new(UpgradeId.Armor1,        "Padded Vest I",      UpgradeCategory.Gear,      100f),
            [UpgradeId.Armor2]        = new(UpgradeId.Armor2,        "Padded Vest II",     UpgradeCategory.Gear,      200f, UpgradeId.Armor1),

            // Gear — Tools
            [UpgradeId.Lockpick]      = new(UpgradeId.Lockpick,      "Master Lockpick",    UpgradeCategory.Gear,       90f),
            [UpgradeId.GrapplingHook] = new(UpgradeId.GrapplingHook, "Grappling Hook",     UpgradeCategory.Gear,      200f),

            // Abilities
            [UpgradeId.SprintBoost1]  = new(UpgradeId.SprintBoost1,  "Fleet Feet I",       UpgradeCategory.Abilities,  60f),
            [UpgradeId.SprintBoost2]  = new(UpgradeId.SprintBoost2,  "Fleet Feet II",      UpgradeCategory.Abilities, 130f, UpgradeId.SprintBoost1),
            [UpgradeId.RoleHaste]     = new(UpgradeId.RoleHaste,     "Role Haste",         UpgradeCategory.Abilities, 150f),
            [UpgradeId.QuickDraw]     = new(UpgradeId.QuickDraw,     "Quick Draw",         UpgradeCategory.Abilities, 100f),

            // Bribes
            [UpgradeId.AdvisorContact]  = new(UpgradeId.AdvisorContact,  "Advisor Contact",   UpgradeCategory.Bribes, 120f),
            [UpgradeId.CourtInformant]  = new(UpgradeId.CourtInformant,  "Court Informant",   UpgradeCategory.Bribes, 180f),
            [UpgradeId.RoyalSpy]        = new(UpgradeId.RoyalSpy,        "Royal Spy",         UpgradeCategory.Bribes, 250f),

            // Unlocks
            [UpgradeId.ExtraInventorySlot] = new(UpgradeId.ExtraInventorySlot, "Extra Satchel",    UpgradeCategory.Unlocks, 100f),
            [UpgradeId.SecretPassage]      = new(UpgradeId.SecretPassage,      "Secret Passage",   UpgradeCategory.Unlocks, 200f),
            [UpgradeId.TreasureMap]        = new(UpgradeId.TreasureMap,        "Treasure Map",     UpgradeCategory.Unlocks, 150f),
        };

    public static UpgradeTreeComponent Default => new() { PurchasedFlags = 0UL };
}
