using REB.Engine.Combat.Components;
using REB.Engine.ECS;
using REB.Engine.Loot.Components;
using REB.Engine.Player;
using REB.Engine.Player.Components;
using REB.Engine.Player.Princess.Components;
using REB.Engine.Player.Systems;
using REB.Engine.Tavern.Components;
using REB.Engine.Tavern.Systems;
using REB.Engine.UI.Systems;
using Xunit;

namespace REB.Tests.UI;

// ---------------------------------------------------------------------------
//  HUDSystem tests
//
//  HUDSystem reads tagged singleton entities and aggregates data into a
//  HUDDataComponent snapshot. These tests verify each data source in isolation.
// ---------------------------------------------------------------------------

public sealed class HUDSystemTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static (World world, HUDSystem hudSystem) BuildWorld()
    {
        var world     = new World();
        var hudSystem = new HUDSystem();
        // MoodSystem omitted: it decays goodwill each frame, making exact assertions flaky.
        // LootValuationSystem omitted: it recomputes TreasureValue from real inventory items
        // (none exist in these tests), which would zero out values set directly on the ledger.
        // HUDSystem only lists them as RunAfter ordering hints; both are safe to omit.
        world.RegisterSystem(new RoleAbilitySystem());
        world.RegisterSystem(new GoldCurrencySystem());
        world.RegisterSystem(hudSystem);
        return (world, hudSystem);
    }

    private static Entity AddPrincess(World world, float health = 80f, float maxHp = 100f, float goodwill = 60f)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "Princess");
        world.AddComponent(e, HealthComponent.For(maxHp));
        // Set current health
        ref var hp = ref world.GetComponent<HealthComponent>(e);
        hp.CurrentHealth = health;
        var gc = PrincessGoodwillComponent.Default;
        gc.Goodwill = goodwill;
        world.AddComponent(e, gc);
        world.AddComponent(e, PrincessStateComponent.Default);
        world.AddComponent(e, PrincessTraitComponent.Random(seed: 1));
        return e;
    }

    private static Entity AddTreasureLedger(World world, int value = 500)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "TreasureLedger");
        var tl = TreasureLedgerComponent.Default;
        tl.TotalValue = value;
        world.AddComponent(e, tl);
        return e;
    }

    private static Entity AddPlayer(World world,
        PlayerRole role = PlayerRole.Carrier,
        float cooldownRemaining = 0f, float cooldownDuration = 10f,
        bool overweight = false)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "Player");
        world.AddComponent(e, new RoleComponent
        {
            Role                     = role,
            AbilityReady             = cooldownRemaining <= 0f,
            AbilityCooldownRemaining = cooldownRemaining,
            AbilityCooldownDuration  = cooldownDuration,
        });
        var inv = InventoryComponent.Default;
        inv.IsOverweight = overweight;
        world.AddComponent(e, inv);
        return e;
    }

    private static Entity AddGoldLedger(World world, float gold = 250f)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "GoldLedger");
        var gc = GoldCurrencyComponent.Default;
        gc.TotalGold = gold;
        world.AddComponent(e, gc);
        world.AddComponent(e, REB.Engine.Tavern.Components.UpgradeTreeComponent.Default);
        return e;
    }

    // -------------------------------------------------------------------------
    //  Princess health
    // -------------------------------------------------------------------------

    [Fact]
    public void HUD_ReflectsPrincessCurrentHealth()
    {
        var (world, hud) = BuildWorld();
        AddPrincess(world, health: 72f, maxHp: 100f);
        AddTreasureLedger(world);
        AddPlayer(world);
        AddGoldLedger(world);

        world.Update(0.016f);

        Assert.Equal(72f, hud.CurrentHUD.PrincessHealth, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void HUD_ReflectsPrincessMaxHealth()
    {
        var (world, hud) = BuildWorld();
        AddPrincess(world, health: 50f, maxHp: 120f);
        AddTreasureLedger(world);
        AddPlayer(world);
        AddGoldLedger(world);

        world.Update(0.016f);

        Assert.Equal(120f, hud.CurrentHUD.PrincessMaxHealth, precision: 3);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Princess goodwill
    // -------------------------------------------------------------------------

    [Fact]
    public void HUD_ReflectsPrincessGoodwill()
    {
        var (world, hud) = BuildWorld();
        AddPrincess(world, goodwill: 88f);
        AddTreasureLedger(world);
        AddPlayer(world);
        AddGoldLedger(world);

        world.Update(0.016f);

        Assert.Equal(88f, hud.CurrentHUD.PrincessGoodwill, precision: 3);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Treasure value
    // -------------------------------------------------------------------------

    [Fact]
    public void HUD_ReflectsTreasureValue()
    {
        var (world, hud) = BuildWorld();
        AddPrincess(world);
        AddTreasureLedger(world, value: 1200);
        AddPlayer(world);
        AddGoldLedger(world);

        world.Update(0.016f);

        Assert.Equal(1200, hud.CurrentHUD.TreasureValue);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Player role
    // -------------------------------------------------------------------------

    [Fact]
    public void HUD_ReflectsPlayerRole()
    {
        var (world, hud) = BuildWorld();
        AddPrincess(world);
        AddTreasureLedger(world);
        AddPlayer(world, role: PlayerRole.Scout);
        AddGoldLedger(world);

        world.Update(0.016f);

        Assert.Equal(PlayerRole.Scout, hud.CurrentHUD.CarrierRole);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Ability cooldown percentage
    // -------------------------------------------------------------------------

    [Fact]
    public void HUD_CooldownPct_IsZero_WhenAbilityReady()
    {
        var (world, hud) = BuildWorld();
        AddPrincess(world);
        AddTreasureLedger(world);
        AddPlayer(world, cooldownRemaining: 0f, cooldownDuration: 10f);
        AddGoldLedger(world);

        world.Update(0.016f);

        Assert.Equal(0f, hud.CurrentHUD.AbilityCooldownPct, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void HUD_CooldownPct_IsHalf_WhenHalfCooldownRemains()
    {
        var (world, hud) = BuildWorld();
        AddPrincess(world);
        AddTreasureLedger(world);
        AddPlayer(world, cooldownRemaining: 5f, cooldownDuration: 10f);
        AddGoldLedger(world);

        world.Update(0.016f);

        Assert.Equal(0.5f, hud.CurrentHUD.AbilityCooldownPct, precision: 3);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Overweight flag
    // -------------------------------------------------------------------------

    [Fact]
    public void HUD_IsOverweight_WhenPlayerInventoryOverweight()
    {
        var (world, hud) = BuildWorld();
        AddPrincess(world);
        AddTreasureLedger(world);
        AddPlayer(world, overweight: true);
        AddGoldLedger(world);

        world.Update(0.016f);

        Assert.True(hud.CurrentHUD.IsOverweight);
        world.Dispose();
    }

    [Fact]
    public void HUD_IsNotOverweight_WhenPlayerInventoryNormal()
    {
        var (world, hud) = BuildWorld();
        AddPrincess(world);
        AddTreasureLedger(world);
        AddPlayer(world, overweight: false);
        AddGoldLedger(world);

        world.Update(0.016f);

        Assert.False(hud.CurrentHUD.IsOverweight);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Gold total
    // -------------------------------------------------------------------------

    [Fact]
    public void HUD_ReflectsGoldBalance()
    {
        var (world, hud) = BuildWorld();
        AddPrincess(world);
        AddTreasureLedger(world);
        AddPlayer(world);
        AddGoldLedger(world, gold: 777f);

        world.Update(0.016f);

        Assert.Equal(777f, hud.CurrentHUD.GoldTotal, precision: 3);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  No entities — does not throw
    // -------------------------------------------------------------------------

    [Fact]
    public void HUD_NoEntities_DoesNotThrow()
    {
        var (world, _) = BuildWorld();

        var ex = Record.Exception(() => world.Update(0.016f));
        Assert.Null(ex);
        world.Dispose();
    }

    [Fact]
    public void HUD_DefaultsToZero_WhenNoPrincessEntity()
    {
        var (world, hud) = BuildWorld();
        AddTreasureLedger(world);
        AddPlayer(world);
        AddGoldLedger(world);

        world.Update(0.016f);

        Assert.Equal(0f, hud.CurrentHUD.PrincessHealth, precision: 3);
        world.Dispose();
    }
}
