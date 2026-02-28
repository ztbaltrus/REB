using REB.Engine.ECS;

namespace REB.Engine.Player.Princess.Components;

/// <summary>
/// Tracks the princess's social trust/goodwill score (0–100) and the reaction mode
/// derived from it. Updated by MoodSystem; reaction applied by MoodReactionSystem.
/// </summary>
public struct PrincessGoodwillComponent : IComponent
{
    /// <summary>Current goodwill score. Starts at 50 (neutral).</summary>
    public float Goodwill;

    /// <summary>
    /// Current behavioural stance toward carriers, re-derived each frame from Goodwill.
    /// </summary>
    public PrincessReactionMode ReactionMode;

    /// <summary>
    /// Speed modifier applied to the active carrier's movement.
    /// 1.0 = neutral, >1 = helping (bonus), &lt;1 = hindering (penalty).
    /// Written by MoodReactionSystem; read by PlayerControllerSystem.
    /// </summary>
    public float CarrierSpeedModifier;

    /// <summary>Seconds until the princess is allowed to emit another bark line.</summary>
    public float DialogueCooldown;

    // -------------------------------------------------------------------------
    //  Thresholds (const so tests can reference them without boxing)
    // -------------------------------------------------------------------------

    /// <summary>Goodwill ≥ this → Helping mode.</summary>
    public const float HelpThreshold   = 70f;

    /// <summary>Goodwill ≤ this → Hindering mode.</summary>
    public const float HinderThreshold = 30f;

    public static PrincessGoodwillComponent Default => new()
    {
        Goodwill             = 50f,
        ReactionMode         = PrincessReactionMode.Neutral,
        CarrierSpeedModifier = 1f,
        DialogueCooldown     = 0f,
    };
}
