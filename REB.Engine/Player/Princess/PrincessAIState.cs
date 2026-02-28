namespace REB.Engine.Player.Princess;

/// <summary>
/// Current behaviour state of PrincessAISystem's finite-state machine.
/// Only meaningful while the princess is not being carried.
/// </summary>
public enum PrincessAIState
{
    /// <summary>Standing still. Default when health is good.</summary>
    Idle,

    /// <summary>Moving to a randomly chosen nearby position.</summary>
    Wandering,

    /// <summary>Furious: trying to reach the nearest Exit entity.</summary>
    SeekingExit,

    /// <summary>Being carried — AI logic fully suspended this frame.</summary>
    Carried,
}
