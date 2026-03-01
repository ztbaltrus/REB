using REB.Engine.ECS;
using REB.Engine.Tavern.Components;

namespace REB.Engine.Tavern.Systems;

/// <summary>
/// Processes upgrade-purchase requests against the Tavern upgrade tree.
/// <para>Per-request validation:</para>
/// <list type="number">
///   <item>Upgrade must exist in <see cref="UpgradeTreeComponent.Catalog"/>.</item>
///   <item>Prerequisite upgrade must already be owned (if any).</item>
///   <item>Upgrade must not already be purchased.</item>
///   <item><see cref="GoldCurrencySystem.TrySpend"/> must succeed.</item>
/// </list>
/// On success: marks the upgrade purchased and fires a <see cref="UpgradePurchasedEvent"/>.
/// </summary>
[RunAfter(typeof(GoldCurrencySystem))]
public sealed class UpgradeTreeSystem : GameSystem
{
    // ── Public events ─────────────────────────────────────────────────────────

    /// <summary>Purchase events fired this frame. Cleared at the start of each update.</summary>
    public IReadOnlyList<UpgradePurchasedEvent> PurchasedEvents => _events;

    private readonly List<UpgradePurchasedEvent> _events = new();
    private readonly Queue<UpgradeId>            _queue  = new();

    // =========================================================================
    //  Public API
    // =========================================================================

    /// <summary>Enqueues a purchase request to be processed on the next <see cref="Update"/>.</summary>
    public void RequestPurchase(UpgradeId id) => _queue.Enqueue(id);

    // =========================================================================
    //  Update
    // =========================================================================

    public override void Update(float deltaTime)
    {
        _events.Clear();

        if (_queue.Count == 0) return;

        Entity ledger = FindGoldLedger();
        if (!World.IsAlive(ledger)) return;

        if (!World.TryGetSystem<GoldCurrencySystem>(out var goldSystem)) return;

        while (_queue.TryDequeue(out var id))
            TryProcess(id, ledger, goldSystem!);
    }

    // =========================================================================
    //  Purchase processing
    // =========================================================================

    private void TryProcess(UpgradeId id, Entity ledger, GoldCurrencySystem goldSystem)
    {
        // Unknown upgrade.
        if (!UpgradeTreeComponent.Catalog.TryGetValue(id, out var def)) return;

        ref var tree = ref World.GetComponent<UpgradeTreeComponent>(ledger);

        // Already owned.
        if (tree.HasUpgrade(id)) return;

        // Prerequisite not met.
        if (def.Prerequisite != UpgradeId.None && !tree.HasUpgrade(def.Prerequisite)) return;

        // Insufficient gold.
        if (!goldSystem.TrySpend(def.Cost)) return;

        // Purchase succeeds.
        tree.AddUpgrade(id);
        _events.Add(new UpgradePurchasedEvent(id, def.Cost));
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
