using Microsoft.Xna.Framework;
using REB.Engine.ECS;
using REB.Engine.Loot.Components;
using REB.Engine.Player.Components;
using REB.Engine.Player.Princess.Components;
using REB.Engine.Rendering;
using REB.Engine.Rendering.Components;
using REB.Engine.World.Components;

namespace REB.Engine.Player.Systems;

/// <summary>
/// Handles role-specific ability cooldowns, activation, and effect timers.
/// <para>
/// <b>Scout</b> — highlights nearby rooms for 5 s (proximity ping via DebugDraw).<br/>
/// <b>Treasurer</b> — highlights all loot entities through walls for 5 s.<br/>
/// <b>Negotiator</b> — halves the princess's passive goodwill decay for 15 s.<br/>
/// <b>Carrier</b> — sprint burst: suppresses carry-struggle mood penalty for 8 s.
/// </para>
/// </summary>
[RunAfter(typeof(PlayerControllerSystem))]
public sealed class RoleAbilitySystem : GameSystem
{
    // =========================================================================
    //  Configuration
    // =========================================================================

    private const float ScoutHighlightDuration     = 5f;
    private const float TreasurerHighlightDuration  = 5f;
    private const float NegotiatorDuration          = 15f;
    private const float CarrierBurstDuration        = 8f;

    /// <summary>World-unit radius within which the Scout ability pings rooms.</summary>
    private const float ScoutRange = 20f;

    /// <summary>World units per tile (matches ProceduralFloorGeneratorSystem.TileSize).</summary>
    private const float TileSize = 2f;

    // =========================================================================
    //  Per-ability state
    // =========================================================================

    private Entity _negotiatorEntity   = Entity.Null;
    private float  _negotiatorTimer    = 0f;
    private Entity _carrierBurstEntity = Entity.Null;
    private float  _carrierBurstTimer  = 0f;

    // =========================================================================
    //  Update
    // =========================================================================

    public override void Update(float deltaTime)
    {
        TickNegotiatorTimer(deltaTime);
        TickCarrierBurstTimer(deltaTime);
        TickRoomHighlights(deltaTime);
        TickLootHighlights(deltaTime);

        foreach (var entity in World.Query<RoleComponent, PlayerInputComponent>())
        {
            ref var role   = ref World.GetComponent<RoleComponent>(entity);
            ref var pinput = ref World.GetComponent<PlayerInputComponent>(entity);

            // Tick cooldown.
            if (role.AbilityCooldownRemaining > 0f)
            {
                role.AbilityCooldownRemaining -= deltaTime;
                if (role.AbilityCooldownRemaining <= 0f)
                {
                    role.AbilityCooldownRemaining = 0f;
                    role.AbilityReady             = true;
                }
            }
            else if (!role.AbilityReady)
            {
                role.AbilityReady = true;
            }

            if (!role.AbilityReady || !pinput.InteractPressed) continue;

            ActivateAbility(entity, ref role);
        }
    }

    // =========================================================================
    //  Draw — DebugDraw highlights (3D world-space)
    // =========================================================================

    public override void Draw(GameTime gameTime)
    {
        // Scout: cyan box outline around each pinged room.
        foreach (var e in World.Query<RoomHighlightComponent, RoomComponent>())
        {
            var hl = World.GetComponent<RoomHighlightComponent>(e);
            if (hl.TimeRemaining <= 0f) continue;

            ref var room   = ref World.GetComponent<RoomComponent>(e);
            float   halfW  = room.Width  * TileSize * 0.5f;
            float   halfH  = room.Height * TileSize * 0.5f;
            var     center = new Vector3(room.CenterGridX * TileSize, 1f, room.CenterGridY * TileSize);
            var     box    = new BoundingBox(
                center - new Vector3(halfW, 1.2f, halfH),
                center + new Vector3(halfW, 1.2f, halfH));

            float alpha = Math.Clamp(hl.TimeRemaining / ScoutHighlightDuration, 0f, 1f);
            DebugDraw.DrawBox(box, Color.Cyan * alpha);
        }

        // Treasurer: yellow sphere marker above each highlighted loot item.
        foreach (var e in World.Query<LootHighlightComponent>())
        {
            var hl = World.GetComponent<LootHighlightComponent>(e);
            if (hl.TimeRemaining <= 0f) continue;
            if (!World.HasComponent<TransformComponent>(e)) continue;

            var   pos   = World.GetComponent<TransformComponent>(e).Position;
            float alpha = Math.Clamp(hl.TimeRemaining / TreasurerHighlightDuration, 0f, 1f);
            DebugDraw.DrawSphere(pos + Vector3.Up, 0.4f, Color.Yellow * alpha);
        }
    }

    // =========================================================================
    //  Ability dispatch
    // =========================================================================

