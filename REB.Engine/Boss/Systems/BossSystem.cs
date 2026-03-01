using Microsoft.Xna.Framework;
using REB.Engine.Boss.Components;
using REB.Engine.Combat.Components;
using REB.Engine.Combat.Systems;
using REB.Engine.ECS;
using REB.Engine.Enemy.Components;
using REB.Engine.Rendering.Components;

namespace REB.Engine.Boss.Systems;

/// <summary>
/// Governs boss phase transitions and publishes <see cref="BossDefeatedEvent"/> on death.
/// <para>Per-frame pipeline:</para>
/// <list type="number">
///   <item>Clear single-frame transition flags.</item>
///   <item>Check health fraction against phase thresholds; apply enrage boosts on transition.</item>
///   <item>When health reaches zero, mark Phase as Defeated and fire the defeated event.</item>
/// </list>
/// The boss entity itself is destroyed by <see cref="DeathSystem"/> — this system only
/// manages phase logic and event publication.
/// </summary>
[RunAfter(typeof(CombatSystem))]
public sealed class BossSystem : GameSystem
{
    /// <summary>Defeat events published this frame. Cleared at the start of each update.</summary>
    public IReadOnlyList<BossDefeatedEvent> DefeatedEvents => _events;

    private readonly List<BossDefeatedEvent> _events = new();

    public override void Update(float deltaTime)
    {
        _events.Clear();

        foreach (var entity in World.Query<BossComponent, HealthComponent>())
        {
            ref var boss = ref World.GetComponent<BossComponent>(entity);
            ref var hp   = ref World.GetComponent<HealthComponent>(entity);

            // Clear single-frame flags.
            boss.Phase2Triggered   = false;
            boss.Phase3Triggered   = false;
            boss.DefeatedThisFrame = false;

            if (boss.Phase == BossPhase.Defeated) continue;

            if (hp.IsDead || hp.HealthFraction <= 0f)
            {
                TransitionToDefeated(entity, ref boss);
                continue;
            }

            float fraction = hp.HealthFraction;

            if (boss.Phase == BossPhase.Phase1 && fraction <= boss.Phase2Threshold)
            {
                boss.Phase           = BossPhase.Phase2;
                boss.Phase2Triggered = true;
                ApplyEnrageBoosts(entity, ref boss);
            }
            else if (boss.Phase == BossPhase.Phase2 && fraction <= boss.Phase3Threshold)
            {
                boss.Phase           = BossPhase.Phase3;
                boss.Phase3Triggered = true;

                // Phase3: additional speed burst on top of the Phase2 boost.
                if (World.HasComponent<EnemyAIComponent>(entity))
                {
                    ref var ai = ref World.GetComponent<EnemyAIComponent>(entity);
                    ai.RunSpeed  *= 1.2f;
                    ai.WalkSpeed *= 1.2f;
                }
            }
        }
    }

    // =========================================================================
    //  Transition helpers
    // =========================================================================

    private void TransitionToDefeated(Entity entity, ref BossComponent boss)
    {
        boss.Phase             = BossPhase.Defeated;
        boss.DefeatedThisFrame = true;

        var pos = World.HasComponent<TransformComponent>(entity)
            ? World.GetComponent<TransformComponent>(entity).Position
            : Vector3.Zero;

        _events.Add(new BossDefeatedEvent(entity, pos, boss.LootSeed));
    }

    private void ApplyEnrageBoosts(Entity entity, ref BossComponent boss)
    {
        if (World.HasComponent<DamageComponent>(entity))
        {
            ref var dmg = ref World.GetComponent<DamageComponent>(entity);
            dmg.Damage         *= boss.EnrageDamageMultiplier;
            dmg.AttackCooldown /= boss.EnrageDamageMultiplier; // also attacks faster
        }

        if (World.HasComponent<EnemyAIComponent>(entity))
        {
            ref var ai = ref World.GetComponent<EnemyAIComponent>(entity);
            ai.RunSpeed  *= boss.EnrageSpeedMultiplier;
            ai.WalkSpeed *= boss.EnrageSpeedMultiplier;
        }
    }
}
