using REB.Engine.ECS;
using REB.Engine.Loot;
using REB.Engine.Loot.Components;
using REB.Engine.Loot.Systems;
using REB.Engine.Player.Components;
using Xunit;

namespace REB.Tests.Loot;

// ---------------------------------------------------------------------------
//  UseItemSystem tests
//
//  UseItemPressed is set manually on PlayerInputComponent (no InputSystem needed).
//  PickupInteractionSystem is omitted; items are pre-owned via OwnerEntity.
// ---------------------------------------------------------------------------

public sealed class UseItemTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static (World world, UseItemSystem useItemSys) BuildWorld()
    {
        var world  = new World();
        var sys    = new UseItemSystem();
        world.RegisterSystem(sys);
        return (world, sys);
    }

    private static Entity AddPlayer(World world)
    {
        var e = world.CreateEntity();
        world.AddComponent(e, PlayerInputComponent.Keyboard);
        return e;
    }

    private static Entity AddOwnedItem(World world, Entity owner, ItemComponent ic)
    {
        ic.OwnerEntity = owner;
        var e = world.CreateEntity();
        world.AddComponent(e, ic);
        return e;
    }

    // -------------------------------------------------------------------------
    //  Active items
    // -------------------------------------------------------------------------

    [Fact]
    public void ActiveItem_UseAppliesCooldown()
    {
        var (world, _) = BuildWorld();
        var player = AddPlayer(world);
        var item   = AddOwnedItem(world, player, ItemComponent.ActiveTool(cooldown: 5f));

        ref var pinput = ref world.GetComponent<PlayerInputComponent>(player);
        pinput.UseItemPressed = true;

        world.Update(0.016f);

        var ic = world.GetComponent<ItemComponent>(item);
        Assert.Equal(5f, ic.CooldownRemaining);
        world.Dispose();
    }

    [Fact]
    public void ActiveItem_CooldownPreventsReuse()
    {
        var (world, useItemSys) = BuildWorld();
        var player = AddPlayer(world);
        var item   = AddOwnedItem(world, player, ItemComponent.ActiveTool(cooldown: 5f));

        // First use.
        ref var pinput = ref world.GetComponent<PlayerInputComponent>(player);
        pinput.UseItemPressed = true;
        world.Update(0.016f);

        // Second use attempt while still on cooldown.
        pinput.UseItemPressed = true;
        world.Update(0.016f);

        Assert.Empty(useItemSys.ItemUseEvents);
        world.Dispose();
    }

    [Fact]
    public void ActiveItem_CooldownTicksDown()
    {
        var (world, _) = BuildWorld();
        var player = AddPlayer(world);
        var item   = AddOwnedItem(world, player, ItemComponent.ActiveTool(cooldown: 1f));

        // Activate.
        ref var pinput = ref world.GetComponent<PlayerInputComponent>(player);
        pinput.UseItemPressed = true;
        world.Update(0.016f);

        // Tick half a second.
        world.Update(0.5f);

        var ic = world.GetComponent<ItemComponent>(item);
        Assert.True(ic.CooldownRemaining < 1f && ic.CooldownRemaining > 0f,
            $"CooldownRemaining should be between 0 and 1 but was {ic.CooldownRemaining}.");
        world.Dispose();
    }

    [Fact]
    public void ActiveItem_NotConsumed()
    {
        var (world, _) = BuildWorld();
        var player = AddPlayer(world);
        var item   = AddOwnedItem(world, player, ItemComponent.ActiveTool());

        ref var pinput = ref world.GetComponent<PlayerInputComponent>(player);
        pinput.UseItemPressed = true;

        world.Update(0.016f);

        Assert.True(world.IsAlive(item), "Active item should remain alive after use.");
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Consumable items
    // -------------------------------------------------------------------------

    [Fact]
    public void ConsumableItem_DestroyedAfterUse()
    {
        var (world, _) = BuildWorld();
        var player = AddPlayer(world);
        var item   = AddOwnedItem(world, player, ItemComponent.Consumable());

        ref var pinput = ref world.GetComponent<PlayerInputComponent>(player);
        pinput.UseItemPressed = true;

        world.Update(0.016f);

        Assert.False(world.IsAlive(item), "Consumable entity should be destroyed after use.");
        world.Dispose();
    }

    [Fact]
    public void ConsumableItem_FiresConsumedEvent()
    {
        var (world, useItemSys) = BuildWorld();
        var player = AddPlayer(world);
        AddOwnedItem(world, player, ItemComponent.Consumable());

        ref var pinput = ref world.GetComponent<PlayerInputComponent>(player);
        pinput.UseItemPressed = true;

        world.Update(0.016f);

        Assert.Single(useItemSys.ItemUseEvents);
        Assert.True(useItemSys.ItemUseEvents[0].WasConsumed);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Passive items
    // -------------------------------------------------------------------------

    [Fact]
    public void PassiveItem_NotActivated()
    {
        var (world, useItemSys) = BuildWorld();
        var player = AddPlayer(world);
        AddOwnedItem(world, player, ItemComponent.Coin);  // Passive

        ref var pinput = ref world.GetComponent<PlayerInputComponent>(player);
        pinput.UseItemPressed = true;

        world.Update(0.016f);

        Assert.Empty(useItemSys.ItemUseEvents);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Event lifecycle
    // -------------------------------------------------------------------------

    [Fact]
    public void ItemUseEvents_ClearedEachFrame()
    {
        var (world, useItemSys) = BuildWorld();
        var player = AddPlayer(world);
        AddOwnedItem(world, player, ItemComponent.Consumable());

        ref var pinput = ref world.GetComponent<PlayerInputComponent>(player);
        pinput.UseItemPressed = true;
        world.Update(0.016f);
        Assert.Single(useItemSys.ItemUseEvents);

        // Next frame — UseItemPressed is false, no events.
        pinput.UseItemPressed = false;
        world.Update(0.016f);
        Assert.Empty(useItemSys.ItemUseEvents);
        world.Dispose();
    }

    [Fact]
    public void NoItemOwned_NoEvent()
    {
        var (world, useItemSys) = BuildWorld();
        var player = AddPlayer(world);
        // No items given to the player.

        ref var pinput = ref world.GetComponent<PlayerInputComponent>(player);
        pinput.UseItemPressed = true;

        world.Update(0.016f);

        Assert.Empty(useItemSys.ItemUseEvents);
        world.Dispose();
    }

    [Fact]
    public void EventContainsCorrectItemType()
    {
        var (world, useItemSys) = BuildWorld();
        var player = AddPlayer(world);
        AddOwnedItem(world, player, ItemComponent.Consumable());

        ref var pinput = ref world.GetComponent<PlayerInputComponent>(player);
        pinput.UseItemPressed = true;
        world.Update(0.016f);

        Assert.Equal(ItemType.Consumable, useItemSys.ItemUseEvents[0].ItemType);
        world.Dispose();
    }
}
