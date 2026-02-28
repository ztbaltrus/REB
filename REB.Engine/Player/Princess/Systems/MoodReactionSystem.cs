using REB.Engine.ECS;
using REB.Engine.Player.Princess.Components;

namespace REB.Engine.Player.Princess.Systems;

/// <summary>
/// Derives the princess's ReactionMode from her Goodwill score and applies the
/// corresponding CarrierSpeedModifier. Fires a PrincessBarkEvent whenever the
/// reaction mode changes (subject to DialogueCooldown).
/// <para>
/// Mode thresholds: Helping ≥ 70, Hindering ≤ 30, Neutral in between.
/// Speed modifiers: Helping 1.10, Neutral 1.00, Hindering 0.85.
/// </para>
/// </summary>
[RunAfter(typeof(MoodSystem))]
public sealed class MoodReactionSystem : GameSystem
{
    private readonly List<PrincessBarkEvent> _barks = new();

    /// <summary>All bark events that fired this frame. Cleared at the start of each update.</summary>
    public IReadOnlyList<PrincessBarkEvent> Barks => _barks;

    // Seconds of silence enforced between bark lines.
    private const float BarkCooldown = 4f;

    public override void Update(float deltaTime)
    {
        _barks.Clear();

        Entity princess = FindPrincess();
        if (!World.IsAlive(princess)) return;
        if (!World.HasComponent<PrincessGoodwillComponent>(princess)) return;
        if (!World.HasComponent<PrincessStateComponent>(princess)) return;

        ref var gw = ref World.GetComponent<PrincessGoodwillComponent>(princess);
        ref var ps = ref World.GetComponent<PrincessStateComponent>(princess);

        PrincessReactionMode newMode = gw.Goodwill >= PrincessGoodwillComponent.HelpThreshold
            ? PrincessReactionMode.Helping
            : gw.Goodwill <= PrincessGoodwillComponent.HinderThreshold
                ? PrincessReactionMode.Hindering
                : PrincessReactionMode.Neutral;

        // Emit a bark on mode change (if cooldown permits).
        if (newMode != gw.ReactionMode && gw.DialogueCooldown <= 0f)
        {
            _barks.Add(new PrincessBarkEvent(
                GetBarkLine(newMode, ps.MoodLevel),
                newMode,
                ps.MoodLevel));
            gw.DialogueCooldown = BarkCooldown;
        }

        gw.ReactionMode = newMode;

        // Apply speed modifier to the active carrier.
        gw.CarrierSpeedModifier = newMode switch
        {
            PrincessReactionMode.Helping   => 1.10f,
            PrincessReactionMode.Neutral   => 1.00f,
            PrincessReactionMode.Hindering => 0.85f,
            _                              => 1.00f,
        };
    }

    // =========================================================================
    //  Dialogue lines
    // =========================================================================

    private static string GetBarkLine(PrincessReactionMode mode, PrincessMoodLevel mood) =>
        (mode, mood) switch
        {
            (PrincessReactionMode.Helping, _)                              =>
                "Fine, I'll cooperate... just don't drop me again.",
            (PrincessReactionMode.Hindering, PrincessMoodLevel.Furious)   =>
                "PUT ME DOWN THIS INSTANT!",
            (PrincessReactionMode.Hindering, PrincessMoodLevel.Upset)     =>
                "This is absolutely unacceptable!",
            (PrincessReactionMode.Hindering, _)                           =>
                "I am NOT amused.",
            _                                                              =>
                "...",
        };

    // =========================================================================
    //  Helper
    // =========================================================================

    private Entity FindPrincess()
    {
        foreach (var e in World.GetEntitiesWithTag("Princess"))
            return e;
        return Entity.Null;
    }
}
