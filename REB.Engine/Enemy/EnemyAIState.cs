namespace REB.Engine.Enemy;

/// <summary>
/// FSM states for the enemy behaviour tree.
/// Transitions are managed by <see cref="Systems.AggroSystem"/> (sight detection)
/// and <see cref="Systems.EnemyAISystem"/> (movement and attack execution).
/// </summary>
public enum EnemyAIState
{
    /// <summary>Standing still. Transitions to Patrol once IdleTimer elapses.</summary>
    Idle,

    /// <summary>Moving between four cardinal waypoints around the spawn position at walk speed.</summary>
    Patrol,

    /// <summary>Running toward ChaseTarget at full speed.</summary>
    Chase,

    /// <summary>Within attack range of ChaseTarget. Triggers DamageComponent each cooldown.</summary>
    Attack,
}
