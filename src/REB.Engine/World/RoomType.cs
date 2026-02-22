namespace REB.Engine.World;

/// <summary>Functional role of a room within a generated floor.</summary>
public enum RoomType
{
    /// <summary>Entry room where players begin the floor.</summary>
    EntranceHall,

    /// <summary>Standard exploration room.</summary>
    Chamber,

    /// <summary>Contains a guaranteed chest or loot cache.</summary>
    TreasureRoom,

    /// <summary>Hidden room reached through a secret passage.</summary>
    SecretRoom,

    /// <summary>Large arena with strong enemies or a mini-boss.</summary>
    BossArena,

    /// <summary>Top-floor room where the princess is held.</summary>
    PrincessChamber,

    /// <summary>Room containing stairs down to the next floor or exit.</summary>
    ExitRoom,
}
