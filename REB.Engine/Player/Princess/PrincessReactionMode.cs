namespace REB.Engine.Player.Princess;

/// <summary>
/// How the princess is currently behaving toward her carriers, derived each frame
/// from the PrincessGoodwillComponent.Goodwill score by MoodReactionSystem.
/// </summary>
public enum PrincessReactionMode
{
    /// <summary>Goodwill ≥ 70 — actively cooperating; carrier receives a speed bonus.</summary>
    Helping,

    /// <summary>Goodwill 30–69 — passive; no speed modifier.</summary>
    Neutral,

    /// <summary>Goodwill &lt; 30 — resistant; carrier receives a speed penalty.</summary>
    Hindering,
}
