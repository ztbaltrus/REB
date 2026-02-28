using REB.Engine.ECS;
using REB.Engine.Player.Princess.Components;
using REB.Engine.Player.Systems;

namespace REB.Engine.Player.Princess.Systems;

/// <summary>
/// Applies the princess's personality trait to her mood-decay rate every frame.
/// Runs after CarrySystem so it can read the settled carry state before modifying
/// the rate that will be used next frame.
/// <para>
/// Trait effects:
/// <list type="bullet">
///   <item>Cooperative  — 0.7× decay</item>
///   <item>Stubborn      — 1.3× decay</item>
///   <item>Excited       — 1.0 ± 0.3 oscillating decay</item>
///   <item>Scared        — 0.9× stable; 1.8× on carrier change</item>
/// </list>
/// </para>
/// </summary>
[RunAfter(typeof(CarrySystem))]
public sealed class TraitBehaviorSystem : GameSystem
{
    // Excited personality: oscillation frequency in cycles per second.
    private const float ExcitedFrequency = 0.5f;

    public override void Update(float deltaTime)
    {
        Entity princess = FindPrincess();
        if (!World.IsAlive(princess)) return;
        if (!World.HasComponent<PrincessTraitComponent>(princess)) return;
        if (!World.HasComponent<PrincessStateComponent>(princess)) return;

        ref var trait = ref World.GetComponent<PrincessTraitComponent>(princess);
        ref var ps    = ref World.GetComponent<PrincessStateComponent>(princess);

        float multiplier  = GetDecayMultiplier(ref trait, ref ps, deltaTime);
        ps.MoodDecayRate  = trait.BaseDecayRate * multiplier;

        // Record carrier for next frame's Scared comparison.
        trait.LastCarrierEntity = ps.CarrierEntity;
    }

    // =========================================================================
    //  Trait multipliers
    // =========================================================================

    private float GetDecayMultiplier(
        ref PrincessTraitComponent trait,
        ref PrincessStateComponent ps,
        float deltaTime)
    {
        switch (trait.Personality)
        {
            case PrincessPersonality.Cooperative:
                return 0.7f;

            case PrincessPersonality.Stubborn:
                return 1.3f;

            case PrincessPersonality.Excited:
                // Advance the oscillation phase and compute ±30 % swing.
                trait.ExcitedPhase += ExcitedFrequency * deltaTime * 2f * MathF.PI;
                return 1f + 0.3f * MathF.Sin(trait.ExcitedPhase);

            case PrincessPersonality.Scared:
                // Spike if the carrier just changed (both old and new must be alive).
                bool carrierChanged =
                    World.IsAlive(ps.CarrierEntity) &&
                    ps.CarrierEntity != trait.LastCarrierEntity &&
                    World.IsAlive(trait.LastCarrierEntity);
                return carrierChanged ? 1.8f : 0.9f;

            default:
                return 1f;
        }
    }

    // =========================================================================
    //  Helper
    // =========================================================================

    private Entity FindPrincess()
    {
        foreach (var e in World.GetEntitiesWithTag("Princess"))
            return e;
        return Entity.Null;
    }
}
