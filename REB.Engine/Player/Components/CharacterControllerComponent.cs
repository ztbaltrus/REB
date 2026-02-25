using REB.Engine.ECS;

namespace REB.Engine.Player.Components;

/// <summary>
/// Drives character movement and camera for a player-controlled entity.
/// Consumed and mutated by <see cref="REB.Engine.Player.Systems.PlayerControllerSystem"/>.
/// </summary>
public struct CharacterControllerComponent : IComponent
{
    /// <summary>Base horizontal movement speed in world units per second.</summary>
    public float MoveSpeed;

    /// <summary>Multiplier applied to MoveSpeed while sprinting.</summary>
    public float RunMultiplier;

    /// <summary>Upward velocity impulse applied when jumping.</summary>
    public float JumpForce;

    /// <summary>Mouse / right-stick sensitivity in radians per input unit.</summary>
    public float LookSensitivity;

    /// <summary>Horizontal camera orbit angle (radians), updated each frame.</summary>
    public float CameraYaw;

    /// <summary>Vertical camera tilt angle (radians), clamped to ±~75°.</summary>
    public float CameraPitch;

    /// <summary>When true, camera orbits behind the player; false = first-person.</summary>
    public bool ThirdPersonView;

    /// <summary>Distance the camera sits behind the player in third-person mode.</summary>
    public float ThirdPersonDistance;

    /// <summary>Vertical height offset for the third-person camera orbit point.</summary>
    public float ThirdPersonHeight;

    /// <summary>Set by PhysicsSystem collision events; true when standing on solid ground.</summary>
    public bool IsGrounded;

    /// <summary>Current locomotion/action state, updated each frame.</summary>
    public PlayerState State;

    public static CharacterControllerComponent Default => new()
    {
        MoveSpeed           = 5f,
        RunMultiplier       = 1.8f,
        JumpForce           = 5f,
        LookSensitivity     = 0.002f,
        CameraYaw           = 0f,
        CameraPitch         = -0.3f,
        ThirdPersonView     = true,
        ThirdPersonDistance = 4f,
        ThirdPersonHeight   = 1.5f,
        IsGrounded          = false,
        State               = PlayerState.Idle,
    };
}
