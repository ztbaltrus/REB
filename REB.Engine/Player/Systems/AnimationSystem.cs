using REB.Engine.ECS;
using REB.Engine.Player.Components;

namespace REB.Engine.Player.Systems;

/// <summary>
/// Drives the <see cref="AnimationComponent"/> state machine based on each entity's
/// <see cref="PlayerState"/> and carry status.
/// Actual skeletal mesh blending is deferred to the rendering layer (Epic 4+).
/// </summary>
[RunAfter(typeof(PlayerControllerSystem))]
public sealed class AnimationSystem : GameSystem
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<AnimationComponent, CharacterControllerComponent>())
        {
            ref var anim = ref World.GetComponent<AnimationComponent>(entity);
            ref var ctrl = ref World.GetComponent<CharacterControllerComponent>(entity);

            bool isCarrying = World.HasComponent<CarryComponent>(entity)
                           && World.GetComponent<CarryComponent>(entity).IsCarrying;

            string desired = StateToClip(ctrl.State, isCarrying);

            if (desired != anim.CurrentClip)
            {
                anim.CurrentClip = desired;
                anim.ElapsedTime = 0f;
            }
            else
            {
                anim.ElapsedTime += deltaTime * anim.PlaybackSpeed;
                if (anim.IsLooping && anim.ClipDuration > 0f)
                    anim.ElapsedTime %= anim.ClipDuration;
            }
        }
    }

    private static string StateToClip(PlayerState state, bool isCarrying) => state switch
    {
        PlayerState.Idle     => isCarrying ? "CarryIdle" : "Idle",
        PlayerState.Walk     => isCarrying ? "CarryWalk" : "Walk",
        PlayerState.Run      => isCarrying ? "CarryWalk" : "Run",  // no carry-run animation
        PlayerState.Jump     => "Jump",
        PlayerState.Fall     => "Fall",
        PlayerState.Carry    => "CarryIdle",
        PlayerState.Interact => "Interact",
        _                    => "Idle",
    };
}
