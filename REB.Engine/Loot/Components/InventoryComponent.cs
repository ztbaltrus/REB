using REB.Engine.ECS;

namespace REB.Engine.Loot.Components;

/// <summary>
/// Tracks the weight and item count for a player's carried loot.
/// InventorySystem recomputes CurrentWeight and ItemCount each frame by summing
/// all ItemComponent entities whose OwnerEntity matches this player.
/// </summary>
public struct InventoryComponent : IComponent
{
    /// <summary>Maximum carry weight in kilograms before IsOverweight is set.</summary>
    public float MaxWeight;

    /// <summary>Total weight of all owned items this frame. Recomputed by InventorySystem.</summary>
    public float CurrentWeight;

    /// <summary>Number of items currently owned. Recomputed by InventorySystem.</summary>
    public int ItemCount;

    /// <summary>True when CurrentWeight exceeds MaxWeight.</summary>
    public bool IsOverweight;

    /// <summary>Default inventory with a 20 kg carry limit.</summary>
    public static InventoryComponent Default => new() { MaxWeight = 20f };
}
