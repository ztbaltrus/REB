using REB.Engine.ECS;
using REB.Engine.KingsCourt.Components;

namespace REB.Engine.KingsCourt.Systems;

/// <summary>
/// Manages the King's Court end-of-run scene.
/// <para>Per-frame pipeline:</para>
/// <list type="number">
///   <item>If not active: watch for <see cref="RunSummaryComponent.IsComplete"/>; start scene on detection.</item>
///   <item>Advance phase timer; transition phases when their duration elapses.</item>
///   <item>Fire <see cref="KingDialogueEvent"/>s on phase entry.</item>
///   <item>Compute <see cref="KingReactionState"/> from run quality when entering Review.</item>
/// </list>
/// </summary>
public sealed class KingsCourtSceneSystem : GameSystem
{
    // ── Dialogue line tables keyed by [reactionIndex][lineIndex] ──────────────

    private static readonly string[][] ArrivalLines =
    [
        // Pleased
        ["king.pleased.arrival.1", "king.pleased.arrival.2"],
        // Neutral
        ["king.neutral.arrival.1", "king.neutral.arrival.2"],
        // Dissatisfied
        ["king.dissatisfied.arrival.1", "king.dissatisfied.arrival.2"],
        // Furious
        ["king.furious.arrival.1", "king.furious.arrival.2"],
    ];

    private static readonly string[][] ReviewLines =
    [
        ["king.pleased.review.1",       "king.pleased.review.2"],
        ["king.neutral.review.1",       "king.neutral.review.2"],
        ["king.dissatisfied.review.1",  "king.dissatisfied.review.2"],
        ["king.furious.review.1",       "king.furious.review.2"],
    ];

    private static readonly string[][] NegotiationOpenLines =
    [
        ["king.pleased.negotiation.1"],
        ["king.neutral.negotiation.1"],
        ["king.dissatisfied.negotiation.1"],
        ["king.furious.negotiation.1"],
    ];

    private static readonly string[][] PayoutLines =
    [
        ["king.pleased.payout.1"],
        ["king.neutral.payout.1"],
        ["king.dissatisfied.payout.1"],
        ["king.furious.payout.1"],
    ];

    private static readonly string[][] DismissalLines =
    [
        ["king.pleased.dismissal.1"],
        ["king.neutral.dismissal.1"],
        ["king.dissatisfied.dismissal.1"],
        ["king.furious.dismissal.1"],
    ];

    // ── Public events ─────────────────────────────────────────────────────────

    /// <summary>Dialogue events fired this frame. Cleared at the start of each update.</summary>
    public IReadOnlyList<KingDialogueEvent> DialogueEvents => _dialogue;

    private readonly List<KingDialogueEvent> _dialogue = new();

    // =========================================================================
    //  Update
    // =========================================================================

    public override void Update(float deltaTime)
    {
        _dialogue.Clear();

        Entity king = FindKing();
        if (!World.IsAlive(king)) return;

        ref var ks = ref World.GetComponent<KingStateComponent>(king);

        if (!ks.SceneActive)
        {
            TryStartScene(king, ref ks);
            return;
        }

        ks.PhaseTimer += deltaTime;
        AdvancePhase(king, ref ks);
    }

    // =========================================================================
    //  Scene start
    // =========================================================================

    private void TryStartScene(Entity king, ref KingStateComponent ks)
    {
        Entity summary = FindRunSummary();
        if (!World.IsAlive(summary)) return;

        var rs = World.GetComponent<RunSummaryComponent>(summary);
        if (!rs.IsComplete) return;

        ks.SceneActive       = true;
        ks.PayoutCalculated  = false;
        ks.ReactionState     = ComputeReaction(rs);
        TransitionTo(king, ref ks, KingsCourtPhase.Arriving);
    }

    // =========================================================================
    //  Phase advancement
    // =========================================================================

