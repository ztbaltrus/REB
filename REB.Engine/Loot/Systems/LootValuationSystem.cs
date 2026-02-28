using REB.Engine.ECS;
using REB.Engine.Loot.Components;
using REB.Engine.Multiplayer.Components;
using REB.Engine.Player;
using REB.Engine.Player.Components;

namespace REB.Engine.Loot.Systems;

/// <summary>
/// Recomputes the session TreasureLedgerComponent each frame, applying rarity
/// multipliers and the Treasurer bonus to all items currently held by players.
/// <para>
/// Value multipliers: Common 1×, Rare 2×, Legendary 5× (7.5× with Treasurer), Cursed 0.5×.
/// </para>
/// </summary>
[RunAfter(typeof(InventorySystem))]
public sealed class LootValuationSystem : GameSystem
{
    public override void Update(float deltaTime)
    {
        Entity ledgerEntity = FindLedger();
        if (!World.IsAlive(ledgerEntity)) return;

        ref var ledger = ref World.GetComponent<TreasureLedgerComponent>(ledgerEntity);

        // Locate the Treasurer (if any) to determine the legendary bonus.
        ledger.TreasurerId = -1;
        foreach (var player in World.Query<RoleComponent, PlayerSessionComponent>())
        {
            var role    = World.GetComponent<RoleComponent>(player);
            var session = World.GetComponent<PlayerSessionComponent>(player);
            if (role.Role == PlayerRole.Treasurer)
            {
                ledger.TreasurerId = session.SlotIndex;
                break;
            }
        }

        bool hasTreasurer = ledger.TreasurerId >= 0;

        // Reset counters.
        ledger.TotalValue     = 0;
        ledger.CommonCount    = 0;
        ledger.RareCount      = 0;
        ledger.LegendaryCount = 0;
        ledger.CursedCount    = 0;

        // Tally all items currently held by a player.
        foreach (var item in World.Query<ItemComponent>())
        {
            var ic = World.GetComponent<ItemComponent>(item);
            if (!World.IsAlive(ic.OwnerEntity)) continue;

            float multiplier = ic.Rarity switch
            {
                ItemRarity.Common    => 1f,
                ItemRarity.Rare      => 2f,
                ItemRarity.Legendary => hasTreasurer ? 7.5f : 5f,
                ItemRarity.Cursed    => 0.5f,
                _                    => 1f,
            };

            ledger.TotalValue += (int)(ic.BaseValue * multiplier);

            switch (ic.Rarity)
            {
                case ItemRarity.Common:    ledger.CommonCount++;    break;
                case ItemRarity.Rare:      ledger.RareCount++;      break;
                case ItemRarity.Legendary: ledger.LegendaryCount++; break;
                case ItemRarity.Cursed:    ledger.CursedCount++;    break;
            }
        }
    }

    private Entity FindLedger()
    {
        foreach (var e in World.GetEntitiesWithTag("TreasureLedger"))
            return e;
        return Entity.Null;
    }
}
