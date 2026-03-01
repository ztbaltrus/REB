using REB.Engine.Combat.Components;
using REB.Engine.ECS;
using REB.Engine.Tavern.Components;

namespace REB.Engine.Tavern.Systems;

/// <summary>
/// Re-derives <see cref="GearBonusComponent"/> for every Player entity each frame from the
/// current contents of <see cref="UpgradeTreeComponent"/>, then writes the computed stats
/// back into <see cref="HealthComponent"/> and <see cref="DamageComponent"/> where present.
/// <para>
/// Base values (<see cref="GearBonusComponent.BaseMaxHealth"/>,
/// <see cref="GearBonusComponent.BaseDamage"/>) are captured on the first frame the system
/// encounters each player entity and never overwritten, so upgrades are always additive on
/// top of the entity's original design values.
/// </para>
/// </summary>
[RunAfter(typeof(UpgradeTreeSystem))]
public sealed class GearUpgradeSystem : GameSystem
{
    public override void Update(float deltaTime)
    {
        // Grab the party-wide upgrade tree (singleton).
        Entity ledger = FindGoldLedger();
        if (!World.IsAlive(ledger)) return;

        var tree = World.GetComponent<UpgradeTreeComponent>(ledger);

        // Compute the aggregate bonuses once.
        float carrySpeed    = ComputeCarrySpeedBonus(tree);
        float dropResist    = ComputeDropResistanceBonus(tree);
        float dmgMultiplier = ComputeDamageMultiplier(tree);
        float maxHpBonus    = ComputeMaxHealthBonus(tree);
        float sprintSpeed   = ComputeSprintSpeedBonus(tree);
        bool  hasLockpick   = tree.HasUpgrade(UpgradeId.Lockpick);
        bool  hasGrapple    = tree.HasUpgrade(UpgradeId.GrapplingHook);

        // Apply to every Player entity.
        foreach (var e in World.GetEntitiesWithTag("Player"))
        {
            ref var bonus = ref GetOrCreateBonus(e);

            // Capture base values on first encounter.
            if (!bonus.BaseMaxHealthInitialized && World.HasComponent<HealthComponent>(e))
            {
                bonus.BaseMaxHealth            = World.GetComponent<HealthComponent>(e).MaxHealth;
                bonus.BaseMaxHealthInitialized = true;
            }

            if (!bonus.BaseDamageInitialized && World.HasComponent<DamageComponent>(e))
            {
                bonus.BaseDamage            = World.GetComponent<DamageComponent>(e).Damage;
                bonus.BaseDamageInitialized = true;
            }

            // Write computed bonuses.
            bonus.CarrySpeedBonus     = carrySpeed;
            bonus.DropResistanceBonus = dropResist;
            bonus.DamageMultiplier    = dmgMultiplier;
            bonus.MaxHealthBonus      = maxHpBonus;
            bonus.SprintSpeedBonus    = sprintSpeed;
            bonus.HasLockpick         = hasLockpick;
            bonus.HasGrapplingHook    = hasGrapple;

            // Push stat changes into sibling components.
            if (bonus.BaseMaxHealthInitialized && World.HasComponent<HealthComponent>(e))
            {
                ref var hp = ref World.GetComponent<HealthComponent>(e);
                hp.MaxHealth = bonus.BaseMaxHealth + maxHpBonus;
            }

            if (bonus.BaseDamageInitialized && World.HasComponent<DamageComponent>(e))
            {
                ref var dmg = ref World.GetComponent<DamageComponent>(e);
                dmg.Damage = bonus.BaseDamage * dmgMultiplier;
            }

            World.SetComponent(e, bonus);
        }
    }

    // =========================================================================
    //  Bonus computation helpers
    // =========================================================================

    private static float ComputeCarrySpeedBonus(in UpgradeTreeComponent tree)
    {
        float bonus = 0f;
        if (tree.HasUpgrade(UpgradeId.HarnessSpeed1)) bonus += 0.10f;
        if (tree.HasUpgrade(UpgradeId.HarnessSpeed2)) bonus += 0.10f;
        return bonus;
    }

    private static float ComputeDropResistanceBonus(in UpgradeTreeComponent tree)
    {
        float bonus = 0f;
        if (tree.HasUpgrade(UpgradeId.HarnessGrip1)) bonus += 0.30f;
        if (tree.HasUpgrade(UpgradeId.HarnessGrip2)) bonus += 0.30f;
        return bonus;
    }

    private static float ComputeDamageMultiplier(in UpgradeTreeComponent tree)
    {
        float mult = 1.0f;
        if (tree.HasUpgrade(UpgradeId.WeaponDamage1)) mult += 0.15f;
        if (tree.HasUpgrade(UpgradeId.WeaponDamage2)) mult += 0.15f;
        return mult;
    }

    private static float ComputeMaxHealthBonus(in UpgradeTreeComponent tree)
    {
        float bonus = 0f;
        if (tree.HasUpgrade(UpgradeId.Armor1)) bonus += 20f;
        if (tree.HasUpgrade(UpgradeId.Armor2)) bonus += 20f;
        return bonus;
    }

    private static float ComputeSprintSpeedBonus(in UpgradeTreeComponent tree)
    {
        float bonus = 0f;
        if (tree.HasUpgrade(UpgradeId.SprintBoost1)) bonus += 0.10f;
        if (tree.HasUpgrade(UpgradeId.SprintBoost2)) bonus += 0.10f;
        return bonus;
    }

    // =========================================================================
    //  Helper — upsert GearBonusComponent
    // =========================================================================

    private ref GearBonusComponent GetOrCreateBonus(Entity e)
    {
        if (!World.HasComponent<GearBonusComponent>(e))
            World.AddComponent(e, GearBonusComponent.Default);
        return ref World.GetComponent<GearBonusComponent>(e);
    }

    private Entity FindGoldLedger()
    {
        foreach (var e in World.GetEntitiesWithTag("GoldLedger"))
            return e;
        return Entity.Null;
    }
}