    private void AdvancePhase(Entity king, ref KingStateComponent ks)
    {
        switch (ks.Phase)
        {
            case KingsCourtPhase.Arriving:
                if (ks.PhaseTimer >= ks.ArrivingDuration)
                    TransitionTo(king, ref ks, KingsCourtPhase.Review);
                break;

            case KingsCourtPhase.Review:
                if (ks.PhaseTimer >= ks.ReviewDuration)
                    TransitionTo(king, ref ks, KingsCourtPhase.Negotiation);
                break;

            case KingsCourtPhase.Negotiation:
                if (ks.PhaseTimer >= ks.NegotiationDuration)
                    TransitionTo(king, ref ks, KingsCourtPhase.Payout);
                break;

            case KingsCourtPhase.Payout:
                if (ks.PhaseTimer >= ks.PayoutDuration)
                    TransitionTo(king, ref ks, KingsCourtPhase.Dismissed);
                break;

            case KingsCourtPhase.Dismissed:
                ks.SceneActive = false;
                // Prevent the scene from re-triggering on subsequent frames
                // by marking the run summary as no longer pending.
                ClearRunSummaryComplete();
                break;
        }
    }

    private void TransitionTo(Entity king, ref KingStateComponent ks, KingsCourtPhase newPhase)
    {
        ks.Phase      = newPhase;
        ks.PhaseTimer = 0f;

        int ri = (int)ks.ReactionState;

        switch (newPhase)
        {
            case KingsCourtPhase.Arriving:
                FireLine(king, ks, ArrivalLines[ri]);
                break;
            case KingsCourtPhase.Review:
                // Fire 1-2 review lines based on reaction.
                FireLine(king, ks, ReviewLines[ri]);
                if ((int)ks.ReactionState <= (int)KingReactionState.Neutral)
                    FireLine(king, ks, ReviewLines[ri], 1); // second line for good reactions
                break;
            case KingsCourtPhase.Negotiation:
                FireLine(king, ks, NegotiationOpenLines[ri]);
                break;
            case KingsCourtPhase.Payout:
                FireLine(king, ks, PayoutLines[ri]);
                break;
            case KingsCourtPhase.Dismissed:
                FireLine(king, ks, DismissalLines[ri]);
                break;
        }
    }

    // =========================================================================
    //  Reaction computation
    // =========================================================================

    private static KingReactionState ComputeReaction(in RunSummaryComponent rs)
    {
        float score = 0f;
        score += rs.LootGoldValue / 100f;       // 0+ for loot
        score += rs.PrincessHealth / 20f;        // 0–5 for health
        score += rs.PrincessGoodwill / 20f;      // 0–5 for goodwill
        score += rs.BossDefeated ? 3f : 0f;
        score += rs.PrincessDeliveredSafely ? 5f : -10f;
        score -= rs.PrincessDropCount * 2f;

        return score switch
        {
            >= 15f => KingReactionState.Pleased,
            >= 8f  => KingReactionState.Neutral,
            >= 3f  => KingReactionState.Dissatisfied,
            _      => KingReactionState.Furious,
        };
    }

    // =========================================================================
    //  Helpers
    // =========================================================================

    private void FireLine(
        Entity king, in KingStateComponent ks,
        string[] lines, int indexOffset = 0)
    {
        if (lines.Length == 0) return;
        int idx = (Random.Shared.Next(lines.Length) + indexOffset) % lines.Length;
        _dialogue.Add(new KingDialogueEvent(king, ks.Phase, ks.ReactionState, lines[idx]));
    }

    private void ClearRunSummaryComplete()
    {
        Entity summary = FindRunSummary();
        if (!World.IsAlive(summary)) return;
        ref var rs = ref World.GetComponent<RunSummaryComponent>(summary);
        rs.IsComplete = false;
    }

    private Entity FindKing()
    {
        foreach (var e in World.GetEntitiesWithTag("King"))
            return e;
        return Entity.Null;
    }

    private Entity FindRunSummary()
    {
        foreach (var e in World.GetEntitiesWithTag("RunSummary"))
            return e;
        return Entity.Null;
    }
}
