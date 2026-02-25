using REB.Engine.ECS;

namespace REB.Engine.World.Components;

/// <summary>
/// Data for a room entity within the generated floor layout.
/// Grid coordinates are in tile units; multiply by
/// <see cref="Systems.ProceduralFloorGeneratorSystem.TileSize"/> to get world units.
/// </summary>
public struct RoomComponent : IComponent
{
    /// <summary>Left-most tile column of the room on the floor grid.</summary>
    public int GridX;

    /// <summary>Top-most tile row of the room on the floor grid.</summary>
    public int GridY;

    /// <summary>Room width in tiles.</summary>
    public int Width;

    /// <summary>Room height in tiles.</summary>
    public int Height;

    /// <summary>Visual and gameplay theme applied to this room.</summary>
    public FloorTheme Theme;

    /// <summary>Functional purpose of this room.</summary>
    public RoomType Type;

    /// <summary>True after the player has entered this room at least once.</summary>
    public bool IsVisited;

    /// <summary>True after all enemies in this room have been defeated.</summary>
    public bool IsCleared;

    // -------------------------------------------------------------------------
    //  Derived
    // -------------------------------------------------------------------------

    /// <summary>Grid X coordinate of the room center tile.</summary>
    public readonly int CenterGridX => GridX + Width  / 2;

    /// <summary>Grid Y coordinate of the room center tile.</summary>
    public readonly int CenterGridY => GridY + Height / 2;
}
