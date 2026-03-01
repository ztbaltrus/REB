namespace REB.Engine.KingsCourt;

/// <summary>
/// An immutable record of a single completed run, stored in
/// <see cref="Components.KingRelationshipComponent"/> as part of the run history log.
/// </summary>
public readonly record struct RunHistoryEntry(
    float LootGoldValue,
    float PrincessHealth,
    bool  PrincessDelivered,
    bool  BossDefeated,
    float FinalPayout);
