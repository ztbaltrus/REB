namespace REB.Engine.Multiplayer;

/// <summary>High-level phase of a play session, driven by <c>LobbySystem</c>.</summary>
public enum SessionPhase
{
    /// <summary>Players are joining and selecting roles.</summary>
    Lobby,

    /// <summary>All players are ready; loading/countdown in progress.</summary>
    Loading,

    /// <summary>Active gameplay.</summary>
    InGame,

    /// <summary>Run complete (success or failure); results screen.</summary>
    EndOfRun,

    /// <summary>Game paused mid-run.</summary>
    Paused,
}
