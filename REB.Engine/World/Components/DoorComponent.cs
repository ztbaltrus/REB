using REB.Engine.ECS;

namespace REB.Engine.World.Components;

/// <summary>Cardinal direction a door faces, relative to its host room.</summary>
public enum DoorDirection : byte
{
    North = 0,
    East  = 1,
    South = 2,
    West  = 3,
}

/// <summary>
/// Data for a door entity that connects two adjacent rooms or corridors.
/// </summary>
public struct DoorComponent : IComponent
{
    /// <summary>Entity index of the first connected room (the "source" side).</summary>
    public uint RoomAIndex;

    /// <summary>Entity index of the second connected room (the "destination" side).</summary>
    public uint RoomBIndex;

    /// <summary>Direction the door faces relative to Room A.</summary>
    public DoorDirection Direction;

    /// <summary>Grid X tile position of the door opening.</summary>
    public int GridX;

    /// <summary>Grid Y tile position of the door opening.</summary>
    public int GridY;

    /// <summary>Whether the door is currently open (passable).</summary>
    public bool IsOpen;

    /// <summary>Whether the door requires a key or event to open.</summary>
    public bool IsLocked;
}
