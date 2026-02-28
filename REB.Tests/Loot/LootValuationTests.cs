using REB.Engine.ECS;
using REB.Engine.Loot.Components;
using REB.Engine.Loot.Systems;
using REB.Engine.Multiplayer;
using REB.Engine.Multiplayer.Components;
using REB.Engine.Player;
using REB.Engine.Player.Components;
using Xunit;

namespace REB.Tests.Loot;

// ---------------------------------------------------------------------------
//  LootValuationSystem tests
//
//  Items' OwnerEntity is set directly on the component to avoid needing
//  PickupInteractionSystem; only LootValuationSystem is registered.
// ---------------------------------------------------------------------------

public sealed class LootValuationTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static (World world, Entity ledger) BuildWorld()
    {
        var world  = new World();
        world.RegisterSystem(new LootValuationSystem());

        var ledger = world.CreateEntity();
        world.AddTag(ledger, "TreasureLedger");
        world.AddComponent(ledger, TreasureLedgerComponent.Default);

        return (world, ledger);
    }

    /// <summary>Creates a minimal player entity (no inventory needed for valuation).</summary>
    private static Entity AddPlayer(World world) => world.CreateEntity();

    private static Entity AddPlayerWithRole(World world, PlayerRole role, byte slot = 0)
    {
        var e = world.CreateEntity();
        world.AddComponent(e, new RoleComponent { Role = role });
        world.AddComponent(e, PlayerSessionComponent.ForSlot(slot));
        return e;
    }

    private static Entity AddOwnedItem(World world, Entity owner, ItemComponent ic)
    {
        ic.OwnerEntity = owner;
        var e = world.CreateEntity();
        world.AddComponent(e, ic);
        return e;
    }

    // -------------------------------------------------------------------------
    //  Rarity multipliers
    // -------------------------------------------------------------------------

    [Fact]
    public void CommonItem_UsesBaseValue()
    {
        var (world, ledger) = BuildWorld();
        var player = AddPlayer(world);
        AddOwnedItem(world, player, ItemComponent.Coin);  // BaseValue = 10, Common

        world.Update(0.016f);

        var lc = world.GetComponent<TreasureLedgerComponent>(ledger);
        Assert.Equal(10, lc.TotalValue);
        world.Dispose();
    }

    [Fact]
    public void RareItem_TwoXMultiplier()
    {
        var (world, ledger) = BuildWorld();
        var player = AddPlayer(world);
        AddOwnedItem(world, player, ItemComponent.Gem);  // BaseValue = 50, Rare

        world.Update(0.016f);

        var lc = world.GetComponent<TreasureLedgerComponent>(ledger);
        Assert.Equal(100, lc.TotalValue);  // 50 × 2
        world.Dispose();
    }

    [Fact]
    public void LegendaryItem_FiveXMultiplier_WithoutTreasurer()
    {
        var (world, ledger) = BuildWorld();
        var player = AddPlayer(world);
        AddOwnedItem(world, player, ItemComponent.Artifact);  // BaseValue = 200, Legendary

        world.Update(0.016f);

        var lc = world.GetComponent<TreasureLedgerComponent>(ledger);
        Assert.Equal(1000, lc.TotalValue);  // 200 × 5
        world.Dispose();
    }

    [Fact]
    public void LegendaryItem_SevenPointFiveX_WithTreasurer()
    {
        var (world, ledger) = BuildWorld();
        AddPlayerWithRole(world, PlayerRole.Treasurer, slot: 0);
        var player = AddPlayer(world);
        AddOwnedItem(world, player, ItemComponent.Artifact);  // BaseValue = 200

        world.Update(0.016f);

        var lc = world.GetComponent<TreasureLedgerComponent>(ledger);
        Assert.Equal(1500, lc.TotalValue);  // 200 × 7.5
        world.Dispose();
    }

    [Fact]
    public void CursedItem_HalfValue()
    {
        var (world, ledger) = BuildWorld();
        var player = AddPlayer(world);
        AddOwnedItem(world, player, ItemComponent.CursedRelic);  // BaseValue = 100, Cursed

        world.Update(0.016f);

        var lc = world.GetComponent<TreasureLedgerComponent>(ledger);
        Assert.Equal(50, lc.TotalValue);  // 100 × 0.5
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Mixed inventory
    // -------------------------------------------------------------------------

    [Fact]
    public void MultipleItems_SumCorrectly()
    {
        var (world, ledger) = BuildWorld();
        var player = AddPlayer(world);
        AddOwnedItem(world, player, ItemComponent.Coin);    // 10 × 1  = 10
        AddOwnedItem(world, player, ItemComponent.Gem);     // 50 × 2  = 100
        AddOwnedItem(world, player, ItemComponent.Artifact); // 200 × 5 = 1000

        world.Update(0.016f);

        var lc = world.GetComponent<TreasureLedgerComponent>(ledger);
        Assert.Equal(1110, lc.TotalValue);
        world.Dispose();
    }

    [Fact]
    public void GroundItems_NotCounted()
    {
        var (world, ledger) = BuildWorld();
        // Item with no owner (on the ground).
        var e = world.CreateEntity();
        world.AddComponent(e, ItemComponent.Artifact);  // OwnerEntity = Entity.Null

        world.Update(0.016f);

        var lc = world.GetComponent<TreasureLedgerComponent>(ledger);
        Assert.Equal(0, lc.TotalValue);
        world.Dispose();
    }

    [Fact]
    public void EmptyInventory_TotalValueZero()
    {
        var (world, ledger) = BuildWorld();

        world.Update(0.016f);

        var lc = world.GetComponent<TreasureLedgerComponent>(ledger);
        Assert.Equal(0, lc.TotalValue);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Rarity counts
    // -------------------------------------------------------------------------

    [Fact]
    public void RarityCounts_UpdatedCorrectly()
    {
        var (world, ledger) = BuildWorld();
        var player = AddPlayer(world);
        AddOwnedItem(world, player, ItemComponent.Coin);        // Common
        AddOwnedItem(world, player, ItemComponent.Gem);         // Rare
        AddOwnedItem(world, player, ItemComponent.Artifact);    // Legendary
        AddOwnedItem(world, player, ItemComponent.CursedRelic); // Cursed

        world.Update(0.016f);

        var lc = world.GetComponent<TreasureLedgerComponent>(ledger);
        Assert.Equal(1, lc.CommonCount);
        Assert.Equal(1, lc.RareCount);
        Assert.Equal(1, lc.LegendaryCount);
        Assert.Equal(1, lc.CursedCount);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Treasurer detection
    // -------------------------------------------------------------------------

    [Fact]
    public void NoTreasurer_TreasurerId_IsMinusOne()
    {
        var (world, ledger) = BuildWorld();
        AddPlayerWithRole(world, PlayerRole.Carrier, slot: 0);

        world.Update(0.016f);

        var lc = world.GetComponent<TreasureLedgerComponent>(ledger);
        Assert.Equal(-1, lc.TreasurerId);
        world.Dispose();
    }

    [Fact]
    public void Treasurer_SlotTwo_RecordedInLedger()
    {
        var (world, ledger) = BuildWorld();
        AddPlayerWithRole(world, PlayerRole.Treasurer, slot: 2);

        world.Update(0.016f);

        var lc = world.GetComponent<TreasureLedgerComponent>(ledger);
        Assert.Equal(2, lc.TreasurerId);
        world.Dispose();
    }
}
