using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using REB.Engine.ECS;
using REB.Engine.Input;
using REB.Engine.Multiplayer.Components;
using REB.Engine.Physics;
using REB.Engine.Physics.Components;
using REB.Engine.Player.Components;
using REB.Engine.Rendering.Components;
using REB.Engine.World.Systems;

namespace REB.Engine.Multiplayer.Systems;

/// <summary>
/// Manages player join/leave for local co-op (Phase 2).
/// <para>
/// Each frame the system checks whether any unjoined gamepad presses Start or the
/// keyboard player presses Enter; on first press it creates a fully-equipped player entity.
/// </para>
/// <para>
/// Phase 3+: replace the join trigger with a network handshake; the entity-creation
/// logic remains the same.
/// </para>
/// </summary>
[RunAfter(typeof(InputSystem))]
public sealed class SessionManagerSystem : GameSystem
{
    private readonly bool[] _slotJoined = new bool[4];

    public override void Update(float deltaTime)
    {
        if (!World.TryGetSystem<InputSystem>(out var input)) return;

        // Keyboard player (slot 0) joins on Enter.
        if (!_slotJoined[0] && input.IsKeyPressed(Keys.Enter))
            JoinPlayer(0);

        // Gamepad players join on Start.
        for (int i = 0; i < 4; i++)
        {
            if (_slotJoined[i]) continue;
            var pad = (PlayerIndex)i;
            if (input.IsConnected(pad) && input.IsButtonPressed(pad, Buttons.Start))
                JoinPlayer(i);
        }
    }

    // =========================================================================
    //  Public API
    // =========================================================================

    /// <summary>
    /// Immediately creates a player entity for the given slot (0–3).
    /// Can be called directly during testing or for auto-join scenarios.
    /// </summary>
    public Entity JoinPlayer(int slot)
    {
        if (slot is < 0 or > 3)
            throw new ArgumentOutOfRangeException(nameof(slot), "Slot must be 0–3.");
        if (_slotJoined[slot])
            throw new InvalidOperationException($"Slot {slot} is already joined.");

        _slotJoined[slot] = true;
        bool isHost = !_slotJoined.Take(slot).Any(x => x);

        var entity = World.CreateEntity();

        // ── tags ─────────────────────────────────────────────────────────────
        World.AddTag(entity, "Player");
        World.AddTag(entity, $"Player{slot + 1}");

        // ── transform ────────────────────────────────────────────────────────
        World.AddComponent(entity, new TransformComponent
        {
            Position    = GetSpawnPosition(slot),
            Rotation    = Quaternion.Identity,
            Scale       = Vector3.One,
            WorldMatrix = Matrix.Identity,
        });

        // ── physics ──────────────────────────────────────────────────────────
        World.AddComponent(entity, ColliderComponent.Capsule(
            radius:     0.4f,
            halfHeight: 0.85f,
            layer:      CollisionLayer.Player,
            mask:       CollisionLayer.Terrain | CollisionLayer.Default));

        World.AddComponent(entity, new RigidBodyComponent
        {
            Velocity    = Vector3.Zero,
            Mass        = 75f,
            UseGravity  = true,
            LinearDrag  = 0.15f,
            IsKinematic = false,
        });

        // ── player systems ───────────────────────────────────────────────────
        bool useKeyboard = slot == 0;
        World.AddComponent(entity,
            useKeyboard
                ? PlayerInputComponent.Keyboard
                : PlayerInputComponent.Gamepad((PlayerIndex)slot));

        World.AddComponent(entity, CharacterControllerComponent.Default);
        World.AddComponent(entity, RoleComponent.None);
        World.AddComponent(entity, CarryComponent.Default);
        World.AddComponent(entity, AnimationComponent.Default);

        // ── session ──────────────────────────────────────────────────────────
        World.AddComponent(entity, PlayerSessionComponent.ForSlot(
            (byte)slot,
            isHost ? SessionRole.Host : SessionRole.Client));

        // ── network sync ─────────────────────────────────────────────────────
        World.AddComponent(entity, NetworkSyncComponent.ForPlayer((byte)slot));

        return entity;
    }

    /// <summary>True if the given slot already has a player entity.</summary>
    public bool IsSlotJoined(int slot) => _slotJoined[slot];

    // =========================================================================
    //  Internal
    // =========================================================================

    private Vector3 GetSpawnPosition(int slot)
    {
        // Use the entrance hall position from the floor generator if available.
        if (World.TryGetSystem<ProceduralFloorGeneratorSystem>(out var gen))
        {
            var entrances = World.GetEntitiesWithTag("Entrance").ToList();
            if (entrances.Count > 0 &&
                World.HasComponent<TransformComponent>(entrances[0]))
            {
                var pos = World.GetComponent<TransformComponent>(entrances[0]).Position;
                return pos + new Vector3(slot * 1.2f, 1f, 0f);
            }
        }

        // Fallback: stagger along the X axis.
        return new Vector3(slot * 1.5f, 1f, 0f);
    }
}
