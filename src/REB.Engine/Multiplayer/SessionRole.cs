namespace REB.Engine.Multiplayer;

/// <summary>Network authority role for a connected player slot.</summary>
public enum SessionRole
{
    /// <summary>First player to join; acts as session authority.</summary>
    Host,

    /// <summary>Subsequent joining players.</summary>
    Client,
}
