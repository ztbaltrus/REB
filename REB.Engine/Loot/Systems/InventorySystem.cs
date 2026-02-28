using REB.Engine.ECS;
using REB.Engine.Loot.Components;

namespace REB.Engine.Loot.Systems;

/// <summary>
/// Recomputes each player's CurrentWeight and ItemCount every frame by summing
/// all ItemComponent entities whose OwnerEntity points at that player.
/// Sets IsOverweight when CurrentWeight exceeds MaxWeight.
/// </summary>
[RunAfter(typeof(PickupInteractionSystem))]
public sealed class InventorySystem : GameSystem
{
    public override void Update(float deltaTime)
    {
        // Reset all counters first.
        foreach (var player in World.Query<InventoryComponent>())
        {
            ref var inv = ref World.GetComponent<InventoryComponent>(player);
            inv.CurrentWeight = 0f;
            inv.ItemCount     = 0;
        }

        // Accumulate from every owned item.
        foreach (var item in World.Query<ItemComponent>())
        {
            var ic = World.GetComponent<ItemComponent>(item);
            if (!World.IsAlive(ic.OwnerEntity)) continue;
            if (!World.HasComponent<InventoryComponent>(ic.OwnerEntity)) continue;

            ref var inv = ref World.GetComponent<InventoryComponent>(ic.OwnerEntity);
            inv.CurrentWeight += ic.Weight;
            inv.ItemCount++;
        }

        // Derive the overweight flag.
        foreach (var player in World.Query<InventoryComponent>())
        {
            ref var inv = ref World.GetComponent<InventoryComponent>(player);
            inv.IsOverweight = inv.CurrentWeight > inv.MaxWeight;
        }
    }
}
