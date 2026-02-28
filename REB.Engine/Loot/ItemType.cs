namespace REB.Engine.Loot;

/// <summary>Classifies the functional nature of a loot item.</summary>
public enum ItemType
{
    Coin,        // Monetary token; negligible weight, low value.
    Gem,         // Precious stone; light, moderate value.
    Artifact,    // Ancient relic; heavy, high value.
    Weapon,      // Combat equipment; can be used or sold.
    Tool,        // Utility item (lockpick, grapple, torch, etc.).
    Consumable,  // Single-use item with a gameplay effect.
    Cursed,      // Negative effect on the party; penalised at King's payout.
}
