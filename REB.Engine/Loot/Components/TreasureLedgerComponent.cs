using REB.Engine.ECS;

namespace REB.Engine.Loot.Components;

/// <summary>
/// Session-wide snapshot of collected loot value, updated each frame by
/// LootValuationSystem. Attach this to a dedicated entity tagged "TreasureLedger".
/// </summary>
public struct TreasureLedgerComponent : IComponent
{
    /// <summary>Effective gold value of all items currently held by any player.</summary>
    public int TotalValue;

    /// <summary>Number of Common-rarity items held by the party.</summary>
    public int CommonCount;

    /// <summary>Number of Rare-rarity items held by the party.</summary>
    public int RareCount;

    /// <summary>Number of Legendary-rarity items held by the party.</summary>
    public int LegendaryCount;

    /// <summary>Number of Cursed-rarity items held by the party.</summary>
    public int CursedCount;

    /// <summary>
    /// Slot index (0–3) of the Treasurer player, or −1 when none is present.
    /// A Treasurer grants a 1.5× bonus on Legendary item values (5× → 7.5×).
    /// </summary>
    public int TreasurerId;

    public static TreasureLedgerComponent Default => new() { TreasurerId = -1 };
}
