namespace REB.Engine.Tavern;

/// <summary>
/// Identifies every purchasable upgrade in the Tavern tree.
/// Values are used as bit-positions in <see cref="Components.UpgradeTreeComponent.PurchasedFlags"/>,
/// so they must stay in the range [0, 63].
/// </summary>
public enum UpgradeId
{
    None = 0,

    // ── Gear — Harness ───────────────────────────────────────────────────────
    HarnessSpeed1 = 1,   // carry walk-speed +10 %
    HarnessSpeed2 = 2,   // carry walk-speed +10 % (prereq: Speed1)
    HarnessGrip1  = 3,   // drop resistance +30 %
    HarnessGrip2  = 4,   // drop resistance +30 % (prereq: Grip1)

    // ── Gear — Combat ────────────────────────────────────────────────────────
    WeaponDamage1 = 5,   // damage ×1.15
    WeaponDamage2 = 6,   // damage ×1.15 more (prereq: Damage1)
    Armor1        = 7,   // max health +20
    Armor2        = 8,   // max health +20 (prereq: Armor1)

    // ── Gear — Tools ─────────────────────────────────────────────────────────
    Lockpick      = 9,   // unlock locked rooms
    GrapplingHook = 10,  // traversal tool

    // ── Abilities ────────────────────────────────────────────────────────────
    SprintBoost1  = 11,  // run speed +10 %
    SprintBoost2  = 12,  // run speed +10 % (prereq: Sprint1)
    RoleHaste     = 13,  // role ability cooldown −25 %
    QuickDraw     = 14,  // attack cooldown −20 %

    // ── Bribes ───────────────────────────────────────────────────────────────
    AdvisorContact  = 15, // enables BribeAdvisor negotiation choice
    CourtInformant  = 16, // +5 % base disposition at negotiation start
    RoyalSpy        = 17, // reveals King's reaction before the scene

    // ── Unlocks ──────────────────────────────────────────────────────────────
    ExtraInventorySlot = 18, // +2 inventory item slots
    SecretPassage      = 19, // enables secret-room access
    TreasureMap        = 20, // reveals loot-container locations
}
