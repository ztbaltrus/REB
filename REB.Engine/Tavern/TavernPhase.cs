namespace REB.Engine.Tavern;

/// <summary>Lifecycle phases for the between-run Tavern scene.</summary>
public enum TavernPhase
{
    /// <summary>Tavern is not visible; waiting for end-of-run trigger.</summary>
    Inactive,

    /// <summary>Tavern is open; players can purchase upgrades and interact with the Tavernkeeper.</summary>
    Open,
}
