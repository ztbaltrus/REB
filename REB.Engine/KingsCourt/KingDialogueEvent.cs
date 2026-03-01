using REB.Engine.ECS;

namespace REB.Engine.KingsCourt;

/// <summary>
/// Published by <see cref="Systems.KingsCourtSceneSystem"/> and
/// <see cref="Systems.NegotiationMinigameSystem"/> whenever the King speaks.
/// UI and audio systems consume these to display subtitles and trigger voice lines.
/// </summary>
public readonly record struct KingDialogueEvent(
    Entity             KingEntity,
    KingsCourtPhase    Phase,
    KingReactionState  Reaction,
    string             LineKey);
