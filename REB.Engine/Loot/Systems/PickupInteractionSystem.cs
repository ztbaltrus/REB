using Microsoft.Xna.Framework;
using REB.Engine.ECS;
using REB.Engine.Loot.Components;
using REB.Engine.Physics.Components;
using REB.Engine.Player.Components;
using REB.Engine.Player.Systems;
using REB.Engine.Rendering.Components;

namespace REB.Engine.Loot.Systems;

/// <summary>
/// Handles player interactions with loot items and containers.
/// <para>Pipeline per frame:</para>
/// <list type="number">
///   <item>Pick-up: InteractPressed + unclaimed item in range + weight budget → claim item.</item>
///   <item>Drop/throw: DropPressed (when not carrying the princess) → release nearest owned item
///         with a forward throw impulse.</item>
///   <item>Container open: InteractPressed near unopened container → flag for LootSpawnSystem.</item>
/// </list>
/// </summary>
[RunAfter(typeof(CarrySystem))]
public sealed class PickupInteractionSystem : GameSystem
{
    private const float PickupRange = 1.5f;
    private const float ThrowForce  = 6f;

    public override void Update(float deltaTime)
    {
        ProcessPickup();
        ProcessDrop();
        OpenContainers();
    }

    // =========================================================================
    //  Pick-up
    // =========================================================================

    private void ProcessPickup()
    {
        foreach (var player in
            World.Query<PlayerInputComponent, TransformComponent, InventoryComponent>())
        {
            ref var pinput = ref World.GetComponent<PlayerInputComponent>(player);
            if (!pinput.InteractPressed) continue;

            // While carrying the princess the interact button goes to CarrySystem.
            if (World.HasComponent<CarryComponent>(player))
            {
                var carry = World.GetComponent<CarryComponent>(player);
                if (carry.IsCarrying) continue;
            }

            var     tf  = World.GetComponent<TransformComponent>(player);
            ref var inv = ref World.GetComponent<InventoryComponent>(player);

            // Find the nearest unclaimed item within reach.
            Entity nearest       = Entity.Null;
            float  nearestDist   = float.MaxValue;
            float  nearestWeight = 0f;

            foreach (var item in World.Query<ItemComponent, TransformComponent>())
            {
                var ic = World.GetComponent<ItemComponent>(item);
                if (World.IsAlive(ic.OwnerEntity)) continue;  // already owned

                var   itf  = World.GetComponent<TransformComponent>(item);
                float dist = Vector3.Distance(tf.Position, itf.Position);
                if (dist < PickupRange && dist < nearestDist)
                {
                    nearest       = item;
                    nearestDist   = dist;
                    nearestWeight = ic.Weight;
                }
            }

            if (!World.IsAlive(nearest)) continue;

            // Reject if the item would push the carrier over the weight limit.
            if (inv.CurrentWeight + nearestWeight > inv.MaxWeight) continue;

            ref var ic2 = ref World.GetComponent<ItemComponent>(nearest);
            ic2.OwnerEntity = player;

            // Disable physics while the item is held.
            if (World.HasComponent<RigidBodyComponent>(nearest))
            {
                ref var rb = ref World.GetComponent<RigidBodyComponent>(nearest);
                rb.IsKinematic = true;
                rb.Velocity    = Vector3.Zero;
            }
        }
    }

    // =========================================================================
    //  Drop / throw
    // =========================================================================

    private void ProcessDrop()
    {
        foreach (var player in World.Query<PlayerInputComponent, TransformComponent>())
        {
            ref var pinput = ref World.GetComponent<PlayerInputComponent>(player);
            if (!pinput.DropPressed) continue;

            // If the player is carrying the princess, let CarrySystem handle the drop.
            if (World.HasComponent<CarryComponent>(player))
            {
                var carry = World.GetComponent<CarryComponent>(player);
                if (carry.IsCarrying) continue;
            }

            // Drop the first item owned by this player.
            foreach (var item in World.Query<ItemComponent, TransformComponent>())
            {
                ref var ic = ref World.GetComponent<ItemComponent>(item);
                if (ic.OwnerEntity != player) continue;

                DropItem(item, ref ic, player);
                break;  // one item per frame
            }
        }
    }

    private void DropItem(Entity item, ref ItemComponent ic, Entity player)
    {
        ic.OwnerEntity = Entity.Null;

        // Place the item at the player's feet.
        var     playerTf = World.GetComponent<TransformComponent>(player);
        ref var itemTf   = ref World.GetComponent<TransformComponent>(item);
        itemTf.Position  = playerTf.Position + new Vector3(0f, -0.3f, 0f);

        if (World.HasComponent<RigidBodyComponent>(item))
        {
            ref var rb = ref World.GetComponent<RigidBodyComponent>(item);
            rb.IsKinematic = false;
            rb.UseGravity  = true;

            // Apply a throw impulse in the player's facing direction.
            if (World.HasComponent<CharacterControllerComponent>(player))
            {
                var ctrl   = World.GetComponent<CharacterControllerComponent>(player);
                var yawRot = Quaternion.CreateFromAxisAngle(Vector3.Up, ctrl.CameraYaw);
                var fwd    = Vector3.Transform(Vector3.Forward, yawRot);
                rb.Velocity = fwd * ThrowForce;
            }
        }
    }

    // =========================================================================
    //  Container opening
    // =========================================================================

    private void OpenContainers()
    {
        foreach (var player in World.Query<PlayerInputComponent, TransformComponent>())
        {
            ref var pinput = ref World.GetComponent<PlayerInputComponent>(player);
            if (!pinput.InteractPressed) continue;

            var tf = World.GetComponent<TransformComponent>(player);

            foreach (var container in
                World.Query<LootContainerComponent, TransformComponent>())
            {
                ref var lc = ref World.GetComponent<LootContainerComponent>(container);
                if (lc.IsOpened) continue;

                var   ctf  = World.GetComponent<TransformComponent>(container);
                float dist = Vector3.Distance(tf.Position, ctf.Position);
                if (dist > PickupRange) continue;

                lc.IsOpened = true;
                break;  // open at most one container per player per frame
            }
        }
    }
}
