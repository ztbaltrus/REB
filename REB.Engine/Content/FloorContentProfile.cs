using REB.Engine.Enemy;
using REB.Engine.Hazards;
using REB.Engine.World;

namespace REB.Engine.Content;

/// <summary>
/// Describes the enemy composition, loot density, and hazard loadout for a specific
/// <see cref="FloorTheme"/>. Registered in <see cref="ContentRegistry"/> and consumed by
/// <see cref="REB.Engine.RunManagement.Systems.RunManagerSystem"/> to configure each
/// procedurally-generated floor.
/// </summary>
public sealed class FloorContentProfile
{
    /// <summary>The theme this profile applies to.</summary>
    public FloorTheme Theme { get; init; }

    /// <summary>Base number of enemies placed per regular room.</summary>
    public int EnemiesPerRoom { get; init; }

    /// <summary>
    /// Multiplier applied to loot gold values on this theme relative to the base table.
    /// Values &gt; 1.0 make the floor more lucrative; &lt; 1.0 make it lean.
    /// </summary>
    public float LootMultiplier { get; init; }

    /// <summary>Base number of environmental hazards placed on the whole floor.</summary>
    public int BaseHazardCount { get; init; }

    /// <summary>
    /// Probability (0–1) that a boss arena room is present.
    /// Scaled up by run number inside RunManagerSystem.
    /// </summary>
    public float BossSpawnChance { get; init; }

    /// <summary>Baseline floor difficulty (1–10) for loot and enemy scaling on run 1.</summary>
    public int BaseFloorDifficulty { get; init; }

    /// <summary>Enemy archetypes that appear as regular enemies on this theme.</summary>
    public EnemyArchetype[] CommonEnemies { get; init; } = [];

    /// <summary>The archetype used for the boss encounter on this theme.</summary>
    public EnemyArchetype BossArchetype { get; init; }

    /// <summary>Hazard types placed preferentially on this theme.</summary>
    public HazardType[] PreferredHazards { get; init; } = [];
}
