using Microsoft.Xna.Framework;
using REB.Engine.ECS;

namespace REB.Engine.Enemy.Components;

/// <summary>
/// All per-entity state required to drive the enemy behaviour tree.
/// Updated by <see cref="Systems.AggroSystem"/> (sight/leash) and
/// <see cref="Systems.EnemyAISystem"/> (movement, attack).
/// </summary>
public struct EnemyAIComponent : IComponent
{
    // ── Identity ───────────────────────────────────────────────────────────────

    /// <summary>Template that defines this enemy's stat profile.</summary>
    public EnemyArchetype Archetype;

    // ── State machine ──────────────────────────────────────────────────────────

    /// <summary>Current behaviour state.</summary>
    public EnemyAIState State;

    /// <summary>Seconds remaining before transitioning out of Idle.</summary>
    public float IdleTimer;

    // ── Aggro ──────────────────────────────────────────────────────────────────

    /// <summary>Detection radius (world units). Targets within this range trigger Chase.</summary>
    public float SightRange;

    /// <summary>If the active target exceeds this distance the enemy returns to Patrol.</summary>
    public float LeashRange;

    /// <summary>The entity currently being chased / attacked. <see cref="Entity.Null"/> if none.</summary>
    public Entity ChaseTarget;

    // ── Movement ───────────────────────────────────────────────────────────────

    /// <summary>Speed (units/s) while patrolling.</summary>
    public float WalkSpeed;

    /// <summary>Speed (units/s) while chasing.</summary>
    public float RunSpeed;

    /// <summary>World-space spawn position — used as the patrol waypoint centre.</summary>
    public Vector3 SpawnPosition;

    /// <summary>Current patrol waypoint index (0–3, cycling through cardinal offsets).</summary>
    public int PatrolIndex;

    // ── Attack ─────────────────────────────────────────────────────────────────

    /// <summary>Distance at which the enemy transitions from Chase to Attack.</summary>
    public float AttackRange;

    // ── Archetype factories ────────────────────────────────────────────────────

    public static EnemyAIComponent Guard(Vector3 spawnPos) => new()
    {
        Archetype     = EnemyArchetype.Guard,
        State         = EnemyAIState.Idle,
        IdleTimer     = 2f,
        SightRange    = 8f,
        LeashRange    = 14f,
        ChaseTarget   = Entity.Null,
        WalkSpeed     = 2f,
        RunSpeed      = 4f,
        SpawnPosition = spawnPos,
        PatrolIndex   = 0,
        AttackRange   = 1.8f,
    };

    public static EnemyAIComponent Archer(Vector3 spawnPos) => new()
    {
        Archetype     = EnemyArchetype.Archer,
        State         = EnemyAIState.Idle,
        IdleTimer     = 1.5f,
        SightRange    = 12f,
        LeashRange    = 18f,
        ChaseTarget   = Entity.Null,
        WalkSpeed     = 1.5f,
        RunSpeed      = 3f,
        SpawnPosition = spawnPos,
        PatrolIndex   = 0,
        AttackRange   = 9f,
    };

    public static EnemyAIComponent Brute(Vector3 spawnPos) => new()
    {
        Archetype     = EnemyArchetype.Brute,
        State         = EnemyAIState.Idle,
        IdleTimer     = 3f,
        SightRange    = 6f,
        LeashRange    = 10f,
        ChaseTarget   = Entity.Null,
        WalkSpeed     = 1f,
        RunSpeed      = 2.5f,
        SpawnPosition = spawnPos,
        PatrolIndex   = 0,
        AttackRange   = 2.2f,
    };
}
