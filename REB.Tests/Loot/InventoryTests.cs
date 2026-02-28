using Microsoft.Xna.Framework;
using REB.Engine.ECS;
using REB.Engine.Loot.Components;
using REB.Engine.Loot.Systems;
using REB.Engine.Player.Components;
using REB.Engine.Rendering.Components;
using Xunit;

namespace REB.Tests.Loot;

// ---------------------------------------------------------------------------
//  InventorySystem + PickupInteractionSystem tests
//
//  InputSystem is not registered (avoids SDL3 at runtime).
//  PlayerInputComponent flags (InteractPressed, DropPressed) are set manually
//  so the pickup/drop paths can be exercised without real hardware input.
// ---------------------------------------------------------------------------

public sealed class InventoryTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static (World world, PickupInteractionSystem pickup, InventorySystem inventory)
        BuildWorld()
    {
        var world    = new World();
        var pickup   = new PickupInteractionSystem();
        var invSys   = new InventorySystem();
        world.RegisterSystem(pickup);
        world.RegisterSystem(invSys);
        return (world, pickup, invSys);
    }

    private static Entity AddPlayer(World world, Vector3 position)
    {
        var e = world.CreateEntity();
        world.AddComponent(e, new TransformComponent
        {
            Position    = position,
            Rotation    = Quaternion.Identity,
            Scale       = Vector3.One,
            WorldMatrix = Matrix.Identity,
        });
        world.AddComponent(e, PlayerInputComponent.Keyboard);
        world.AddComponent(e, InventoryComponent.Default);
        world.AddComponent(e, CharacterControllerComponent.Default);
        return e;
    }

    private static Entity AddItem(World world, Vector3 position, ItemComponent item)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "Item");
        world.AddComponent(e, new TransformComponent
        {
            Position    = position,
            Rotation    = Quaternion.Identity,
            Scale       = Vector3.One,
            WorldMatrix = Matrix.Identity,
        });
        world.AddComponent(e, item);
        return e;
    }

    // -------------------------------------------------------------------------
    //  Pick-up
    // -------------------------------------------------------------------------

    [Fact]
    public void Pickup_InRange_ClaimsItem()
    {
        var (world, _, _) = BuildWorld();
        var player = AddPlayer(world, Vector3.Zero);
        var item   = AddItem(world, new Vector3(0.5f, 0f, 0f), ItemComponent.Coin);

        ref var pinput = ref world.GetComponent<PlayerInputComponent>(player);
        pinput.InteractPressed = true;

        world.Update(0.016f);

        var ic = world.GetComponent<ItemComponent>(item);
        Assert.Equal(player, ic.OwnerEntity);
        world.Dispose();
    }

    [Fact]
    public void Pickup_OutOfRange_DoesNotClaim()
    {
        var (world, _, _) = BuildWorld();
        var player = AddPlayer(world, Vector3.Zero);
        var item   = AddItem(world, new Vector3(10f, 0f, 0f), ItemComponent.Coin);

        ref var pinput = ref world.GetComponent<PlayerInputComponent>(player);
        pinput.InteractPressed = true;

        world.Update(0.016f);

        var ic = world.GetComponent<ItemComponent>(item);
        Assert.False(world.IsAlive(ic.OwnerEntity));
        world.Dispose();
    }

    [Fact]
    public void Pickup_NoInteract_DoesNotClaim()
    {
        var (world, _, _) = BuildWorld();
        var player = AddPlayer(world, Vector3.Zero);
        var item   = AddItem(world, new Vector3(0.5f, 0f, 0f), ItemComponent.Coin);

        // InteractPressed left as false (default).
        world.Update(0.016f);

        var ic = world.GetComponent<ItemComponent>(item);
        Assert.False(world.IsAlive(ic.OwnerEntity));
        world.Dispose();
    }

    [Fact]
    public void Pickup_ExceedsWeightLimit_DoesNotClaim()
    {
        var (world, _, _) = BuildWorld();
        var player = AddPlayer(world, Vector3.Zero);

        // Pre-load the inventory to near its limit (simulates previous frame).
        ref var inv = ref world.GetComponent<InventoryComponent>(player);
        inv.CurrentWeight = 19f;  // MaxWeight = 20f

        // Artifact weighs 5 kg — would push total to 24 kg.
        var item = AddItem(world, new Vector3(0.5f, 0f, 0f), ItemComponent.Artifact);

        ref var pinput = ref world.GetComponent<PlayerInputComponent>(player);
        pinput.InteractPressed = true;

        world.Update(0.016f);

        var ic = world.GetComponent<ItemComponent>(item);
        Assert.False(world.IsAlive(ic.OwnerEntity));
        world.Dispose();
    }

    [Fact]
    public void Pickup_WithinWeightBudget_Succeeds()
    {
        var (world, _, _) = BuildWorld();
        var player = AddPlayer(world, Vector3.Zero);
        // Coin weighs 0.01 kg — well within the 20 kg default limit.
        var item = AddItem(world, new Vector3(0.5f, 0f, 0f), ItemComponent.Coin);

        ref var pinput = ref world.GetComponent<PlayerInputComponent>(player);
        pinput.InteractPressed = true;

        world.Update(0.016f);

        var ic = world.GetComponent<ItemComponent>(item);
        Assert.Equal(player, ic.OwnerEntity);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Drop
    // -------------------------------------------------------------------------

    [Fact]
    public void Drop_ReleasesOwnedItem()
    {
        var (world, _, _) = BuildWorld();
        var player = AddPlayer(world, Vector3.Zero);

        // Give the player a pre-owned coin.
        var ic   = ItemComponent.Coin;
        ic.OwnerEntity = player;
        var item = AddItem(world, Vector3.Zero, ic);

        ref var pinput = ref world.GetComponent<PlayerInputComponent>(player);
        pinput.DropPressed = true;

        world.Update(0.016f);

        var ic2 = world.GetComponent<ItemComponent>(item);
        Assert.False(world.IsAlive(ic2.OwnerEntity));
        world.Dispose();
    }

    [Fact]
    public void Drop_WhileCarryingPrincess_DoesNotDropItem()
    {
        var (world, _, _) = BuildWorld();
        var player = AddPlayer(world, Vector3.Zero);
        world.AddComponent(player, CarryComponent.Default);

        // Simulate princess being carried.
        ref var carry = ref world.GetComponent<CarryComponent>(player);
        carry.IsCarrying = true;

        // Give the player an owned item.
        var ic = ItemComponent.Coin;
        ic.OwnerEntity = player;
        var item = AddItem(world, Vector3.Zero, ic);

        ref var pinput = ref world.GetComponent<PlayerInputComponent>(player);
        pinput.DropPressed = true;

        world.Update(0.016f);

        var ic2 = world.GetComponent<ItemComponent>(item);
        Assert.Equal(player, ic2.OwnerEntity);  // item stays owned
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  InventorySystem weight tracking
    // -------------------------------------------------------------------------

    [Fact]
    public void InventorySystem_UpdatesCurrentWeight()
    {
        var (world, _, _) = BuildWorld();
        var player = AddPlayer(world, Vector3.Zero);

        // Two coins (0.01 kg each) owned by player.
        var ic1 = ItemComponent.Coin; ic1.OwnerEntity = player;
        var ic2 = ItemComponent.Coin; ic2.OwnerEntity = player;
        AddItem(world, Vector3.Zero, ic1);
        AddItem(world, Vector3.Zero, ic2);

        world.Update(0.016f);

        var inv = world.GetComponent<InventoryComponent>(player);
        Assert.Equal(2,      inv.ItemCount);
        Assert.Equal(0.02f,  inv.CurrentWeight, 4);
        world.Dispose();
    }

    [Fact]
    public void InventorySystem_IsOverweight_WhenExceedingLimit()
    {
        var (world, _, _) = BuildWorld();
        var player = AddPlayer(world, Vector3.Zero);

        // Artifact weighs 5 kg; add 5 artifacts → 25 kg > 20 kg limit.
        for (int i = 0; i < 5; i++)
        {
            var ic = ItemComponent.Artifact;
            ic.OwnerEntity = player;
            AddItem(world, Vector3.Zero, ic);
        }

        world.Update(0.016f);

        var inv = world.GetComponent<InventoryComponent>(player);
        Assert.True(inv.IsOverweight);
        world.Dispose();
    }

    [Fact]
    public void InventorySystem_NotOverweight_WhenUnderLimit()
    {
        var (world, _, _) = BuildWorld();
        var player = AddPlayer(world, Vector3.Zero);

        var ic = ItemComponent.Coin; ic.OwnerEntity = player;
        AddItem(world, Vector3.Zero, ic);

        world.Update(0.016f);

        var inv = world.GetComponent<InventoryComponent>(player);
        Assert.False(inv.IsOverweight);
        world.Dispose();
    }

    [Fact]
    public void InventorySystem_CountsOnlyOwnedItems()
    {
        var (world, _, _) = BuildWorld();
        var player = AddPlayer(world, Vector3.Zero);

        // One owned, one on the ground.
        var ownedIc = ItemComponent.Coin; ownedIc.OwnerEntity = player;
        AddItem(world, Vector3.Zero, ownedIc);
        AddItem(world, Vector3.Zero, ItemComponent.Coin);  // unowned

        world.Update(0.016f);

        var inv = world.GetComponent<InventoryComponent>(player);
        Assert.Equal(1, inv.ItemCount);
        world.Dispose();
    }
}
