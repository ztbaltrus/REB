using REB.Engine.ECS;
using REB.Engine.KingsCourt;
using REB.Engine.KingsCourt.Components;
using REB.Engine.KingsCourt.Systems;
using Xunit;

namespace REB.Tests.KingsCourt;

// ---------------------------------------------------------------------------
//  NegotiationMinigameSystem tests
// ---------------------------------------------------------------------------

public sealed class NegotiationTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static (World world, NegotiationMinigameSystem negotiation) BuildWorld()
    {
        var world = new World();
        // KingsCourtSceneSystem is listed as a RunAfter dependency but is optional.
        var negotiation = new NegotiationMinigameSystem();
        world.RegisterSystem(negotiation);
        return (world, negotiation);
    }

    /// <summary>
    /// Creates a King entity with the minimal components needed by
    /// NegotiationMinigameSystem and sets the Phase to Negotiation.
    /// </summary>
    private static Entity AddKingInNegotiation(
        World world,
        KingReactionState reaction = KingReactionState.Neutral,
        NegotiationChoiceType choice = NegotiationChoiceType.None)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "King");

        var ks = KingStateComponent.Default;
        ks.Phase         = KingsCourtPhase.Negotiation;
        ks.ReactionState = reaction;
        world.AddComponent(e, ks);

        var dc = DialogueChoiceComponent.Default;
        dc.SelectedChoice = choice;
        world.AddComponent(e, dc);

        world.AddComponent(e, KingDispositionComponent.Default);
        return e;
    }

    private static Entity AddRunSummary(World world, bool delivered = true)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "RunSummary");
        world.AddComponent(e, new RunSummaryComponent
        {
            PrincessDeliveredSafely = delivered,
            IsComplete              = true,
        });
        return e;
    }

    private static float GetDisposition(World world, Entity king) =>
        world.GetComponent<KingDispositionComponent>(king).DispositionModifierPercent;

    // -------------------------------------------------------------------------
    //  No-op guards
    // -------------------------------------------------------------------------

    [Fact]
    public void Negotiation_DoesNothing_WhenPhaseIsNotNegotiation()
    {
        var (world, _) = BuildWorld();
        var e = world.CreateEntity();
        world.AddTag(e, "King");

        var ks = KingStateComponent.Default;
        ks.Phase = KingsCourtPhase.Review;      // wrong phase
        world.AddComponent(e, ks);
        world.AddComponent(e, DialogueChoiceComponent.Default);
        world.AddComponent(e, KingDispositionComponent.Default);

        world.Update(0.016f);

        Assert.Equal(0f, GetDisposition(world, e));
        world.Dispose();
    }

    [Fact]
    public void Negotiation_DoesNothing_WhenChoiceIsNone()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInNegotiation(world, choice: NegotiationChoiceType.None);

        world.Update(0.016f);

        Assert.Equal(0f, GetDisposition(world, king));
        world.Dispose();
    }

    [Fact]
    public void Negotiation_DoesNothing_WhenChoiceAlreadyProcessed()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInNegotiation(world, choice: NegotiationChoiceType.BribeAdvisor);

        ref var dc = ref world.GetComponent<DialogueChoiceComponent>(king);
        dc.ChoiceProcessed = true;

        world.Update(0.016f);

        Assert.Equal(0f, GetDisposition(world, king));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  FlattersKing
    // -------------------------------------------------------------------------

    [Fact]
    public void FlattersKing_GivesTenPercent_WhenKingIsNeutral()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInNegotiation(world,
            reaction: KingReactionState.Neutral,
            choice: NegotiationChoiceType.FlattersKing);

        world.Update(0.016f);

        Assert.Equal(10f, GetDisposition(world, king));
        world.Dispose();
    }

    [Fact]
    public void FlattersKing_GivesTenPercent_WhenKingIspleased()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInNegotiation(world,
            reaction: KingReactionState.Pleased,
            choice: NegotiationChoiceType.FlattersKing);

        world.Update(0.016f);

        Assert.Equal(10f, GetDisposition(world, king));
        world.Dispose();
    }

    [Fact]
    public void FlattersKing_GivesNegativeFivePercent_WhenKingIsFurious()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInNegotiation(world,
            reaction: KingReactionState.Furious,
            choice: NegotiationChoiceType.FlattersKing);

        world.Update(0.016f);

        Assert.Equal(-5f, GetDisposition(world, king));
        world.Dispose();
    }

    [Fact]
    public void FlattersKing_GivesNegativeFivePercent_WhenKingIsDissatisfied()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInNegotiation(world,
            reaction: KingReactionState.Dissatisfied,
            choice: NegotiationChoiceType.FlattersKing);

        world.Update(0.016f);

        Assert.Equal(-5f, GetDisposition(world, king));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  CitePrincessPlight
    // -------------------------------------------------------------------------

    [Fact]
    public void CitePrincessPlight_GivesFivePercent_WhenPrincessNotDelivered()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInNegotiation(world, choice: NegotiationChoiceType.CitePrincessPlight);
        AddRunSummary(world, delivered: false);

        world.Update(0.016f);

        Assert.Equal(5f, GetDisposition(world, king));
        world.Dispose();
    }

    [Fact]
    public void CitePrincessPlight_GivesTenPercent_WhenPrincessDelivered()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInNegotiation(world, choice: NegotiationChoiceType.CitePrincessPlight);
        AddRunSummary(world, delivered: true);

        world.Update(0.016f);

        Assert.Equal(10f, GetDisposition(world, king));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  BribeAdvisor
    // -------------------------------------------------------------------------

    [Fact]
    public void BribeAdvisor_GivesFifteenPercent_Always()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInNegotiation(world,
            reaction: KingReactionState.Furious,
            choice: NegotiationChoiceType.BribeAdvisor);

        world.Update(0.016f);

        Assert.Equal(15f, GetDisposition(world, king));
        world.Dispose();
    }

    [Fact]
    public void BribeAdvisor_SetsHasBribedAdvisor()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInNegotiation(world, choice: NegotiationChoiceType.BribeAdvisor);

        world.Update(0.016f);

        Assert.True(world.GetComponent<KingDispositionComponent>(king).HasBribedAdvisor);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Grovel
    // -------------------------------------------------------------------------

    [Fact]
    public void Grovel_GivesTwoPercent_WhenKingIspleased()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInNegotiation(world,
            reaction: KingReactionState.Pleased,
            choice: NegotiationChoiceType.Grovel);

        world.Update(0.016f);

        Assert.Equal(2f, GetDisposition(world, king));
        world.Dispose();
    }

    [Fact]
    public void Grovel_GivesFivePercent_WhenKingIsFurious()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInNegotiation(world,
            reaction: KingReactionState.Furious,
            choice: NegotiationChoiceType.Grovel);

        world.Update(0.016f);

        Assert.Equal(5f, GetDisposition(world, king));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  ChallengeLedger
    // -------------------------------------------------------------------------

    [Fact]
    public void ChallengeLedger_GivesTwentyPercent_WhenKingIspleased()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInNegotiation(world,
            reaction: KingReactionState.Pleased,
            choice: NegotiationChoiceType.ChallengeLedger);

        world.Update(0.016f);

        Assert.Equal(20f, GetDisposition(world, king));
        world.Dispose();
    }

    [Fact]
    public void ChallengeLedger_GivesNegativeTwentyPercent_WhenKingIsFurious()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInNegotiation(world,
            reaction: KingReactionState.Furious,
            choice: NegotiationChoiceType.ChallengeLedger);

        world.Update(0.016f);

        Assert.Equal(-20f, GetDisposition(world, king));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Disposition clamping
    // -------------------------------------------------------------------------

    [Fact]
    public void Disposition_ClampedAtPositiveThirty()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInNegotiation(world,
            reaction: KingReactionState.Pleased,
            choice: NegotiationChoiceType.ChallengeLedger);

        // Pre-seed disposition near cap.
        ref var d = ref world.GetComponent<KingDispositionComponent>(king);
        d.DispositionModifierPercent = 25f;

        world.Update(0.016f);  // +20 → would be 45, clamped to 30

        Assert.Equal(30f, GetDisposition(world, king));
        world.Dispose();
    }

    [Fact]
    public void Disposition_ClampedAtNegativeThirty()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInNegotiation(world,
            reaction: KingReactionState.Furious,
            choice: NegotiationChoiceType.ChallengeLedger);

        ref var d = ref world.GetComponent<KingDispositionComponent>(king);
        d.DispositionModifierPercent = -25f;

        world.Update(0.016f);  // −20 → would be −45, clamped to −30

        Assert.Equal(-30f, GetDisposition(world, king));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Choice is consumed (processed flag set, choice reset to None)
    // -------------------------------------------------------------------------

    [Fact]
    public void Choice_IsConsumed_AfterProcessing()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInNegotiation(world, choice: NegotiationChoiceType.BribeAdvisor);

        world.Update(0.016f);

        var dc = world.GetComponent<DialogueChoiceComponent>(king);
        Assert.True(dc.ChoiceProcessed);
        Assert.Equal(NegotiationChoiceType.None, dc.SelectedChoice);
        world.Dispose();
    }

    [Fact]
    public void Choice_DoesNotRepeat_OnSecondFrame()
    {
        var (world, _) = BuildWorld();
        var king = AddKingInNegotiation(world, choice: NegotiationChoiceType.BribeAdvisor);

        world.Update(0.016f);  // processes choice, disposition = 15
        world.Update(0.016f);  // choice already processed — no repeat

        Assert.Equal(15f, GetDisposition(world, king));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Dialogue events
    // -------------------------------------------------------------------------

    [Fact]
    public void DialogueEvent_Fired_WhenChoiceProcessed()
    {
        var (world, negotiation) = BuildWorld();
        AddKingInNegotiation(world, choice: NegotiationChoiceType.Grovel);

        world.Update(0.016f);

        Assert.Single(negotiation.DialogueEvents);
        world.Dispose();
    }

    [Fact]
    public void DialogueEvents_ClearedEachFrame()
    {
        var (world, negotiation) = BuildWorld();
        AddKingInNegotiation(world, choice: NegotiationChoiceType.Grovel);

        world.Update(0.016f);
        Assert.Single(negotiation.DialogueEvents);

        world.Update(0.016f);  // choice consumed — no new event
        Assert.Empty(negotiation.DialogueEvents);
        world.Dispose();
    }

    [Fact]
    public void DialogueEvent_HasNegotiationPhase()
    {
        var (world, negotiation) = BuildWorld();
        AddKingInNegotiation(world, choice: NegotiationChoiceType.FlattersKing);

        world.Update(0.016f);

        Assert.All(negotiation.DialogueEvents,
            e => Assert.Equal(KingsCourtPhase.Negotiation, e.Phase));
        world.Dispose();
    }
}
