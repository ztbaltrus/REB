namespace REB.Engine.Player.Princess;

/// <summary>
/// Published by MoodReactionSystem whenever the princess's ReactionMode changes.
/// UI and audio systems read this to display subtitles and trigger voice lines.
/// </summary>
public readonly record struct PrincessBarkEvent(
    string               Line,
    PrincessReactionMode ReactionMode,
    PrincessMoodLevel    MoodLevel);
