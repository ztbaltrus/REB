namespace REB.Engine.KingsCourt;

/// <summary>
/// A choice the Negotiator can make during the <see cref="KingsCourtPhase.Negotiation"/> window.
/// Applied by <see cref="Systems.NegotiationMinigameSystem"/> to adjust
/// <see cref="Components.KingDispositionComponent.DispositionModifierPercent"/>.
/// </summary>
public enum NegotiationChoiceType
{
    /// <summary>No choice selected this frame.</summary>
    None,

    /// <summary>
    /// Compliment the King's wisdom and taste.
    /// +10 % when Pleased/Neutral; −5 % when Dissatisfied/Furious (he sees through it).
    /// </summary>
    FlattersKing,

    /// <summary>
    /// Appeal to sympathy for the princess's ordeal.
    /// +5 % always; +5 % additional if she was delivered safely.
    /// </summary>
    CitePrincessPlight,

    /// <summary>
    /// Slip a coin to the King's advisor to adjust the final tally.
    /// +15 % regardless of reaction. Costs gold (enforced by GoldCurrencySystem in Epic 8).
    /// </summary>
    BribeAdvisor,

    /// <summary>
    /// Prostrate yourself entirely.
    /// +2–5 % depending on how angry the King is (more grovelling needed for furious kings).
    /// </summary>
    Grovel,

    /// <summary>
    /// Contest the accounting directly.
    /// +20 % if Pleased; −10 % if Dissatisfied; −20 % if Furious.
    /// </summary>
    ChallengeLedger,
}
