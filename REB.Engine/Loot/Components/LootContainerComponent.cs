using REB.Engine.ECS;

namespace REB.Engine.Loot.Components;

/// <summary>
/// Marks an entity as an openable loot container. A player pressing Interact nearby
/// sets IsOpened = true; LootSpawnSystem then spawns items on the next frame.
/// </summary>
public struct LootContainerComponent : IComponent
{
    /// <summary>Visual and audio style of this container.</summary>
    public LootContainerType ContainerType;

    /// <summary>RNG seed for item selection inside this container.</summary>
    public int Seed;

    /// <summary>Floor difficulty tier (1–10); higher values bias toward rarer loot.</summary>
    public int FloorDifficulty;

    /// <summary>True once a player has opened this container.</summary>
    public bool IsOpened;

    /// <summary>
    /// Number of items already spawned from this container.
    /// Zero while awaiting LootSpawnSystem; positive after spawn is complete.
    /// </summary>
    public int LootCount;

    public static LootContainerComponent Chest(int seed, int difficulty = 1) => new()
    {
        ContainerType   = LootContainerType.Chest,
        Seed            = seed,
        FloorDifficulty = difficulty,
    };

    public static LootContainerComponent Shrine(int seed, int difficulty = 1) => new()
    {
        ContainerType   = LootContainerType.Shrine,
        Seed            = seed,
        FloorDifficulty = difficulty,
    };

    public static LootContainerComponent Corpse(int seed, int difficulty = 1) => new()
    {
        ContainerType   = LootContainerType.Corpse,
        Seed            = seed,
        FloorDifficulty = difficulty,
    };
}
