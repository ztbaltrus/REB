using REB.Engine.Combat.Components;
using REB.Engine.ECS;

namespace REB.Engine.Combat.Systems;

/// <summary>
/// Despawns entities whose <see cref="HealthComponent.IsDead"/> flag is set.
/// Players and the princess are excluded — their death is handled by game-flow systems.
/// <para>
/// Runs after <see cref="CombatSystem"/> and <see cref="HitReactionSystem"/> so that
/// the same-frame hit events are fully processed before the entity disappears.
/// </para>
/// </summary>
[RunAfter(typeof(CombatSystem))]
[RunAfter(typeof(HitReactionSystem))]
public sealed class DeathSystem : GameSystem
{
    /// <summary>Entities destroyed this frame. Captured before removal for downstream use.</summary>
    public IReadOnlyList<Entity> Killed => _killed;

    private readonly List<Entity> _killed = new();

    public override void Update(float deltaTime)
    {
        _killed.Clear();

        // Collect dead entities first; avoid mutating the pool while iterating.
        var dead = new List<Entity>();
        foreach (var entity in World.Query<HealthComponent>())
        {
            var hp = World.GetComponent<HealthComponent>(entity);
            if (!hp.IsDead) continue;

            // Preserve players and princess — game-flow logic handles their death.
            if (World.HasTag(entity, "Player") || World.HasTag(entity, "Princess")) continue;

            dead.Add(entity);
        }

        foreach (var entity in dead)
        {
            if (!World.IsAlive(entity)) continue;
            _killed.Add(entity);
            World.DestroyEntity(entity);
        }
    }
}
