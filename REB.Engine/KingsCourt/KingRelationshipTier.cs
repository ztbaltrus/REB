namespace REB.Engine.KingsCourt;

/// <summary>
/// Persistent relationship tier between the crew and the King, derived from
/// <see cref="Components.KingRelationshipComponent.Score"/>.
/// Tiers apply a base payout multiplier bonus or penalty in
/// <see cref="Systems.PayoutCalculationSystem"/>.
/// </summary>
public enum KingRelationshipTier
{
    /// <summary>Score ≥ 80. King genuinely likes this crew. +20 % payout.</summary>
    Beloved,

    /// <summary>Score ≥ 60. King respects competence. +10 % payout.</summary>
    Respected,

    /// <summary>Score ≥ 40. Neutral professional relationship. No modifier.</summary>
    Known,

    /// <summary>Score ≥ 20. King suspects the crew is more trouble than they're worth. −10 %.</summary>
    Suspected,

    /// <summary>Score &lt; 20. King barely tolerates their existence. −25 % payout.</summary>
    Despised,
}
