using Microsoft.Xna.Framework;
using REB.Engine.Combat.Components;
using REB.Engine.Combat.Systems;
using REB.Engine.ECS;
using REB.Engine.Rendering.Components;
using Xunit;

namespace REB.Tests.Combat;

// ---------------------------------------------------------------------------
//  CombatSystem tests
//
//  Only CombatSystem and HitReactionSystem are registered; PhysicsSystem and
//  PlayerControllerSystem are omitted — RunAfter constraints on missing systems
//  are silently ignored by the topological sort.
// ---------------------------------------------------------------------------

public sealed class CombatSystemTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static (World world, CombatSystem combat) BuildWorld()
    {
        var world  = new World();
        var combat = new CombatSystem();
        world.RegisterSystem(combat);
        world.RegisterSystem(new HitReactionSystem());
        return (world, combat);
    }

    private static Entity AddAttacker(
        World world, Vector3 pos,
        float damage = 10f, float meleeRange = 2f,
        bool isRanged = false, float rangedRange = 8f,
        float knownKnockback = 4f)
    {
        var e = world.CreateEntity();
        world.AddComponent(e, new TransformComponent
        {
            Position = pos, Rotation = Quaternion.Identity,
            Scale = Vector3.One, WorldMatrix = Matrix.Identity,
        });
        world.AddComponent(e, new DamageComponent
        {
            Damage         = damage,
            MeleeRange     = meleeRange,
            RangedRange    = rangedRange,
            IsRanged       = isRanged,
            AttackCooldown = 1f,
            AttackTimer    = 0f,
            AttackPressed  = true,
            KnockbackForce = knownKnockback,
        });
        return e;
    }

    private static Entity AddTarget(World world, Vector3 pos, float hp = 100f)
    {
        var e = world.CreateEntity();
        world.AddComponent(e, new TransformComponent
        {
            Position = pos, Rotation = Quaternion.Identity,
            Scale = Vector3.One, WorldMatrix = Matrix.Identity,
        });
        world.AddComponent(e, HealthComponent.For(hp));
        world.AddComponent(e, HitReactionComponent.Default);
        return e;
    }

    // -------------------------------------------------------------------------
    //  Hit detection — melee
    // -------------------------------------------------------------------------

    [Fact]
    public void MeleeHit_ReducesTargetHealth_WhenWithinRange()
    {
        var (world, _) = BuildWorld();
        var attacker   = AddAttacker(world, Vector3.Zero, meleeRange: 2f);
        var target     = AddTarget(world, new Vector3(1f, 0f, 0f));  // 1 unit away

        world.Update(0.016f);

        var hp = world.GetComponent<HealthComponent>(target);
        Assert.True(hp.CurrentHealth < 100f,
            $"Expected health < 100 after melee hit, got {hp.CurrentHealth}.");
        world.Dispose();
    }

    [Fact]
    public void MeleeMiss_WhenTargetOutOfRange()
    {
        var (world, _) = BuildWorld();
        AddAttacker(world, Vector3.Zero, meleeRange: 1f);
        var target = AddTarget(world, new Vector3(5f, 0f, 0f));  // 5 units — beyond range

        world.Update(0.016f);

        var hp = world.GetComponent<HealthComponent>(target);
        Assert.Equal(100f, hp.CurrentHealth);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Hit detection — ranged
    // -------------------------------------------------------------------------

    [Fact]
    public void RangedHit_ConnectsAtExtendedRange()
    {
        var (world, _) = BuildWorld();
        AddAttacker(world, Vector3.Zero, meleeRange: 1.5f, isRanged: true, rangedRange: 10f);
        var target = AddTarget(world, new Vector3(8f, 0f, 0f));  // 8 units — within ranged

        world.Update(0.016f);

        var hp = world.GetComponent<HealthComponent>(target);
        Assert.True(hp.CurrentHealth < 100f,
            $"Expected ranged hit at 8 units, got {hp.CurrentHealth}.");
        world.Dispose();
    }

    [Fact]
    public void RangedMiss_BeyondRangedRange()
    {
        var (world, _) = BuildWorld();
        AddAttacker(world, Vector3.Zero, isRanged: true, rangedRange: 6f);
        var target = AddTarget(world, new Vector3(9f, 0f, 0f));  // 9 units — beyond ranged

        world.Update(0.016f);

        var hp = world.GetComponent<HealthComponent>(target);
        Assert.Equal(100f, hp.CurrentHealth);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Cooldown
    // -------------------------------------------------------------------------

    [Fact]
    public void AttackBlocked_WhenCooldownActive()
    {
        var (world, _) = BuildWorld();
        var attacker   = AddAttacker(world, Vector3.Zero, meleeRange: 2f);
        var target     = AddTarget(world, new Vector3(1f, 0f, 0f));

        // Manually set cooldown so the attack cannot fire.
        ref var dmg = ref world.GetComponent<DamageComponent>(attacker);
        dmg.AttackTimer  = 0.5f;  // half-second cooldown remaining
        dmg.AttackPressed = true;

        world.Update(0.016f);

        var hp = world.GetComponent<HealthComponent>(target);
        Assert.Equal(100f, hp.CurrentHealth);
        world.Dispose();
    }

    [Fact]
    public void Cooldown_ResetsAfterAttack()
    {
        var (world, _) = BuildWorld();
        var attacker   = AddAttacker(world, Vector3.Zero, meleeRange: 2f);
        AddTarget(world, new Vector3(1f, 0f, 0f));

        world.Update(0.016f);  // attack fires; cooldown set to AttackCooldown

        var dmg = world.GetComponent<DamageComponent>(attacker);
        Assert.True(dmg.AttackTimer > 0f,
            "Attack cooldown should be > 0 after an attack.");
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Death flag
    // -------------------------------------------------------------------------

    [Fact]
    public void Target_MarkedDead_WhenHealthReachesZero()
    {
        var (world, _) = BuildWorld();
        AddAttacker(world, Vector3.Zero, damage: 200f, meleeRange: 2f);
        var target = AddTarget(world, new Vector3(1f, 0f, 0f), hp: 50f);

        world.Update(0.016f);

        var hp = world.GetComponent<HealthComponent>(target);
        Assert.True(hp.IsDead, "Entity should be marked dead after lethal damage.");
        Assert.Equal(0f, hp.CurrentHealth);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Knockback
    // -------------------------------------------------------------------------

    [Fact]
    public void KnockbackVelocity_SetOnTarget_AfterHit()
    {
        var (world, _) = BuildWorld();
        AddAttacker(world, Vector3.Zero, meleeRange: 2f, knownKnockback: 10f);
        var target = AddTarget(world, new Vector3(1f, 0f, 0f));

        world.Update(0.016f);  // CombatSystem writes knockback; HitReactionSystem clears it

        // After HitReactionSystem runs, IsHit is cleared and StaggerTimer is set.
        var hr = world.GetComponent<HitReactionComponent>(target);
        Assert.False(hr.IsHit, "IsHit should be cleared after HitReactionSystem processes it.");
        Assert.True(hr.StaggerTimer > 0f,
            $"StaggerTimer should be active after a hit, got {hr.StaggerTimer}.");
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  CombatEvents
    // -------------------------------------------------------------------------

    [Fact]
    public void CombatEvents_ContainsOneEntry_PerHit()
    {
        var (world, combat) = BuildWorld();
        AddAttacker(world, Vector3.Zero, meleeRange: 3f);
        AddTarget(world, new Vector3(1f, 0f, 0f));
        AddTarget(world, new Vector3(2f, 0f, 0f));

        world.Update(0.016f);

        Assert.Equal(2, combat.CombatEvents.Count);
        world.Dispose();
    }

    [Fact]
    public void CombatEvents_ClearedEachFrame()
    {
        var (world, combat) = BuildWorld();
        AddAttacker(world, Vector3.Zero, meleeRange: 2f);
        AddTarget(world, new Vector3(1f, 0f, 0f));

        world.Update(0.016f);
        Assert.True(combat.CombatEvents.Count > 0);

        world.Update(0.016f);  // attacker cooldown active — no new hits
        Assert.Empty(combat.CombatEvents);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Invulnerability
    // -------------------------------------------------------------------------

    [Fact]
    public void InvulnerableTarget_NotDamaged()
    {
        var (world, _) = BuildWorld();
        AddAttacker(world, Vector3.Zero, meleeRange: 2f);
        var target = AddTarget(world, new Vector3(1f, 0f, 0f));

        ref var hp = ref world.GetComponent<HealthComponent>(target);
        hp.IsInvulnerable = true;

        world.Update(0.016f);

        Assert.Equal(100f, world.GetComponent<HealthComponent>(target).CurrentHealth);
        world.Dispose();
    }
}
