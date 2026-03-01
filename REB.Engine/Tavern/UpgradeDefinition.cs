namespace REB.Engine.Tavern;

/// <summary>
/// Immutable descriptor for a single Tavern upgrade.
/// The full catalog lives in <see cref="Components.UpgradeTreeComponent.Catalog"/>.
/// </summary>
public readonly record struct UpgradeDefinition(
    UpgradeId        Id,
    string           Name,
    UpgradeCategory  Category,
    float            Cost,
    UpgradeId        Prerequisite = UpgradeId.None);
