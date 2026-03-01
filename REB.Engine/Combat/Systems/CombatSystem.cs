using Microsoft.Xna.Framework;
using REB.Engine.Combat.Components;
using REB.Engine.ECS;
using REB.Engine.Physics.Systems;
using REB.Engine.Player.Systems;
using REB.Engine.Rendering.Components;

namespace REB.Engine.Combat.Systems;

/// <summary>
/// Core combat resolution system.
/// <para>Per-frame pipeline:</para>
/// <list type="number">
///   <item>Decrement all attack cooldown timers.</item>
///   <item>For each attacker with <c>AttackPressed = true</c> and a ready cooldown,
///         find all <see cref="HealthComponent"/> entities within effective attack range.</item>
///   <item>Apply damage, flag IsDead if health reaches zero, and write knockback
///         impulse into <see cref="HitReactionComponent"/>.</item>
///   <item>Publish a <see cref="CombatEvent"/> per hit for downstream systems.</item>
/// </list>
/// </summary>
[RunAfter(typeof(PhysicsSystem))]
[RunAfter(typeof(PlayerControllerSystem))]
public sealed class CombatSystem : GameSystem
{
    /// <summary>All damage events generated this frame. Cleared at the start of each update.</summary>
    public IReadOnlyList<CombatEvent> CombatEvents => _events;

    private readonly List<CombatEvent> _events = new();

    public override void Update(float deltaTime)
    {
        _events.Clear();
        TickCooldowns(deltaTime);
        ProcessAttacks();
    }

    // =========================================================================
    //  Cooldown management
    // =========================================================================

    private void TickCooldowns(float dt)
    {
        foreach (var entity in World.Query<DamageComponent>())
        {
            ref var dmg = ref World.GetComponent<DamageComponent>(entity);
            if (dmg.AttackTimer > 0f)
                dmg.AttackTimer -= dt;
        }
    }

    // =========================================================================
    //  Attack resolution
    // =========================================================================

    private void ProcessAttacks()
    {
        // Snapshot targets so attacker iteration can mutate HealthComponents freely.
        var targets = new List<(Entity entity, Vector3 pos)>();
        foreach (var t in World.Query<HealthComponent, TransformComponent>())
        {
            var hp = World.GetComponent<HealthComponent>(t);
            if (hp.IsDead || hp.IsInvulnerable) continue;
            targets.Add((t, World.GetComponent<TransformComponent>(t).Position));
        }

        foreach (var attacker in World.Query<DamageComponent, TransformComponent>())
        {
            ref var dmg = ref World.GetComponent<DamageComponent>(attacker);

            if (!dmg.AttackPressed || dmg.AttackTimer > 0f) continue;

            dmg.AttackPressed = false;          // consume the press
            dmg.AttackTimer   = dmg.AttackCooldown;

            var   attackerPos    = World.GetComponent<TransformComponent>(attacker).Position;
            float effectiveRange = dmg.IsRanged ? dmg.RangedRange : dmg.MeleeRange;

            foreach (var (target, targetPos) in targets)
            {
                if (target == attacker) continue;

                if (Vector3.Distance(attackerPos, targetPos) > effectiveRange) continue;

                // Apply damage.
                ref var hp = ref World.GetComponent<HealthComponent>(target);
                hp.CurrentHealth = MathHelper.Clamp(
                    hp.CurrentHealth - dmg.Damage, 0f, hp.MaxHealth);
                if (hp.CurrentHealth <= 0f)
                    hp.IsDead = true;

                // Write knockback impulse.
                var kbDir = targetPos - attackerPos;
                float len = kbDir.Length();
                kbDir = len > 1e-6f ? kbDir / len : Vector3.Forward;

                if (World.HasComponent<HitReactionComponent>(target))
                {
                    ref var hr = ref World.GetComponent<HitReactionComponent>(target);
                    hr.IsHit             = true;
                    hr.KnockbackVelocity = kbDir * dmg.KnockbackForce;
                    hr.StaggerTimer      = hr.StaggerDuration;
                }

                _events.Add(new CombatEvent(
                    attacker, target,
                    dmg.Damage,
                    (attackerPos + targetPos) * 0.5f,
                    kbDir));
            }
        }
    }
}
