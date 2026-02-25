using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using REB.Engine.ECS;
using REB.Engine.Input;
using REB.Engine.Physics;
using REB.Engine.Physics.Components;
using REB.Engine.Physics.Systems;
using REB.Engine.Player.Components;
using REB.Engine.Rendering.Components;

namespace REB.Engine.Player.Systems;

/// <summary>
/// Translates raw hardware input into character movement and camera positioning.
/// <para>Pipeline per frame:</para>
/// <list type="number">
///   <item>Collect grounded entities from last frame's <see cref="PhysicsSystem.CollisionEvents"/>.</item>
///   <item>Read move/look/action input for each player.</item>
///   <item>Apply horizontal velocity and jump impulse to <see cref="RigidBodyComponent"/>.</item>
///   <item>Compute <see cref="PlayerState"/> for animation/carry systems.</item>
///   <item>Reposition the player's camera entity.</item>
/// </list>
/// </summary>
[RunAfter(typeof(InputSystem))]
[RunAfter(typeof(PhysicsSystem))]
public sealed class PlayerControllerSystem : GameSystem
{
    private const float MaxPitch  =  1.3f;  // ~75° look up
    private const float MinPitch  = -1.3f;  // ~75° look down
    private const float EyeHeight =  1.7f;  // first-person eye height above entity origin

    public override void Update(float deltaTime)
    {
        // PhysicsSystem is required; InputSystem is optional (absent in headless tests).
        if (!World.TryGetSystem<PhysicsSystem>(out var physics)) return;
        World.TryGetSystem<InputSystem>(out var input);

        var grounded = CollectGroundedEntities(physics.CollisionEvents);

        // Always update grounded state regardless of input availability.
        foreach (var entity in World.Query<CharacterControllerComponent>())
        {
            ref var ctrl = ref World.GetComponent<CharacterControllerComponent>(entity);
            ctrl.IsGrounded = grounded.Contains(entity.Index);
        }

        // Input-dependent processing requires InputSystem.
        if (input == null) return;

        foreach (var entity in World.Query<CharacterControllerComponent,
                                          PlayerInputComponent,
                                          RigidBodyComponent,
                                          TransformComponent>())
        {
            ref var ctrl   = ref World.GetComponent<CharacterControllerComponent>(entity);
            ref var pinput = ref World.GetComponent<PlayerInputComponent>(entity);
            ref var rb     = ref World.GetComponent<RigidBodyComponent>(entity);
            ref var tf     = ref World.GetComponent<TransformComponent>(entity);

            // IsGrounded already set above; no need to repeat here.

            // ── gather input ──────────────────────────────────────────────────
            Vector2 moveDir   = ReadMoveInput(input, in pinput);
            Vector2 lookDelta = ReadLookInput(input, in pinput, deltaTime);
            bool    jump      = ReadJump(input, in pinput);
            bool    run       = ReadRun(input, in pinput);
            bool    interact  = ReadInteract(input, in pinput);
            bool    drop      = ReadDrop(input, in pinput);
            bool    camToggle = ReadCameraToggle(input, in pinput);

            // Cache per-frame flags for downstream systems.
            pinput.JumpPressed         = jump;
            pinput.RunHeld             = run;
            pinput.InteractPressed     = interact;
            pinput.DropPressed         = drop;
            pinput.CameraTogglePressed = camToggle;

            // ── look ──────────────────────────────────────────────────────────
            ctrl.CameraYaw  -= lookDelta.X;
            ctrl.CameraPitch = MathHelper.Clamp(
                ctrl.CameraPitch + lookDelta.Y, MinPitch, MaxPitch);

            if (camToggle) ctrl.ThirdPersonView = !ctrl.ThirdPersonView;

            // ── movement ──────────────────────────────────────────────────────
            float speed    = ctrl.MoveSpeed * (run ? ctrl.RunMultiplier : 1f);
            var   yawRot   = Quaternion.CreateFromAxisAngle(Vector3.Up, ctrl.CameraYaw);
            var   fwd      = Vector3.Transform(Vector3.Forward, yawRot);
            var   right    = Vector3.Transform(Vector3.Right,   yawRot);
            var   wishHoriz = (fwd * moveDir.Y + right * moveDir.X) * speed;

            // Preserve vertical velocity (gravity / jump); replace horizontal only.
            rb.Velocity = new Vector3(wishHoriz.X, rb.Velocity.Y, wishHoriz.Z);

            // ── jump ──────────────────────────────────────────────────────────
            if (jump && ctrl.IsGrounded)
                rb.Velocity = new Vector3(rb.Velocity.X, ctrl.JumpForce, rb.Velocity.Z);

            // ── state machine ─────────────────────────────────────────────────
            ctrl.State = ComputeState(in ctrl, in rb, moveDir, run);

            // ── camera ────────────────────────────────────────────────────────
            UpdateCamera(entity, in ctrl, in tf);
        }
    }

    // =========================================================================
    //  Input helpers
    // =========================================================================

