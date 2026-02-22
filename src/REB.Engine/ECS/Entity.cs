namespace REB.Engine.ECS;

/// <summary>
/// Lightweight handle to a game object. Entities are IDs only — all data lives in components.
/// An entity remains valid only while its <see cref="Version"/> matches the world's internal record.
/// </summary>
public readonly struct Entity : IEquatable<Entity>
{
    /// <summary>Slot index in the entity pool (1-based; 0 reserved for <see cref="Null"/>).</summary>
    public readonly uint Index;

    /// <summary>
    /// Increments each time this slot is reused, making stale handles detectable.
    /// </summary>
    public readonly uint Version;

    /// <summary>The null / invalid entity.</summary>
    public static readonly Entity Null = default;

    /// <summary>Returns false for the null entity.</summary>
    public bool IsValid => Index != 0;

    internal Entity(uint index, uint version)
    {
        Index = index;
        Version = version;
    }

    public bool Equals(Entity other) => Index == other.Index && Version == other.Version;
    public override bool Equals(object? obj) => obj is Entity e && Equals(e);
    public override int GetHashCode() => HashCode.Combine(Index, Version);
    public override string ToString() => $"Entity({Index}:v{Version})";

    public static bool operator ==(Entity a, Entity b) => a.Equals(b);
    public static bool operator !=(Entity a, Entity b) => !a.Equals(b);
}
