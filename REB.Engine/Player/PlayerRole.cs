namespace REB.Engine.Player;

/// <summary>Co-op role chosen in the lobby by each player.</summary>
public enum PlayerRole
{
    None,

    /// <summary>Physically carries the princess; fastest sprint while holding her.</summary>
    Carrier,

    /// <summary>Reveals nearby rooms and traps on the minimap.</summary>
    Scout,

    /// <summary>Senses loot through walls; bonus carry capacity for valuables.</summary>
    Treasurer,

    /// <summary>Reduces the princess's mood decay rate; can calm her during struggles.</summary>
    Negotiator,
}
