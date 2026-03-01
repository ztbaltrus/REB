using REB.Engine.ECS;
using REB.Engine.Tavern;
using REB.Engine.Tavern.Components;
using REB.Engine.Tavern.Systems;
using Xunit;

namespace REB.Tests.Tavern;

// ---------------------------------------------------------------------------
//  UpgradeTreeSystem tests
//
//  GoldCurrencySystem is registered alongside UpgradeTreeSystem so that
//  TrySpend can operate. Tests start the GoldLedger with a large balance
//  to avoid gold-related failures when testing other constraints.
// ---------------------------------------------------------------------------

public sealed class UpgradeTreeTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static (World world, UpgradeTreeSystem upgradeTree) BuildWorld()
    {
        var world       = new World();
        var goldSystem  = new GoldCurrencySystem();
        var upgradeTree = new UpgradeTreeSystem();
        world.RegisterSystem(goldSystem);
        world.RegisterSystem(upgradeTree);
        return (world, upgradeTree);
    }

    private static Entity AddGoldLedger(World world, float gold = 10_000f)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "GoldLedger");
        var gc = GoldCurrencyComponent.Default;
        gc.TotalGold = gold;
        world.AddComponent(e, gc);
        world.AddComponent(e, UpgradeTreeComponent.Default);
        return e;
    }

    private static UpgradeTreeComponent GetTree(World world, Entity ledger) =>
        world.GetComponent<UpgradeTreeComponent>(ledger);

    // -------------------------------------------------------------------------
    //  HasUpgrade — initial state
    // -------------------------------------------------------------------------

    [Fact]
    public void HasUpgrade_ReturnsFalse_BeforePurchase()
    {
        var tree = UpgradeTreeComponent.Default;
        Assert.False(tree.HasUpgrade(UpgradeId.HarnessSpeed1));
    }

    [Fact]
    public void HasUpgrade_ReturnsTrue_AfterManualAdd()
    {
        var tree = UpgradeTreeComponent.Default;
        tree.AddUpgrade(UpgradeId.HarnessSpeed1);
        Assert.True(tree.HasUpgrade(UpgradeId.HarnessSpeed1));
    }

    [Fact]
    public void HasUpgrade_NoneId_AlwaysReturnsFalse()
    {
        var tree = UpgradeTreeComponent.Default;
        tree.AddUpgrade(UpgradeId.None);     // should be a no-op
        Assert.False(tree.HasUpgrade(UpgradeId.None));
    }

    // -------------------------------------------------------------------------
    //  Successful purchase
    // -------------------------------------------------------------------------

    [Fact]
    public void Purchase_Succeeds_WhenGoldSufficient_AndNoPrerequisite()
    {
        var (world, upgradeTree) = BuildWorld();
        var ledger = AddGoldLedger(world, gold: 10_000f);

        upgradeTree.RequestPurchase(UpgradeId.HarnessSpeed1);
        world.Update(0.016f);

        Assert.Single(upgradeTree.PurchasedEvents);
        Assert.True(GetTree(world, ledger).HasUpgrade(UpgradeId.HarnessSpeed1));
        world.Dispose();
    }

    [Fact]
    public void Purchase_DeductsCorrectGold()
    {
        var (world, upgradeTree) = BuildWorld();
        AddGoldLedger(world, gold: 10_000f);

        upgradeTree.RequestPurchase(UpgradeId.HarnessSpeed1);  // costs 50g
        world.Update(0.016f);

        var gc = world.GetComponent<GoldCurrencyComponent>(
            FindTagged(world, "GoldLedger"));
        Assert.Equal(10_000f - 50f, gc.TotalGold, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void Purchase_Succeeds_WhenPrerequisiteOwned()
    {
        var (world, upgradeTree) = BuildWorld();
        var ledger = AddGoldLedger(world, gold: 10_000f);

        // Manually own the prerequisite.
        ref var tree = ref world.GetComponent<UpgradeTreeComponent>(ledger);
        tree.AddUpgrade(UpgradeId.HarnessSpeed1);

        upgradeTree.RequestPurchase(UpgradeId.HarnessSpeed2);   // prereq: Speed1
        world.Update(0.016f);

        Assert.True(GetTree(world, ledger).HasUpgrade(UpgradeId.HarnessSpeed2));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Purchase failures
    // -------------------------------------------------------------------------

    [Fact]
    public void Purchase_Fails_WhenInsufficientGold()
    {
        var (world, upgradeTree) = BuildWorld();
        AddGoldLedger(world, gold: 10f);   // HarnessSpeed1 costs 50g

        upgradeTree.RequestPurchase(UpgradeId.HarnessSpeed1);
        world.Update(0.016f);

        Assert.Empty(upgradeTree.PurchasedEvents);
        world.Dispose();
    }

    [Fact]
    public void Purchase_Fails_WhenPrerequisiteNotMet()
    {
        var (world, upgradeTree) = BuildWorld();
        var ledger = AddGoldLedger(world, gold: 10_000f);

        // Try Speed2 without owning Speed1.
        upgradeTree.RequestPurchase(UpgradeId.HarnessSpeed2);
        world.Update(0.016f);

        Assert.Empty(upgradeTree.PurchasedEvents);
        Assert.False(GetTree(world, ledger).HasUpgrade(UpgradeId.HarnessSpeed2));
        world.Dispose();
    }

    [Fact]
    public void Purchase_Fails_WhenAlreadyOwned()
    {
        var (world, upgradeTree) = BuildWorld();
        var ledger = AddGoldLedger(world, gold: 10_000f);

        upgradeTree.RequestPurchase(UpgradeId.HarnessSpeed1);
        world.Update(0.016f);

        float goldAfterFirst = world.GetComponent<GoldCurrencyComponent>(
            FindTagged(world, "GoldLedger")).TotalGold;

        // Second request — should be ignored.
        upgradeTree.RequestPurchase(UpgradeId.HarnessSpeed1);
        world.Update(0.016f);

        float goldAfterSecond = world.GetComponent<GoldCurrencyComponent>(
            FindTagged(world, "GoldLedger")).TotalGold;

        Assert.Equal(goldAfterFirst, goldAfterSecond, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void Purchase_Fails_WhenNoGoldLedgerEntity()
    {
        var (world, upgradeTree) = BuildWorld();
        // No GoldLedger entity.

        upgradeTree.RequestPurchase(UpgradeId.HarnessSpeed1);
        world.Update(0.016f);   // should not throw

        Assert.Empty(upgradeTree.PurchasedEvents);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  PurchasedEvents
    // -------------------------------------------------------------------------

    [Fact]
    public void PurchasedEvent_HasCorrectId_AndCost()
    {
        var (world, upgradeTree) = BuildWorld();
        AddGoldLedger(world, gold: 10_000f);

        upgradeTree.RequestPurchase(UpgradeId.HarnessSpeed1);
        world.Update(0.016f);

        Assert.Single(upgradeTree.PurchasedEvents);
        Assert.Equal(UpgradeId.HarnessSpeed1, upgradeTree.PurchasedEvents[0].Id);
        Assert.Equal(50f, upgradeTree.PurchasedEvents[0].GoldSpent, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void PurchasedEvents_ClearedEachFrame()
    {
        var (world, upgradeTree) = BuildWorld();
        AddGoldLedger(world, gold: 10_000f);

        upgradeTree.RequestPurchase(UpgradeId.HarnessSpeed1);
        world.Update(0.016f);
        Assert.Single(upgradeTree.PurchasedEvents);

        world.Update(0.016f);  // no new request
        Assert.Empty(upgradeTree.PurchasedEvents);
        world.Dispose();
    }

    [Fact]
    public void TwoPurchases_InSameFrame_BothSucceed()
    {
        var (world, upgradeTree) = BuildWorld();
        var ledger = AddGoldLedger(world, gold: 10_000f);

        upgradeTree.RequestPurchase(UpgradeId.HarnessGrip1);
        upgradeTree.RequestPurchase(UpgradeId.WeaponDamage1);
        world.Update(0.016f);

        Assert.Equal(2, upgradeTree.PurchasedEvents.Count);
        Assert.True(GetTree(world, ledger).HasUpgrade(UpgradeId.HarnessGrip1));
        Assert.True(GetTree(world, ledger).HasUpgrade(UpgradeId.WeaponDamage1));
        world.Dispose();
    }

    [Fact]
    public void ChainedPurchases_AcrossFrames_WorkCorrectly()
    {
        var (world, upgradeTree) = BuildWorld();
        var ledger = AddGoldLedger(world, gold: 10_000f);

        upgradeTree.RequestPurchase(UpgradeId.HarnessSpeed1);
        world.Update(0.016f);   // Speed1 purchased

        upgradeTree.RequestPurchase(UpgradeId.HarnessSpeed2);
        world.Update(0.016f);   // Speed2 now purchasable

        Assert.True(GetTree(world, ledger).HasUpgrade(UpgradeId.HarnessSpeed2));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Catalog sanity
    // -------------------------------------------------------------------------

    [Fact]
    public void Catalog_ContainsAllExpectedCategories()
    {
        var catalog = UpgradeTreeComponent.Catalog;

        Assert.Contains(catalog.Values, d => d.Category == UpgradeCategory.Gear);
        Assert.Contains(catalog.Values, d => d.Category == UpgradeCategory.Abilities);
        Assert.Contains(catalog.Values, d => d.Category == UpgradeCategory.Bribes);
        Assert.Contains(catalog.Values, d => d.Category == UpgradeCategory.Unlocks);
    }

    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static Entity FindTagged(World world, string tag)
    {
        foreach (var e in world.GetEntitiesWithTag(tag))
            return e;
        return Entity.Null;
    }
}