    private static Vector2 ReadMoveInput(InputSystem input, in PlayerInputComponent p)
    {
        if (p.UseKeyboard)
        {
            float x = 0f, y = 0f;
            if (input.IsKeyDown(Keys.A) || input.IsKeyDown(Keys.Left))  x -= 1f;
            if (input.IsKeyDown(Keys.D) || input.IsKeyDown(Keys.Right)) x += 1f;
            if (input.IsKeyDown(Keys.W) || input.IsKeyDown(Keys.Up))    y += 1f;
            if (input.IsKeyDown(Keys.S) || input.IsKeyDown(Keys.Down))  y -= 1f;
            return new Vector2(x, y);
        }
        return input.LeftStick(p.GamepadSlot);
    }

    private static Vector2 ReadLookInput(InputSystem input, in PlayerInputComponent p, float dt)
    {
        const float StickScale = 2.5f;  // radians/second at full deflection
        if (p.UseKeyboard)
        {
            var delta = input.MouseDelta;
            return delta * (p.LookSensitivity * 0.002f);
        }
        var stick = input.RightStick(p.GamepadSlot);
        return new Vector2(stick.X, -stick.Y) * (StickScale * dt);
    }

    private static bool ReadJump(InputSystem input, in PlayerInputComponent p) =>
        p.UseKeyboard
            ? input.IsKeyPressed(Keys.Space)
            : input.IsButtonPressed(p.GamepadSlot, Buttons.A);

    private static bool ReadRun(InputSystem input, in PlayerInputComponent p) =>
        p.UseKeyboard
            ? input.IsKeyDown(Keys.LeftShift)
            : input.LeftTrigger(p.GamepadSlot) > 0.3f;

    private static bool ReadInteract(InputSystem input, in PlayerInputComponent p) =>
        p.UseKeyboard
            ? input.IsKeyPressed(Keys.E)
            : input.IsButtonPressed(p.GamepadSlot, Buttons.X);

    private static bool ReadDrop(InputSystem input, in PlayerInputComponent p) =>
        p.UseKeyboard
            ? input.IsKeyPressed(Keys.G)
            : input.IsButtonPressed(p.GamepadSlot, Buttons.B);

    private static bool ReadCameraToggle(InputSystem input, in PlayerInputComponent p) =>
        p.UseKeyboard
            ? input.IsKeyPressed(Keys.F)
            : input.IsButtonPressed(p.GamepadSlot, Buttons.Y);

    // =========================================================================
    //  State machine
    // =========================================================================

    private static PlayerState ComputeState(
        in CharacterControllerComponent ctrl,
        in RigidBodyComponent           rb,
        Vector2                         moveDir,
        bool                            run)
    {
        if (!ctrl.IsGrounded)
            return rb.Velocity.Y > 0.1f ? PlayerState.Jump : PlayerState.Fall;

        if (moveDir == Vector2.Zero) return PlayerState.Idle;
        return run ? PlayerState.Run : PlayerState.Walk;
    }

    // =========================================================================
    //  Camera
    // =========================================================================

    private void UpdateCamera(
        Entity                          playerEntity,
        in CharacterControllerComponent ctrl,
        in TransformComponent           playerTf)
    {
        // Each player slot has its own camera tag; slot 0 uses "MainCamera".
        string camTag = World.HasTag(playerEntity, "Player1") ? "MainCamera"
                      : World.HasTag(playerEntity, "Player2") ? "Camera2"
                      : World.HasTag(playerEntity, "Player3") ? "Camera3"
                      : "Camera4";

        Entity camEntity = Entity.Null;
        foreach (var e in World.GetEntitiesWithTag(camTag))
        {
            camEntity = e;
            break;
        }
        if (!World.IsAlive(camEntity)) return;

        ref var camTf  = ref World.GetComponent<TransformComponent>(camEntity);
        var     lookRot = Quaternion.CreateFromYawPitchRoll(ctrl.CameraYaw, ctrl.CameraPitch, 0f);

        if (ctrl.ThirdPersonView)
        {
            var backward = Vector3.Transform(Vector3.Backward, lookRot);
            camTf.Position = playerTf.Position
                           + Vector3.Up * ctrl.ThirdPersonHeight
                           + backward   * ctrl.ThirdPersonDistance;
            camTf.Rotation = lookRot;
        }
        else
        {
            camTf.Position = playerTf.Position + Vector3.Up * EyeHeight;
            camTf.Rotation = lookRot;
        }
    }

    // =========================================================================
    //  Grounded detection
    // =========================================================================

    private static HashSet<uint> CollectGroundedEntities(
        IReadOnlyList<CollisionEvent> events)
    {
        var set = new HashSet<uint>();
        foreach (var ev in events)
        {
            // An upward-facing normal means one body is resting on top of the other.
            if (ev.Normal.Y > 0.7f)
            {
                set.Add(ev.EntityA.Index);
                set.Add(ev.EntityB.Index);
            }
        }
        return set;
    }
}
