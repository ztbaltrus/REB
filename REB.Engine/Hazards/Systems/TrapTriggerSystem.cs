using Microsoft.Xna.Framework;
using REB.Engine.Combat.Components;
using REB.Engine.ECS;
using REB.Engine.Hazards.Components;
using REB.Engine.Physics.Systems;
using REB.Engine.Rendering.Components;

namespace REB.Engine.Hazards.Systems;

/// <summary>
/// Drives environmental hazard state machines and applies damage to overlapping entities.
/// <para>Per-frame pipeline per hazard:</para>
/// <list type="number">
///   <item>Advance SwingingBlade oscillation phase.</item>
///   <item><b>Armed</b>: check for players/princess within trigger radius and transition to Triggered.</item>
///   <item><b>Triggered</b>: apply damage each frame to overlapping entities; count down TriggeredDuration.</item>
///   <item><b>Resetting</b>: count down ResetTimer; return to Armed when elapsed.</item>
/// </list>
/// The princess receives double damage from all hazard types.
/// </summary>
[RunAfter(typeof(PhysicsSystem))]
public sealed class TrapTriggerSystem : GameSystem
{
    public override void Update(float deltaTime)
    {
        var mortals = GatherMortals();

        foreach (var hazard in World.Query<HazardComponent, TransformComponent>())
        {
            ref var hz     = ref World.GetComponent<HazardComponent>(hazard);
            var     hazPos = World.GetComponent<TransformComponent>(hazard).Position;

            // SwingingBlade: advance phase and test overlap every frame regardless of state.
            if (hz.Type == HazardType.SwingingBlade && hz.OscillationPeriod > 0f)
            {
                hz.OscillationPhase += deltaTime * (MathF.Tau / hz.OscillationPeriod);
                if (hz.OscillationPhase > MathF.Tau)
                    hz.OscillationPhase -= MathF.Tau;

                ApplyBladeOverlap(ref hz, hazPos, mortals);
                continue;
            }

            switch (hz.State)
            {
                case HazardState.Armed:
                    UpdateArmed(ref hz, hazPos, mortals);
                    break;

                case HazardState.Triggered:
                    UpdateTriggered(ref hz, hazPos, mortals, deltaTime);
                    break;

                case HazardState.Resetting:
                    UpdateResetting(ref hz, deltaTime);
                    break;
            }
        }
    }

    // =========================================================================
    //  State handlers
    // =========================================================================

    private void UpdateArmed(
        ref HazardComponent hz, Vector3 hazPos,
        List<(Entity entity, Vector3 pos, bool isPrincess)> mortals)
    {
        bool triggered = false;
        foreach (var (entity, pos, isPrincess) in mortals)
        {
            if (Vector3.Distance(hazPos, pos) <= hz.TriggerRadius)
            {
                if (!triggered)
                {
                    hz.State          = HazardState.Triggered;
                    hz.TriggeredTimer = hz.TriggeredDuration;
                    triggered         = true;
                }
                // Damage fires on the trigger frame so the first contact is not free.
                ApplyDamage(entity, hz.Damage, isPrincess);
            }
        }
    }

    private void UpdateTriggered(
        ref HazardComponent hz, Vector3 hazPos,
        List<(Entity entity, Vector3 pos, bool isPrincess)> mortals,
        float dt)
    {
        foreach (var (entity, pos, isPrincess) in mortals)
            if (Vector3.Distance(hazPos, pos) <= hz.TriggerRadius + 0.1f)
                ApplyDamage(entity, hz.Damage, isPrincess);

        hz.TriggeredTimer -= dt;
        if (hz.TriggeredTimer <= 0f)
        {
            if (hz.ResetTime > 0f)
            {
                hz.State      = HazardState.Resetting;
                hz.ResetTimer = hz.ResetTime;
            }
            // Pits with ResetTime == 0 stay in Triggered (one-way fall).
        }
    }

    private static void UpdateResetting(ref HazardComponent hz, float dt)
    {
        hz.ResetTimer -= dt;
        if (hz.ResetTimer <= 0f)
            hz.State = HazardState.Armed;
    }

    private void ApplyBladeOverlap(
        ref HazardComponent hz, Vector3 hazPos,
        List<(Entity entity, Vector3 pos, bool isPrincess)> mortals)
    {
        float bladeX = hazPos.X + MathF.Sin(hz.OscillationPhase) * hz.OscillationHalfWidth;

        foreach (var (entity, pos, isPrincess) in mortals)
        {
            if (MathF.Abs(pos.X - bladeX)  < 0.5f &&
                MathF.Abs(pos.Z - hazPos.Z) < 1.0f &&
                MathF.Abs(pos.Y - hazPos.Y) < 1.5f)
            {
                // Per-frame rate (full damage * dt so 1 s contact = full Damage).
                ApplyDamage(entity, hz.Damage * 0.05f, isPrincess);
            }
        }
    }

    // =========================================================================
    //  Damage helper
    // =========================================================================

    private void ApplyDamage(Entity entity, float baseDamage, bool isPrincess)
    {
        if (!World.HasComponent<HealthComponent>(entity)) return;

        ref var hp = ref World.GetComponent<HealthComponent>(entity);
        if (hp.IsDead || hp.IsInvulnerable) return;

        float damage = isPrincess ? baseDamage * 2f : baseDamage;
        hp.CurrentHealth = MathHelper.Clamp(hp.CurrentHealth - damage, 0f, hp.MaxHealth);
        if (hp.CurrentHealth <= 0f)
            hp.IsDead = true;
    }

    // =========================================================================
    //  Target gathering
    // =========================================================================

    private List<(Entity entity, Vector3 pos, bool isPrincess)> GatherMortals()
    {
        var list = new List<(Entity, Vector3, bool)>();

        foreach (var e in World.GetEntitiesWithTag("Player"))
            if (World.HasComponent<TransformComponent>(e) && World.HasComponent<HealthComponent>(e))
                list.Add((e, World.GetComponent<TransformComponent>(e).Position, false));

        foreach (var e in World.GetEntitiesWithTag("Princess"))
            if (World.HasComponent<TransformComponent>(e) && World.HasComponent<HealthComponent>(e))
                list.Add((e, World.GetComponent<TransformComponent>(e).Position, true));

        return list;
    }
}
