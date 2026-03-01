using REB.Engine.ECS;
using REB.Engine.KingsCourt;
using REB.Engine.KingsCourt.Components;
using REB.Engine.KingsCourt.Systems;
using REB.Engine.Tavern.Components;
using REB.Engine.Tavern.Systems;
using Xunit;

namespace REB.Tests.Tavern;

// ---------------------------------------------------------------------------
//  GoldCurrencySystem tests
//
//  PayoutCalculationSystem is registered so its PayoutEvents flow into
//  GoldCurrencySystem. Tests trigger payout by setting KingStateComponent.Phase
//  to Payout (identical to the PayoutCalculationTests pattern).
// ---------------------------------------------------------------------------

public sealed class GoldCurrencyTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static (World world, GoldCurrencySystem goldSystem) BuildWorld()
    {
        var world      = new World();
        var payoutCalc = new PayoutCalculationSystem();
        var goldSystem = new GoldCurrencySystem();
        world.RegisterSystem(payoutCalc);
        world.RegisterSystem(goldSystem);
        return (world, goldSystem);
    }

    private static Entity AddGoldLedger(World world, float gold = 0f)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "GoldLedger");
        var gc = GoldCurrencyComponent.Default;
        gc.TotalGold = gold;
        world.AddComponent(e, gc);
        return e;
    }

    private static Entity AddKingInPayout(World world,
        KingReactionState reaction = KingReactionState.Neutral)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "King");
        var ks = KingStateComponent.Default;
        ks.Phase         = KingsCourtPhase.Payout;
        ks.ReactionState = reaction;
        world.AddComponent(e, ks);
        return e;
    }

    private static Entity AddRunSummary(World world,
        float loot = 300f, int items = 5)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "RunSummary");
        world.AddComponent(e, new RunSummaryComponent
        {
            LootGoldValue           = loot,
            LootItemCount           = items,
            PrincessHealth          = 80f,
            PrincessGoodwill        = 60f,
            PrincessDeliveredSafely = true,
            IsComplete              = true,
        });
        return e;
    }

    private static float GetGold(World world, Entity ledger) =>
        world.GetComponent<GoldCurrencyComponent>(ledger).TotalGold;

    // -------------------------------------------------------------------------
    //  Default balance
    // -------------------------------------------------------------------------

    [Fact]
    public void GoldBalance_StartsAtDefault()
    {
        var (world, _) = BuildWorld();
        var ledger = world.CreateEntity();
        world.AddTag(ledger, "GoldLedger");
        world.AddComponent(ledger, GoldCurrencyComponent.Default);

        Assert.Equal(50f, GetGold(world, ledger));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Gold added from payout
    // -------------------------------------------------------------------------

    [Fact]
    public void Gold_AddedFromPayoutEvent()
    {
        var (world, _) = BuildWorld();
        var ledger = AddGoldLedger(world, gold: 0f);
        AddKingInPayout(world);
        // base = 300 + 5×10 = 350; health/goodwill/delivery bonuses will push it up
        AddRunSummary(world, loot: 300f, items: 5);

        world.Update(0.016f);

        Assert.True(GetGold(world, ledger) > 0f,
            "Gold should increase after a payout event.");
        world.Dispose();
    }

    [Fact]
    public void Gold_NotAdded_WhenNoPayoutEvents()
    {
        var (world, _) = BuildWorld();
        var ledger = AddGoldLedger(world, gold: 100f);
        // No King entity in Payout phase — no payout event fires.

        world.Update(0.016f);

        Assert.Equal(100f, GetGold(world, ledger));
        world.Dispose();
    }

    [Fact]
    public void LifetimeGoldEarned_AlsoIncremented()
    {
        var (world, _) = BuildWorld();
        var ledger = AddGoldLedger(world, gold: 0f);
        AddKingInPayout(world);
        AddRunSummary(world);

        world.Update(0.016f);

        var gc = world.GetComponent<GoldCurrencyComponent>(ledger);
        Assert.Equal(gc.TotalGold, gc.LifetimeGoldEarned, precision: 3);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  TrySpend
    // -------------------------------------------------------------------------

    [Fact]
    public void TrySpend_DeductsCorrectly()
    {
        var (world, goldSystem) = BuildWorld();
        AddGoldLedger(world, gold: 200f);

        bool ok = goldSystem.TrySpend(75f);

        Assert.True(ok);
        Assert.Equal(125f, goldSystem.TotalGold, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void TrySpend_ReturnsFalse_WhenInsufficientGold()
    {
        var (world, goldSystem) = BuildWorld();
        AddGoldLedger(world, gold: 40f);

        bool ok = goldSystem.TrySpend(50f);

        Assert.False(ok);
        Assert.Equal(40f, goldSystem.TotalGold, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void TrySpend_Succeeds_WhenBalanceIsExact()
    {
        var (world, goldSystem) = BuildWorld();
        AddGoldLedger(world, gold: 50f);

        bool ok = goldSystem.TrySpend(50f);

        Assert.True(ok);
        Assert.Equal(0f, goldSystem.TotalGold, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void TrySpend_ReturnsFalse_WhenNoGoldLedgerExists()
    {
        var (world, goldSystem) = BuildWorld();
        // No GoldLedger entity.

        bool ok = goldSystem.TrySpend(1f);

        Assert.False(ok);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  TotalGold property
    // -------------------------------------------------------------------------

    [Fact]
    public void TotalGold_ReturnsZero_WhenNoLedgerExists()
    {
        var (world, goldSystem) = BuildWorld();
        Assert.Equal(0f, goldSystem.TotalGold);
        world.Dispose();
    }

    [Fact]
    public void TotalGold_ReflectsCurrentBalance()
    {
        var (world, goldSystem) = BuildWorld();
        AddGoldLedger(world, gold: 123f);

        Assert.Equal(123f, goldSystem.TotalGold, precision: 3);
        world.Dispose();
    }
}
