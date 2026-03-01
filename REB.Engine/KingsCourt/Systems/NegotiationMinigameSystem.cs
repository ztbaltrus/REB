using REB.Engine.ECS;
using REB.Engine.KingsCourt.Components;

namespace REB.Engine.KingsCourt.Systems;

/// <summary>
/// Processes Negotiator choices during the <see cref="KingsCourtPhase.Negotiation"/> window
/// and adjusts <see cref="KingDispositionComponent.DispositionModifierPercent"/> accordingly.
/// <para>Choice outcomes vary by the King's current <see cref="KingReactionState"/>:</para>
/// <list type="bullet">
///   <item><b>FlattersKing</b>: +10 % when Pleased/Neutral; −5 % when Dissatisfied/Furious.</item>
///   <item><b>CitePrincessPlight</b>: +5 % always; +5 % extra if princess was delivered.</item>
///   <item><b>BribeAdvisor</b>: +15 % always (gold cost enforced in Epic 8).</item>
///   <item><b>Grovel</b>: +2 % (Pleased) to +5 % (Furious) — more grovelling soothes a furious King.</item>
///   <item><b>ChallengeLedger</b>: +20 % (Pleased); +5 % (Neutral); −10 % (Dissatisfied); −20 % (Furious).</item>
/// </list>
/// </summary>
[RunAfter(typeof(KingsCourtSceneSystem))]
public sealed class NegotiationMinigameSystem : GameSystem
{
    /// <summary>Dialogue events fired this frame when the King responds to a negotiation move.</summary>
    public IReadOnlyList<KingDialogueEvent> DialogueEvents => _dialogue;

    private readonly List<KingDialogueEvent> _dialogue = new();

    public override void Update(float deltaTime)
    {
        _dialogue.Clear();

        Entity king = FindKing();
        if (!World.IsAlive(king)) return;

        var ks = World.GetComponent<KingStateComponent>(king);
        if (ks.Phase != KingsCourtPhase.Negotiation) return;

        ref var choice = ref World.GetComponent<DialogueChoiceComponent>(king);
        if (choice.SelectedChoice == NegotiationChoiceType.None || choice.ChoiceProcessed)
            return;

        ref var disposition = ref World.GetComponent<KingDispositionComponent>(king);

        float delta = ComputeDispositionDelta(choice.SelectedChoice, ks.ReactionState, king);
        disposition.DispositionModifierPercent = Math.Clamp(
            disposition.DispositionModifierPercent + delta, -30f, 30f);

        if (choice.SelectedChoice == NegotiationChoiceType.BribeAdvisor)
            disposition.HasBribedAdvisor = true;

        // King responds with dialogue.
        string lineKey = $"king.negotiation.response.{choice.SelectedChoice.ToString().ToLower()}";
        _dialogue.Add(new KingDialogueEvent(king, KingsCourtPhase.Negotiation, ks.ReactionState, lineKey));

        choice.ChoiceProcessed = true;
        choice.SelectedChoice  = NegotiationChoiceType.None;
    }

    // =========================================================================
    //  Disposition delta table
    // =========================================================================

    private float ComputeDispositionDelta(
        NegotiationChoiceType choice,
        KingReactionState reaction,
        Entity king)
    {
        bool kingIsAngry = reaction is KingReactionState.Dissatisfied or KingReactionState.Furious;

        return choice switch
        {
            NegotiationChoiceType.FlattersKing => kingIsAngry ? -5f : 10f,

            NegotiationChoiceType.CitePrincessPlight => ComputePrincessPlightDelta(king),

            NegotiationChoiceType.BribeAdvisor => 15f,

            NegotiationChoiceType.Grovel => reaction switch
            {
                KingReactionState.Pleased      => 2f,
                KingReactionState.Neutral      => 3f,
                KingReactionState.Dissatisfied => 4f,
                KingReactionState.Furious      => 5f,
                _                              => 3f,
            },

            NegotiationChoiceType.ChallengeLedger => reaction switch
            {
                KingReactionState.Pleased      =>  20f,
                KingReactionState.Neutral      =>   5f,
                KingReactionState.Dissatisfied => -10f,
                KingReactionState.Furious      => -20f,
                _                              =>   0f,
            },

            _ => 0f,
        };
    }

    private float ComputePrincessPlightDelta(Entity king)
    {
        // Base +5 %; +5 % extra if the princess was delivered safely.
        float delta = 5f;

        Entity summary = FindRunSummary();
        if (World.IsAlive(summary))
        {
            var rs = World.GetComponent<RunSummaryComponent>(summary);
            if (rs.PrincessDeliveredSafely) delta += 5f;
        }

        return delta;
    }

    // =========================================================================
    //  Helpers
    // =========================================================================

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
