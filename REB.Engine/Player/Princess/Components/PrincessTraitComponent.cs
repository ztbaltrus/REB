using REB.Engine.ECS;

namespace REB.Engine.Player.Princess.Components;

/// <summary>
/// Personality trait assigned to the princess at run start. Drives TraitBehaviorSystem's
/// per-frame adjustment of PrincessStateComponent.MoodDecayRate.
/// </summary>
public struct PrincessTraitComponent : IComponent
{
    /// <summary>The personality assigned this run.</summary>
    public PrincessPersonality Personality;

    /// <summary>Seed used for reproducible trait selection.</summary>
    public int TraitSeed;

    /// <summary>
    /// Base mood-decay rate (points/second) before the trait multiplier is applied.
    /// Mirrors PrincessStateComponent.Default.MoodDecayRate and is preserved so
    /// TraitBehaviorSystem can always restore the un-modified value.
    /// </summary>
    public float BaseDecayRate;

    /// <summary>
    /// Carrier entity observed last frame; used by Scared personality to detect handoffs.
    /// </summary>
    public Entity LastCarrierEntity;

    /// <summary>
    /// Oscillation angle (radians) advanced each frame for Excited personality.
    /// </summary>
    public float ExcitedPhase;

    // -------------------------------------------------------------------------
    //  Factory helpers
    // -------------------------------------------------------------------------

    /// <summary>Selects a personality pseudo-randomly from the given seed.</summary>
    public static PrincessTraitComponent Random(int seed) => new()
    {
        Personality       = (PrincessPersonality)(new System.Random(seed).Next(4)),
        TraitSeed         = seed,
        BaseDecayRate     = 2f,
        LastCarrierEntity = Entity.Null,
    };

    /// <summary>Assigns a specific personality (useful for tests and designer overrides).</summary>
    public static PrincessTraitComponent ForPersonality(PrincessPersonality p) => new()
    {
        Personality       = p,
        TraitSeed         = 0,
        BaseDecayRate     = 2f,
        LastCarrierEntity = Entity.Null,
    };
}
