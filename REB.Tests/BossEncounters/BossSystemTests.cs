using Microsoft.Xna.Framework;
using REB.Engine.Boss;
using REB.Engine.Boss.Components;
using REB.Engine.Boss.Systems;
using REB.Engine.Combat.Components;
using REB.Engine.ECS;
using REB.Engine.Enemy.Components;
using REB.Engine.Rendering.Components;
using Xunit;

namespace REB.Tests.BossEncounters;

// ---------------------------------------------------------------------------
//  BossSystem tests
//
//  CombatSystem is not registered; RunAfter is silently ignored.
// ---------------------------------------------------------------------------

public sealed class BossSystemTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static (World world, BossSystem bossSystem) BuildWorld()
    {
        var world      = new World();
        var bossSystem = new BossSystem();
        world.RegisterSystem(bossSystem);
        return (world, bossSystem);
    }

    private static TransformComponent MakeTransform(Vector3 pos) => new()
    {
        Position = pos, Rotation = Quaternion.Identity,
        Scale = Vector3.One, WorldMatrix = Matrix.Identity,
    };

    private static Entity AddBoss(
        World world, float currentHp = 100f, float maxHp = 100f)
    {
        var e = world.CreateEntity();
        world.AddComponent(e, MakeTransform(Vector3.Zero));
        world.AddComponent(e, new HealthComponent
        {
            MaxHealth     = maxHp,
            CurrentHealth = currentHp,
            IsDead        = false,
            IsInvulnerable = false,
        });
        world.AddComponent(e, BossComponent.Default);
        return e;
    }

    // -------------------------------------------------------------------------
    //  Phase transitions
    // -------------------------------------------------------------------------

    [Fact]
    public void Boss_TransitionsToPhase2_AtSixtyPercentHealth()
    {
        var (world, _) = BuildWorld();
        var boss = AddBoss(world);

        // Reduce health to exactly the Phase2Threshold (60 %).
        ref var hp = ref world.GetComponent<HealthComponent>(boss);
        hp.CurrentHealth = 60f;

        world.Update(0.016f);

        var bossComp = world.GetComponent<BossComponent>(boss);
        Assert.Equal(BossPhase.Phase2, bossComp.Phase);
        Assert.True(bossComp.Phase2Triggered,
            "Phase2Triggered should be true on the transition frame.");
        world.Dispose();
    }

    [Fact]
    public void Boss_TransitionsToPhase3_AtTwentyFivePercentHealth()
    {
        var (world, _) = BuildWorld();
        var boss = AddBoss(world);

        // Jump straight to Phase2 first.
        ref var bossComp = ref world.GetComponent<BossComponent>(boss);
        bossComp.Phase = BossPhase.Phase2;

        ref var hp = ref world.GetComponent<HealthComponent>(boss);
        hp.CurrentHealth = 25f;

        world.Update(0.016f);

        bossComp = world.GetComponent<BossComponent>(boss);
        Assert.Equal(BossPhase.Phase3, bossComp.Phase);
        Assert.True(bossComp.Phase3Triggered,
            "Phase3Triggered should be true on the transition frame.");
        world.Dispose();
    }

    [Fact]
    public void Boss_StaysInPhase1_WhenHealthAboveThreshold()
    {
        var (world, _) = BuildWorld();
        var boss = AddBoss(world, currentHp: 80f);

        world.Update(0.016f);

        Assert.Equal(BossPhase.Phase1,
            world.GetComponent<BossComponent>(boss).Phase);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Defeat
    // -------------------------------------------------------------------------

    [Fact]
    public void Boss_MarkedDefeated_WhenHealthReachesZero()
    {
        var (world, _) = BuildWorld();
        var boss = AddBoss(world);

        ref var hp = ref world.GetComponent<HealthComponent>(boss);
        hp.CurrentHealth = 0f;
        hp.IsDead        = true;

        world.Update(0.016f);

        var bossComp = world.GetComponent<BossComponent>(boss);
        Assert.Equal(BossPhase.Defeated, bossComp.Phase);
        Assert.True(bossComp.DefeatedThisFrame,
            "DefeatedThisFrame should be true on the kill frame.");
        world.Dispose();
    }

    [Fact]
    public void BossDefeatedEvent_Published_OnKillFrame()
    {
        var (world, bossSystem) = BuildWorld();
        var boss = AddBoss(world);

        ref var hp = ref world.GetComponent<HealthComponent>(boss);
        hp.CurrentHealth = 0f;
        hp.IsDead        = true;

        world.Update(0.016f);

        Assert.Single(bossSystem.DefeatedEvents);
        Assert.Equal(boss, bossSystem.DefeatedEvents[0].BossEntity);
        world.Dispose();
    }

    [Fact]
    public void BossDefeatedEvent_NotPublished_WhenAlive()
    {
        var (world, bossSystem) = BuildWorld();
        AddBoss(world, currentHp: 50f);

        world.Update(0.016f);

        Assert.Empty(bossSystem.DefeatedEvents);
        world.Dispose();
    }

    [Fact]
    public void DefeatedEvents_ClearedEachFrame()
    {
        var (world, bossSystem) = BuildWorld();
        var boss = AddBoss(world);

        ref var hp = ref world.GetComponent<HealthComponent>(boss);
        hp.CurrentHealth = 0f;
        hp.IsDead        = true;

        world.Update(0.016f);  // defeat fires
        Assert.Single(bossSystem.DefeatedEvents);

        world.Update(0.016f);  // boss already Defeated — no repeat
        Assert.Empty(bossSystem.DefeatedEvents);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Single-frame flag reset
    // -------------------------------------------------------------------------

    [Fact]
    public void Phase2Triggered_ClearedOnNextFrame()
    {
        var (world, _) = BuildWorld();
        var boss = AddBoss(world);

        ref var hp = ref world.GetComponent<HealthComponent>(boss);
        hp.CurrentHealth = 60f;

        world.Update(0.016f);  // Phase2Triggered = true
        Assert.True(world.GetComponent<BossComponent>(boss).Phase2Triggered);

        world.Update(0.016f);  // flag cleared
        Assert.False(world.GetComponent<BossComponent>(boss).Phase2Triggered);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Enrage boosts
    // -------------------------------------------------------------------------

    [Fact]
    public void EnrageBoost_IncreasesEnemyRunSpeed_OnPhase2()
    {
        var (world, _) = BuildWorld();
        var boss = AddBoss(world);

        world.AddComponent(boss, EnemyAIComponent.Guard(Vector3.Zero));

        float speedBefore = world.GetComponent<EnemyAIComponent>(boss).RunSpeed;

        ref var hp = ref world.GetComponent<HealthComponent>(boss);
        hp.CurrentHealth = 60f;

        world.Update(0.016f);

        float speedAfter = world.GetComponent<EnemyAIComponent>(boss).RunSpeed;
        Assert.True(speedAfter > speedBefore,
            $"RunSpeed should increase on Phase2 enrage. Before={speedBefore}, After={speedAfter}.");
        world.Dispose();
    }

    [Fact]
    public void EnrageBoost_IncreasesDamage_OnPhase2()
    {
        var (world, _) = BuildWorld();
        var boss = AddBoss(world);
        world.AddComponent(boss, DamageComponent.MeleeDefault);

        float dmgBefore = world.GetComponent<DamageComponent>(boss).Damage;

        ref var hp = ref world.GetComponent<HealthComponent>(boss);
        hp.CurrentHealth = 60f;

        world.Update(0.016f);

        float dmgAfter = world.GetComponent<DamageComponent>(boss).Damage;
        Assert.True(dmgAfter > dmgBefore,
            $"Damage should increase on Phase2 enrage. Before={dmgBefore}, After={dmgAfter}.");
        world.Dispose();
    }
}