    private void ActivateAbility(Entity entity, ref RoleComponent role)
    {
        switch (role.Role)
        {
            case PlayerRole.Scout:      ActivateScout(entity);      break;
            case PlayerRole.Treasurer:  ActivateTreasurer();        break;
            case PlayerRole.Negotiator: ActivateNegotiator(entity); break;
            case PlayerRole.Carrier:    ActivateCarrier(entity);    break;
        }

        role.AbilityReady             = false;
        role.AbilityCooldownRemaining = role.AbilityCooldownDuration;
    }

    // =========================================================================
    //  Ability implementations
    // =========================================================================

    /// <summary>Highlights rooms within ScoutRange of the activating player for 5 seconds.</summary>
    private void ActivateScout(Entity player)
    {
        if (!World.HasComponent<TransformComponent>(player)) return;
        var playerPos = World.GetComponent<TransformComponent>(player).Position;

        foreach (var roomEntity in World.Query<RoomComponent>())
        {
            ref var room   = ref World.GetComponent<RoomComponent>(roomEntity);
            var     center = new Vector3(room.CenterGridX * TileSize, 0f, room.CenterGridY * TileSize);

            if (Vector3.Distance(playerPos, center) > ScoutRange) continue;

            if (World.HasComponent<RoomHighlightComponent>(roomEntity))
            {
                ref var hl = ref World.GetComponent<RoomHighlightComponent>(roomEntity);
                hl.TimeRemaining = ScoutHighlightDuration;
            }
            else
            {
                World.AddComponent(roomEntity,
                    new RoomHighlightComponent { TimeRemaining = ScoutHighlightDuration });
            }
        }
    }

    /// <summary>Highlights all loot items for 5 seconds so they're visible through walls.</summary>
    private void ActivateTreasurer()
    {
        foreach (var itemEntity in World.GetEntitiesWithTag("Item"))
        {
            if (World.HasComponent<LootHighlightComponent>(itemEntity))
            {
                ref var hl = ref World.GetComponent<LootHighlightComponent>(itemEntity);
                hl.TimeRemaining = TreasurerHighlightDuration;
            }
            else
            {
                World.AddComponent(itemEntity,
                    new LootHighlightComponent { TimeRemaining = TreasurerHighlightDuration });
            }
        }
    }

    /// <summary>Halves the princess's passive goodwill decay for 15 seconds.</summary>
    private void ActivateNegotiator(Entity player)
    {
        _negotiatorEntity = player;
        _negotiatorTimer  = NegotiatorDuration;

        foreach (var e in World.GetEntitiesWithTag("Princess"))
        {
            if (World.HasComponent<PrincessGoodwillComponent>(e))
            {
                ref var gw = ref World.GetComponent<PrincessGoodwillComponent>(e);
                gw.GoodwillDecayMultiplier = 0.5f;
            }
            break;
        }
    }

    /// <summary>Sprint burst: suppresses carry-struggle mood penalty for 8 seconds.</summary>
    private void ActivateCarrier(Entity player)
    {
        _carrierBurstEntity = player;
        _carrierBurstTimer  = CarrierBurstDuration;

        if (World.HasComponent<CarryComponent>(player))
        {
            ref var carry = ref World.GetComponent<CarryComponent>(player);
            carry.SprintBurstActive = true;
        }
    }

    // =========================================================================
    //  Timer ticks
    // =========================================================================

    private void TickNegotiatorTimer(float dt)
    {
        if (_negotiatorTimer <= 0f) return;
        _negotiatorTimer -= dt;
        if (_negotiatorTimer > 0f) return;

        _negotiatorTimer = 0f;
        if (World.IsAlive(_negotiatorEntity))
        {
            foreach (var e in World.GetEntitiesWithTag("Princess"))
            {
                if (World.HasComponent<PrincessGoodwillComponent>(e))
                {
                    ref var gw = ref World.GetComponent<PrincessGoodwillComponent>(e);
                    gw.GoodwillDecayMultiplier = 1f;
                }
                break;
            }
        }
        _negotiatorEntity = Entity.Null;
    }

    private void TickCarrierBurstTimer(float dt)
    {
        if (_carrierBurstTimer <= 0f) return;
        _carrierBurstTimer -= dt;
        if (_carrierBurstTimer > 0f) return;

        _carrierBurstTimer = 0f;
        if (World.IsAlive(_carrierBurstEntity) &&
            World.HasComponent<CarryComponent>(_carrierBurstEntity))
        {
            ref var carry = ref World.GetComponent<CarryComponent>(_carrierBurstEntity);
            carry.SprintBurstActive = false;
        }
        _carrierBurstEntity = Entity.Null;
    }

    private void TickRoomHighlights(float dt)
    {
        foreach (var e in World.Query<RoomHighlightComponent>())
        {
            ref var hl = ref World.GetComponent<RoomHighlightComponent>(e);
            if (hl.TimeRemaining > 0f) hl.TimeRemaining -= dt;
        }
    }

    private void TickLootHighlights(float dt)
    {
        foreach (var e in World.Query<LootHighlightComponent>())
        {
            ref var hl = ref World.GetComponent<LootHighlightComponent>(e);
            if (hl.TimeRemaining > 0f) hl.TimeRemaining -= dt;
        }
    }
}
