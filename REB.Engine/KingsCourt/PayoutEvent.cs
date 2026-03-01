namespace REB.Engine.KingsCourt;

/// <summary>
/// Published by <see cref="Systems.PayoutCalculationSystem"/> on the frame
/// the final payout is computed. Consumed by <see cref="Systems.KingRelationshipSystem"/>
/// and UI systems.
/// </summary>
public readonly record struct PayoutEvent(
    float             FinalPayout,
    KingReactionState KingReaction);
