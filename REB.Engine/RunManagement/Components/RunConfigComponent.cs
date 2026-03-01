using REB.Engine.ECS;
using REB.Engine.World;

namespace REB.Engine.RunManagement.Components;

/// <summary>
/// Per-run configuration derived by <see cref="Systems.RunManagerSystem"/>.
/// Attach this to the singleton entity tagged <c>"RunConfig"</c>.
/// <para>
/// All seeds are deterministically derived from <see cref="MasterSeed"/> combined with
/// <see cref="RunNumber"/>, ensuring every run is uniquely procedural yet reproducible.
/// </para>
/// </summary>
public struct RunConfigComponent : IComponent
{
    /// <summary>1-based run counter. Increments at the start of each new run.</summary>
    public int RunNumber;

    /// <summary>
    /// Game-wide master seed. Combined with <see cref="RunNumber"/> to derive all
    /// per-run sub-seeds. Set from <c>Environment.TickCount</c> for random runs, or
    /// from a fixed value for deterministic testing.
    /// </summary>
    public int MasterSeed;

    /// <summary>
    /// Seed passed to <see cref="REB.Engine.World.Systems.ProceduralFloorGeneratorSystem"/>
    /// to generate the floor layout.
    /// </summary>
    public int FloorSeed;

    /// <summary>
    /// Seed passed to <see cref="REB.Engine.Loot.Systems.LootSpawnSystem"/> to
    /// populate the floor with items.
    /// </summary>
    public int LootSeed;

    /// <summary>
    /// Seed available to enemy placement and AI variance systems for this run.
    /// </summary>
    public int EnemySeed;

    /// <summary>Seed used for princess personality selection this run.</summary>
    public int PrincessSeed;

    /// <summary>Floor theme selected for this run.</summary>
    public FloorTheme Theme;

    /// <summary>
    /// Floor difficulty (1–10). Ramps every few runs. Drives loot quality and
    /// enemy stat boosts via the content profile.
    /// </summary>
    public int FloorDifficulty;

    public static RunConfigComponent Default => new()
    {
        RunNumber       = 0,
        MasterSeed      = 0,
        FloorSeed       = 0,
        LootSeed        = 0,
        EnemySeed       = 0,
        PrincessSeed    = 0,
        Theme           = FloorTheme.Dungeon,
        FloorDifficulty = 1,
    };
}
