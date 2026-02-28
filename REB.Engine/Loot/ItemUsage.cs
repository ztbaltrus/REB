namespace REB.Engine.Loot;

/// <summary>Determines how a player can activate an item.</summary>
public enum ItemUsage
{
    Passive,     // No active trigger; contributes value only.
    Active,      // Activated on command; enters a cooldown after use.
    Consumable,  // One-time use; entity is destroyed after activation.
}
