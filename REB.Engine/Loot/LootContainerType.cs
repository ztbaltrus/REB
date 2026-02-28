namespace REB.Engine.Loot;

/// <summary>Physical form of an openable loot container in the dungeon.</summary>
public enum LootContainerType
{
    Chest,   // Standard wooden chest; holds general loot.
    Corpse,  // Fallen enemy or NPC remains; small loot chance.
    Shrine,  // Magical altar; higher chance of Legendary or Cursed items.
}
