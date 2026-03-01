using REB.Engine.ECS;

namespace REB.Engine.Tavern.Components;

/// <summary>
/// Holds the crew's persistent gold balance.
/// Placed on the singleton entity tagged <c>"GoldLedger"</c>.
/// Gold is added by <see cref="Systems.GoldCurrencySystem"/> from end-of-run payouts
/// and deducted by <see cref="Systems.UpgradeTreeSystem"/> on upgrade purchases.
/// </summary>
public struct GoldCurrencyComponent : IComponent
{
    /// <summary>Current gold balance. Never drops below zero.</summary>
    public float TotalGold;

    /// <summary>Lifetime gold earned across all runs.</summary>
    public float LifetimeGoldEarned;

    /// <summary>Starting balance of 50 g so new players can afford one small upgrade immediately.</summary>
    public static GoldCurrencyComponent Default => new() { TotalGold = 50f, LifetimeGoldEarned = 0f };
}
