namespace REB.Engine.Player.Princess;

/// <summary>
/// A per-run personality trait that modifies how difficult the princess is to carry.
/// Assigned once at run start (seeded random or explicit) via PrincessTraitComponent.
/// </summary>
public enum PrincessPersonality
{
    /// <summary>Willing participant. 0.7× mood-decay rate; slower to reach Furious.</summary>
    Cooperative,

    /// <summary>Difficult customer. 1.3× decay rate; reaches Furious faster.</summary>
    Stubborn,

    /// <summary>Unpredictable energy. Decay rate oscillates ±30 % over time.</summary>
    Excited,

    /// <summary>Skittish. 0.9× decay normally; spikes to 1.8× whenever the carrier changes.</summary>
    Scared,
}
