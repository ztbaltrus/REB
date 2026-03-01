using REB.Engine.Combat.Components;
using REB.Engine.ECS;
using REB.Engine.Loot.Components;
using REB.Engine.Loot.Systems;
using REB.Engine.Player.Components;
using REB.Engine.Player.Princess.Components;
using REB.Engine.Player.Princess.Systems;
using REB.Engine.Player.Systems;
using REB.Engine.Tavern.Components;
using REB.Engine.Tavern.Systems;
using REB.Engine.UI.Components;

namespace REB.Engine.UI.Systems;

/// <summary>
/// Aggregates per-frame game state into a single <see cref="HUDDataComponent"/>
/// snapshot for the UI layer to consume.
/// <para>
/// Reads: Princess (HealthComponent, PrincessGoodwillComponent),
///        TreasureLedger (TreasureLedgerComponent),
///        first Player (RoleComponent, InventoryComponent),
///        GoldLedger (GoldCurrencyComponent).
/// Writes: HUDDataComponent on the entity tagged "HUDData" (upsert).
/// </para>
/// </summary>
[RunAfter(typeof(MoodSystem))]
[RunAfter(typeof(LootValuationSystem))]
[RunAfter(typeof(RoleAbilitySystem))]
[RunAfter(typeof(GoldCurrencySystem))]
public sealed class HUDSystem : GameSystem
{
    /// <summary>Last HUD snapshot computed this frame.</summary>
    public HUDDataComponent CurrentHUD { get; private set; }

    public override void Update(float deltaTime)
    {
        var hud = HUDDataComponent.Default;

        // ── Princess ─────────────────────────────────────────────────────────
        foreach (var e in World.GetEntitiesWithTag("Princess"))
        {
            if (World.HasComponent<HealthComponent>(e))
            {
                var hp = World.GetComponent<HealthComponent>(e);
                hud.PrincessHealth    = hp.CurrentHealth;
                hud.PrincessMaxHealth = hp.MaxHealth;
            }

            if (World.HasComponent<PrincessGoodwillComponent>(e))
                hud.PrincessGoodwill = World.GetComponent<PrincessGoodwillComponent>(e).Goodwill;

            break;  // only one princess
        }

        // ── Treasure ledger ───────────────────────────────────────────────────
        foreach (var e in World.GetEntitiesWithTag("TreasureLedger"))
        {
            if (World.HasComponent<TreasureLedgerComponent>(e))
                hud.TreasureValue = World.GetComponent<TreasureLedgerComponent>(e).TotalValue;
            break;
        }

        // ── First active player ───────────────────────────────────────────────
        foreach (var e in World.GetEntitiesWithTag("Player"))
        {
            if (World.HasComponent<RoleComponent>(e))
            {
                var role = World.GetComponent<RoleComponent>(e);
                hud.CarrierRole         = role.Role;
                hud.AbilityCooldownPct  = role.AbilityCooldownDuration > 0f
                    ? role.AbilityCooldownRemaining / role.AbilityCooldownDuration
                    : 0f;
            }

            if (World.HasComponent<InventoryComponent>(e))
                hud.IsOverweight = World.GetComponent<InventoryComponent>(e).IsOverweight;

            break;  // HUD tracks first player only
        }

        // ── Gold balance ──────────────────────────────────────────────────────
        foreach (var e in World.GetEntitiesWithTag("GoldLedger"))
        {
            if (World.HasComponent<GoldCurrencyComponent>(e))
                hud.GoldTotal = World.GetComponent<GoldCurrencyComponent>(e).TotalGold;
            break;
        }

        CurrentHUD = hud;

        // Upsert onto the singleton HUDData entity.
        foreach (var e in World.GetEntitiesWithTag("HUDData"))
        {
            World.SetComponent(e, hud);
            return;
        }
    }
}
