namespace REB.Engine.World;

/// <summary>
/// Visual and gameplay theme applied to a generated tower floor.
/// Drives room template selection, enemy archetypes, loot tables,
/// and atmospheric lighting during procedural generation.
/// </summary>
public enum FloorTheme
{
    Dungeon,
    Crypt,
    TreasureVault,
    Garden,
    Library,
    Sewers,
}
