namespace REB.Engine.Player.Princess;

/// <summary>The princess's current emotional state, driven by her health/mood meter.</summary>
public enum PrincessMoodLevel
{
    /// <summary>Health ≥ 75 — cooperative, no penalty.</summary>
    Calm,

    /// <summary>Health 50–74 — occasional complaints, minor speed penalty.</summary>
    Nervous,

    /// <summary>Health 25–49 — active resistance, moderate speed penalty.</summary>
    Upset,

    /// <summary>Health &lt; 25 — struggles violently, causing carrier stumble.</summary>
    Furious,
}
