using REB.Engine.ECS;

namespace REB.Engine.Loot.Components;

/// <summary>
/// Marks an entity as a loot item. Stores its type, rarity, weight, base value,
/// current owner, and — for active/consumable items — its cooldown state.
/// </summary>
public struct ItemComponent : IComponent
{
    /// <summary>Functional category of this item.</summary>
    public ItemType   Type;

    /// <summary>Rarity tier; drives the value multiplier in LootValuationSystem.</summary>
    public ItemRarity Rarity;

    /// <summary>Whether and how the item can be actively used.</summary>
    public ItemUsage  Usage;

    /// <summary>Physical weight in kilograms. Contributes to the carrier's CurrentWeight.</summary>
    public float Weight;

    /// <summary>Base gold value before rarity and role multipliers.</summary>
    public int BaseValue;

    /// <summary>True when Rarity is Cursed.</summary>
    public bool IsCursed;

    /// <summary>
    /// The player entity currently holding this item.
    /// Entity.Null when the item is on the ground.
    /// </summary>
    public Entity OwnerEntity;

    /// <summary>Seconds remaining before this item can be used again (Active items only).</summary>
    public float CooldownRemaining;

    /// <summary>Base cooldown duration in seconds after activation.</summary>
    public float MaxCooldown;

    // -------------------------------------------------------------------------
    //  Factory presets
    // -------------------------------------------------------------------------

    /// <summary>Small coin; negligible weight, low value, Common rarity.</summary>
    public static ItemComponent Coin => new()
    {
        Type      = ItemType.Coin,
        Rarity    = ItemRarity.Common,
        Usage     = ItemUsage.Passive,
        Weight    = 0.01f,
        BaseValue = 10,
    };

    /// <summary>Precious gem; light, moderate value, Rare rarity.</summary>
    public static ItemComponent Gem => new()
    {
        Type      = ItemType.Gem,
        Rarity    = ItemRarity.Rare,
        Usage     = ItemUsage.Passive,
        Weight    = 0.5f,
        BaseValue = 50,
    };

    /// <summary>Ancient artifact; heavy, high value, Legendary rarity.</summary>
    public static ItemComponent Artifact => new()
    {
        Type      = ItemType.Artifact,
        Rarity    = ItemRarity.Legendary,
        Usage     = ItemUsage.Passive,
        Weight    = 5f,
        BaseValue = 200,
    };

    /// <summary>Single-use consumable with a gameplay effect.</summary>
    public static ItemComponent Consumable(int baseValue = 30) => new()
    {
        Type      = ItemType.Consumable,
        Rarity    = ItemRarity.Common,
        Usage     = ItemUsage.Consumable,
        Weight    = 0.3f,
        BaseValue = baseValue,
    };

    /// <summary>Reusable tool with an activation cooldown.</summary>
    public static ItemComponent ActiveTool(int baseValue = 40, float cooldown = 5f) => new()
    {
        Type        = ItemType.Tool,
        Rarity      = ItemRarity.Common,
        Usage       = ItemUsage.Active,
        Weight      = 0.5f,
        BaseValue   = baseValue,
        MaxCooldown = cooldown,
    };

    /// <summary>Cursed relic; heavy, deceptively high base value, negative effect.</summary>
    public static ItemComponent CursedRelic => new()
    {
        Type      = ItemType.Cursed,
        Rarity    = ItemRarity.Cursed,
        Usage     = ItemUsage.Passive,
        Weight    = 3f,
        BaseValue = 100,
        IsCursed  = true,
    };
}
