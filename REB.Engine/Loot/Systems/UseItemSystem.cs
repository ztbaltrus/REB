using REB.Engine.ECS;
using REB.Engine.Loot.Components;
using REB.Engine.Player.Components;

namespace REB.Engine.Loot.Systems;

/// <summary>
/// Activates items when a player presses the UseItem button and ticks down
/// active-item cooldowns each frame.
/// <para>
/// Fires <see cref="ItemUseEvent"/> records for downstream systems; events are
/// cleared at the start of each update and available via <see cref="ItemUseEvents"/>.
/// </para>
/// </summary>
[RunAfter(typeof(PickupInteractionSystem))]
public sealed class UseItemSystem : GameSystem
{
    private readonly List<ItemUseEvent> _events = new();

    /// <summary>All item-use events that occurred this frame. Cleared at the start of each update.</summary>
    public IReadOnlyList<ItemUseEvent> ItemUseEvents => _events;

    public override void Update(float deltaTime)
    {
        _events.Clear();
        TickCooldowns(deltaTime);

        // Collect (player, item) pairs before processing to avoid mutating pools mid-enumeration.
        var toActivate = new List<(Entity Player, Entity Item)>();

        foreach (var player in World.Query<PlayerInputComponent>())
        {
            ref var pinput = ref World.GetComponent<PlayerInputComponent>(player);
            if (!pinput.UseItemPressed) continue;

            foreach (var item in World.Query<ItemComponent>())
            {
                var ic = World.GetComponent<ItemComponent>(item);
                if (ic.OwnerEntity       != player)            continue;
                if (ic.Usage             == ItemUsage.Passive) continue;
                if (ic.CooldownRemaining >  0f)                continue;

                toActivate.Add((player, item));
                break;  // one use per player per frame
            }
        }

        foreach (var (player, item) in toActivate)
        {
            if (!World.IsAlive(item)) continue;

            // Capture type before potential entity destruction.
            var      ic       = World.GetComponent<ItemComponent>(item);
            ItemType itemType = ic.Type;
            bool     consumed = ActivateItem(item, in ic, player);
            _events.Add(new ItemUseEvent(player, item, itemType, consumed));
        }
    }

    // =========================================================================
    //  Cooldowns
    // =========================================================================

    private void TickCooldowns(float deltaTime)
    {
        foreach (var item in World.Query<ItemComponent>())
        {
            ref var ic = ref World.GetComponent<ItemComponent>(item);
            if (ic.CooldownRemaining > 0f)
                ic.CooldownRemaining = MathF.Max(0f, ic.CooldownRemaining - deltaTime);
        }
    }

    // =========================================================================
    //  Activation
    // =========================================================================

    private bool ActivateItem(Entity item, in ItemComponent ic, Entity player)
    {
        switch (ic.Usage)
        {
            case ItemUsage.Consumable:
                ApplyConsumableEffect(in ic, player);
                World.DestroyEntity(item);
                return true;

            case ItemUsage.Active:
                ref var live = ref World.GetComponent<ItemComponent>(item);
                ApplyActiveEffect(in live, player);
                live.CooldownRemaining = live.MaxCooldown;
                return false;

            default:
                return false;
        }
    }

    // -------------------------------------------------------------------------
    //  Effect stubs — expanded in Story 4.4 / Epic 5+
    // -------------------------------------------------------------------------

    private void ApplyConsumableEffect(in ItemComponent ic, Entity player)
    {
        // Placeholder: health restores, mood boost, speed burst, etc.
        // Wired once the full item-effect system is implemented (Epic 5+).
    }

    private void ApplyActiveEffect(in ItemComponent ic, Entity player)
    {
        // Placeholder: torch lighting, lockpick, grapple hook, etc.
    }
}
