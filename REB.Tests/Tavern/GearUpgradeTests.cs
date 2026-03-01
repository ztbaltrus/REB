using REB.Engine.Combat.Components;
using REB.Engine.ECS;
using REB.Engine.Tavern;
using REB.Engine.Tavern.Components;
using REB.Engine.Tavern.Systems;
using Xunit;

namespace REB.Tests.Tavern;

// ---------------------------------------------------------------------------
//  GearUpgradeSystem tests
//
//  Full system chain: GoldCurrencySystem → UpgradeTreeSystem → GearUpgradeSystem.
//  Tests purchase upgrades via UpgradeTreeSystem.RequestPurchase then verify
//  the resulting GearBonusComponent values and stat modifications.
// ---------------------------------------------------------------------------

public sealed class GearUpgradeTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static (World world, UpgradeTreeSystem upgradeTree) BuildWorld()
    {
        var world       = new World();
        var goldSystem  = new GoldCurrencySystem();
        var upgradeTree = new UpgradeTreeSystem();
        var gearSystem  = new GearUpgradeSystem();
        world.RegisterSystem(goldSystem);
        world.RegisterSystem(upgradeTree);
        world.RegisterSystem(gearSystem);
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

    private static Entity AddPlayer(World world,
        float maxHp  = 100f,
        float damage = 10f)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "Player");
        world.AddComponent(e, HealthComponent.For(maxHp));
        world.AddComponent(e, DamageComponent.MeleeDefault);
        // Override damage to a known value.
        ref var dmg = ref world.GetComponent<DamageComponent>(e);
        dmg.Damage = damage;
        return e;
    }

    private static GearBonusComponent GetBonus(World world, Entity player) =>
        world.GetComponent<GearBonusComponent>(player);

    // -------------------------------------------------------------------------
    //  Default state — no upgrades
    // -------------------------------------------------------------------------

    [Fact]
    public void GearBonus_Default_WhenNoUpgrades()
    {
        var (world, _) = BuildWorld();
        AddGoldLedger(world);
        var player = AddPlayer(world);

        world.Update(0.016f);

        var bonus = GetBonus(world, player);
        Assert.Equal(0f, bonus.CarrySpeedBonus,     precision: 3);
        Assert.Equal(0f, bonus.DropResistanceBonus, precision: 3);
        Assert.Equal(1f, bonus.DamageMultiplier,     precision: 3);
        Assert.Equal(0f, bonus.MaxHealthBonus,       precision: 3);
        Assert.Equal(0f, bonus.SprintSpeedBonus,     precision: 3);
        Assert.False(bonus.HasLockpick);
        Assert.False(bonus.HasGrapplingHook);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Harness speed
    // -------------------------------------------------------------------------

    [Fact]
    public void HarnessSpeed1_SetsCarrySpeedBonusToTen()
    {
        var (world, upgradeTree) = BuildWorld();
        AddGoldLedger(world);
        var player = AddPlayer(world);

        upgradeTree.RequestPurchase(UpgradeId.HarnessSpeed1);
        world.Update(0.016f);

        Assert.Equal(0.10f, GetBonus(world, player).CarrySpeedBonus, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void BothHarnessSpeedTiers_AccumulateTwentyPercent()
    {
        var (world, upgradeTree) = BuildWorld();
        AddGoldLedger(world);
        var player = AddPlayer(world);

        upgradeTree.RequestPurchase(UpgradeId.HarnessSpeed1);
        world.Update(0.016f);
        upgradeTree.RequestPurchase(UpgradeId.HarnessSpeed2);
        world.Update(0.016f);

        Assert.Equal(0.20f, GetBonus(world, player).CarrySpeedBonus, precision: 3);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Harness grip (drop resistance)
    // -------------------------------------------------------------------------

    [Fact]
    public void HarnessGrip1_SetsDropResistanceToThirty()
    {
        var (world, upgradeTree) = BuildWorld();
        AddGoldLedger(world);
        var player = AddPlayer(world);

        upgradeTree.RequestPurchase(UpgradeId.HarnessGrip1);
        world.Update(0.016f);

        Assert.Equal(0.30f, GetBonus(world, player).DropResistanceBonus, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void BothHarnessGripTiers_AccumulateSixtyPercent()
    {
        var (world, upgradeTree) = BuildWorld();
        AddGoldLedger(world);
        var player = AddPlayer(world);

        upgradeTree.RequestPurchase(UpgradeId.HarnessGrip1);
        world.Update(0.016f);
        upgradeTree.RequestPurchase(UpgradeId.HarnessGrip2);
        world.Update(0.016f);

        Assert.Equal(0.60f, GetBonus(world, player).DropResistanceBonus, precision: 3);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Weapon damage
    // -------------------------------------------------------------------------

    [Fact]
    public void WeaponDamage1_SetsDamageMultiplierToOneFifteen()
    {
        var (world, upgradeTree) = BuildWorld();
        AddGoldLedger(world);
        var player = AddPlayer(world);

        upgradeTree.RequestPurchase(UpgradeId.WeaponDamage1);
        world.Update(0.016f);

        Assert.Equal(1.15f, GetBonus(world, player).DamageMultiplier, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void BothWeaponTiers_SetDamageMultiplierToOneThirty()
    {
        var (world, upgradeTree) = BuildWorld();
        AddGoldLedger(world);
        var player = AddPlayer(world);

        upgradeTree.RequestPurchase(UpgradeId.WeaponDamage1);
        world.Update(0.016f);
        upgradeTree.RequestPurchase(UpgradeId.WeaponDamage2);
        world.Update(0.016f);

        Assert.Equal(1.30f, GetBonus(world, player).DamageMultiplier, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void WeaponDamage1_AppliesMultiplierToDamageComponent()
    {
        var (world, upgradeTree) = BuildWorld();
        AddGoldLedger(world);
        // base damage = 10
        var player = AddPlayer(world, damage: 10f);

        upgradeTree.RequestPurchase(UpgradeId.WeaponDamage1);
        world.Update(0.016f);

        float dmg = world.GetComponent<DamageComponent>(player).Damage;
        Assert.Equal(10f * 1.15f, dmg, precision: 3);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Armor (max health)
    // -------------------------------------------------------------------------

    [Fact]
    public void Armor1_SetsMaxHealthBonusToTwenty()
    {
        var (world, upgradeTree) = BuildWorld();
        AddGoldLedger(world);
        var player = AddPlayer(world);

        upgradeTree.RequestPurchase(UpgradeId.Armor1);
        world.Update(0.016f);

        Assert.Equal(20f, GetBonus(world, player).MaxHealthBonus, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void BothArmorTiers_SetMaxHealthBonusToForty()
    {
        var (world, upgradeTree) = BuildWorld();
        AddGoldLedger(world);
        var player = AddPlayer(world);

        upgradeTree.RequestPurchase(UpgradeId.Armor1);
        world.Update(0.016f);
        upgradeTree.RequestPurchase(UpgradeId.Armor2);
        world.Update(0.016f);

        Assert.Equal(40f, GetBonus(world, player).MaxHealthBonus, precision: 3);
        world.Dispose();
    }

    [Fact]
    public void Armor1_AppliesBonusToHealthComponent()
    {
        var (world, upgradeTree) = BuildWorld();
        AddGoldLedger(world);
        var player = AddPlayer(world, maxHp: 100f);

        upgradeTree.RequestPurchase(UpgradeId.Armor1);
        world.Update(0.016f);

        float maxHp = world.GetComponent<HealthComponent>(player).MaxHealth;
        Assert.Equal(120f, maxHp, precision: 3);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Tool upgrades
    // -------------------------------------------------------------------------

    [Fact]
    public void Lockpick_SetsHasLockpickTrue()
    {
        var (world, upgradeTree) = BuildWorld();
        AddGoldLedger(world);
        var player = AddPlayer(world);

        upgradeTree.RequestPurchase(UpgradeId.Lockpick);
        world.Update(0.016f);

        Assert.True(GetBonus(world, player).HasLockpick);
        world.Dispose();
    }

    [Fact]
    public void GrapplingHook_SetsHasGrapplingHookTrue()
    {
        var (world, upgradeTree) = BuildWorld();
        AddGoldLedger(world);
        var player = AddPlayer(world);

        upgradeTree.RequestPurchase(UpgradeId.GrapplingHook);
        world.Update(0.016f);

        Assert.True(GetBonus(world, player).HasGrapplingHook);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Sprint speed
    // -------------------------------------------------------------------------

    [Fact]
    public void SprintBoost1_SetsSprintBonusToTen()
    {
        var (world, upgradeTree) = BuildWorld();
        AddGoldLedger(world);
        var player = AddPlayer(world);

        upgradeTree.RequestPurchase(UpgradeId.SprintBoost1);
        world.Update(0.016f);

        Assert.Equal(0.10f, GetBonus(world, player).SprintSpeedBonus, precision: 3);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Base values not overwritten on subsequent frames
    // -------------------------------------------------------------------------

    [Fact]
    public void BaseMaxHealth_FixedOnFirstFrame_NotDriftingEachFrame()
    {
        var (world, upgradeTree) = BuildWorld();
        AddGoldLedger(world);
        var player = AddPlayer(world, maxHp: 100f);

        // First frame: base captured as 100
        world.Update(0.016f);

        // Purchase Armor1 (adds 20)
        upgradeTree.RequestPurchase(UpgradeId.Armor1);
        world.Update(0.016f);   // MaxHealth → 120

        // Run several more frames
        world.Update(0.016f);
        world.Update(0.016f);

        // Should still be exactly 120 (not growing each frame)
        Assert.Equal(120f, world.GetComponent<HealthComponent>(player).MaxHealth, precision: 3);
        world.Dispose();
    }
}
