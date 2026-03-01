using REB.Engine.ECS;

namespace REB.Engine.KingsCourt.Components;

/// <summary>
/// Tracks available and selected negotiation choices on the King entity.
/// The Negotiator player sets <see cref="SelectedChoice"/> from the UI;
/// <see cref="Systems.NegotiationMinigameSystem"/> applies the outcome and clears the selection.
/// </summary>
public struct DialogueChoiceComponent : IComponent
{
    // ── Availability flags (all unlocked by default; gated by tavern upgrades in Epic 8) ──

    public bool FlattersKingAvailable;
    public bool CitePrincessPlightAvailable;
    public bool BribeAdvisorAvailable;
    public bool GrovelAvailable;
    public bool ChallengeLedgerAvailable;

    /// <summary>The choice the Negotiator has selected this frame. None = no action.</summary>
    public NegotiationChoiceType SelectedChoice;

    /// <summary>True after NegotiationMinigameSystem has applied the choice this frame.</summary>
    public bool ChoiceProcessed;

    public static DialogueChoiceComponent Default => new()
    {
        FlattersKingAvailable        = true,
        CitePrincessPlightAvailable  = true,
        BribeAdvisorAvailable        = true,
        GrovelAvailable              = true,
        ChallengeLedgerAvailable     = true,
        SelectedChoice               = NegotiationChoiceType.None,
        ChoiceProcessed              = false,
    };
}
