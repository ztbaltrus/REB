namespace REB.Engine.RunManagement;

/// <summary>
/// High-level lifecycle phases of a single game-run cycle,
/// managed by <see cref="Systems.RunManagerSystem"/>.
/// </summary>
public enum RunPhase
{
    /// <summary>No run has been started yet; the world is in its initial state.</summary>
    Idle,

    /// <summary>
    /// Floor is being regenerated and run state is being reset. Lasts one update tick
    /// while systems rebuild their state.
    /// </summary>
    Loading,

    /// <summary>Players are inside the tower attempting to extract the princess and loot.</summary>
    InRun,

    /// <summary>The run has ended; the King's Court review scene is active.</summary>
    KingsCourt,

    /// <summary>The tavern is open between runs for upgrades and dialogue.</summary>
    Tavern,
}
