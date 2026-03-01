using REB.Engine.ECS;
using REB.Engine.KingsCourt.Systems;
using REB.Engine.Tavern.Components;

namespace REB.Engine.Tavern.Systems;

/// <summary>
/// Maintains the crew's gold balance on the singleton entity tagged <c>"GoldLedger"</c>.
/// <list type="number">
///   <item>Each frame: adds gold from <see cref="PayoutCalculationSystem.PayoutEvents"/>.</item>
///   <item>Exposes <see cref="TrySpend"/> for other systems to deduct gold atomically.</item>
/// </list>
/// </summary>
[RunAfter(typeof(PayoutCalculationSystem))]
public sealed class GoldCurrencySystem : GameSystem
{
    // =========================================================================
    //  Public API
    // =========================================================================

    /// <summary>
    /// Attempts to deduct <paramref name="amount"/> gold from the balance.
    /// Returns <c>true</c> and deducts the amount if balance is sufficient;
    /// returns <c>false</c> and leaves the balance unchanged if not.
    /// </summary>
    public bool TrySpend(float amount)
    {
        Entity e = FindGoldLedger();
        if (!World.IsAlive(e)) return false;

        ref var gc = ref World.GetComponent<GoldCurrencyComponent>(e);
        if (gc.TotalGold < amount) return false;

        gc.TotalGold -= amount;
        return true;
    }

    /// <summary>Current gold balance (0 if no GoldLedger entity exists).</summary>
    public float TotalGold
    {
        get
        {
            Entity e = FindGoldLedger();
            return World.IsAlive(e)
                ? World.GetComponent<GoldCurrencyComponent>(e).TotalGold
                : 0f;
        }
    }

    // =========================================================================
    //  Update
    // =========================================================================

    public override void Update(float deltaTime)
    {
        if (!World.TryGetSystem<PayoutCalculationSystem>(out var payoutCalc)) return;
        if (payoutCalc!.PayoutEvents.Count == 0) return;

        Entity e = FindGoldLedger();
        if (!World.IsAlive(e)) return;

        ref var gc = ref World.GetComponent<GoldCurrencyComponent>(e);

        foreach (var evt in payoutCalc.PayoutEvents)
        {
            gc.TotalGold          += evt.FinalPayout;
            gc.LifetimeGoldEarned += evt.FinalPayout;
        }
    }

    // =========================================================================
    //  Helper
    // =========================================================================

    private Entity FindGoldLedger()
    {
        foreach (var e in World.GetEntitiesWithTag("GoldLedger"))
            return e;
        return Entity.Null;
    }
}
