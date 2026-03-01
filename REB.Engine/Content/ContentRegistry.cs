using REB.Engine.Enemy;
using REB.Engine.Hazards;
using REB.Engine.World;

namespace REB.Engine.Content;

/// <summary>
/// Static registry that maps each <see cref="FloorTheme"/> to its
/// <see cref="FloorContentProfile"/>.
/// <para>
/// Consumed by <see cref="REB.Engine.RunManagement.Systems.RunManagerSystem"/> when
/// configuring procedurally-generated floors. All six base themes are pre-registered;
/// add new entries here for modded or DLC content.
/// </para>
/// </summary>
public static class ContentRegistry
{
    private static readonly IReadOnlyDictionary<FloorTheme, FloorContentProfile> _profiles =
        new Dictionary<FloorTheme, FloorContentProfile>
        {
            [FloorTheme.Dungeon] = new FloorContentProfile
            {
                Theme               = FloorTheme.Dungeon,
                EnemiesPerRoom      = 2,
                LootMultiplier      = 1.0f,
                BaseHazardCount     = 3,
                BossSpawnChance     = 0.25f,
                BaseFloorDifficulty = 1,
                CommonEnemies       = [EnemyArchetype.Guard, EnemyArchetype.Archer],
                BossArchetype       = EnemyArchetype.Brute,
                PreferredHazards    = [HazardType.SpikeTrap, HazardType.Pit],
            },
            [FloorTheme.Crypt] = new FloorContentProfile
            {
                Theme               = FloorTheme.Crypt,
                EnemiesPerRoom      = 3,
                LootMultiplier      = 1.2f,
                BaseHazardCount     = 4,
                BossSpawnChance     = 0.35f,
                BaseFloorDifficulty = 2,
                CommonEnemies       = [EnemyArchetype.Guard, EnemyArchetype.Brute],
                BossArchetype       = EnemyArchetype.Brute,
                PreferredHazards    = [HazardType.SpikeTrap, HazardType.SwingingBlade],
            },
            [FloorTheme.TreasureVault] = new FloorContentProfile
            {
                Theme               = FloorTheme.TreasureVault,
                EnemiesPerRoom      = 2,
                LootMultiplier      = 2.0f,
                BaseHazardCount     = 5,
                BossSpawnChance     = 0.40f,
                BaseFloorDifficulty = 3,
                CommonEnemies       = [EnemyArchetype.Archer, EnemyArchetype.Guard],
                BossArchetype       = EnemyArchetype.Brute,
                PreferredHazards    = [HazardType.Pit, HazardType.SwingingBlade],
            },
            [FloorTheme.Garden] = new FloorContentProfile
            {
                Theme               = FloorTheme.Garden,
                EnemiesPerRoom      = 2,
                LootMultiplier      = 0.9f,
                BaseHazardCount     = 2,
                BossSpawnChance     = 0.20f,
                BaseFloorDifficulty = 1,
                CommonEnemies       = [EnemyArchetype.Archer],
                BossArchetype       = EnemyArchetype.Guard,
                PreferredHazards    = [HazardType.Pit],
            },
            [FloorTheme.Library] = new FloorContentProfile
            {
                Theme               = FloorTheme.Library,
                EnemiesPerRoom      = 2,
                LootMultiplier      = 1.5f,
                BaseHazardCount     = 3,
                BossSpawnChance     = 0.30f,
                BaseFloorDifficulty = 2,
                CommonEnemies       = [EnemyArchetype.Archer, EnemyArchetype.Guard],
                BossArchetype       = EnemyArchetype.Archer,
                PreferredHazards    = [HazardType.SwingingBlade, HazardType.SpikeTrap],
            },
            [FloorTheme.Sewers] = new FloorContentProfile
            {
                Theme               = FloorTheme.Sewers,
                EnemiesPerRoom      = 3,
                LootMultiplier      = 0.8f,
                BaseHazardCount     = 4,
                BossSpawnChance     = 0.30f,
                BaseFloorDifficulty = 2,
                CommonEnemies       = [EnemyArchetype.Guard, EnemyArchetype.Brute],
                BossArchetype       = EnemyArchetype.Brute,
                PreferredHazards    = [HazardType.Pit, HazardType.SpikeTrap],
            },
        };

    // =========================================================================
    //  Public API
    // =========================================================================

    /// <summary>
    /// Returns the content profile for the given theme, or the Dungeon fallback
    /// if no profile is registered.
    /// </summary>
    public static FloorContentProfile GetProfile(FloorTheme theme) =>
        _profiles.TryGetValue(theme, out var p) ? p : _profiles[FloorTheme.Dungeon];

    /// <summary>All registered content profiles (one per theme).</summary>
    public static IEnumerable<FloorContentProfile> AllProfiles => _profiles.Values;

    /// <summary>All themes with a registered content profile.</summary>
    public static IEnumerable<FloorTheme> AllThemes => _profiles.Keys;

    /// <summary>Total number of registered content profiles.</summary>
    public static int ProfileCount => _profiles.Count;
}
