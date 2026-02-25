using REB.Engine.ECS;
using REB.Engine.Player.Components;

namespace REB.Engine.Player.Systems;

/// <summary>
/// Handles role-specific ability cooldowns and activation.
/// Full ability implementations are delivered in Epic 4; stubs are provided here
/// so that the cooldown pipeline and component data are exercised from day one.
/// </summary>
[RunAfter(typeof(PlayerControllerSystem))]
public sealed class RoleAbilitySystem : GameSystem
{
    public override void Update(float deltaTime)
    {
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

    private void ActivateAbility(Entity entity, ref RoleComponent role)
    {
        switch (role.Role)
        {
            case PlayerRole.Scout:
                // TODO Epic 4: ping nearby rooms on the minimap.
                break;

            case PlayerRole.Treasurer:
                // TODO Epic 4: highlight loot entities through walls for 5 s.
                break;

            case PlayerRole.Negotiator:
                // TODO Epic 4: temporarily halve PrincessStateComponent.MoodDecayRate.
                break;

            case PlayerRole.Carrier:
                // TODO Epic 4: grant a carry-sprint burst without mood penalty.
                break;
        }

        role.AbilityReady             = false;
        role.AbilityCooldownRemaining = role.AbilityCooldownDuration;
    }
}
