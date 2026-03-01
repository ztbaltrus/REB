namespace REB.Engine.Tavern;

/// <summary>Fired by <see cref="Systems.UpgradeTreeSystem"/> when a purchase completes successfully.</summary>
public readonly record struct UpgradePurchasedEvent(UpgradeId Id, float GoldSpent);
