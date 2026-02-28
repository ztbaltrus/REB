using Microsoft.Xna.Framework;
using REB.Engine.ECS;

namespace REB.Engine.Player.Princess.Components;

/// <summary>
/// Navigation agent for the princess. Drives PrincessAISystem's FSM movement
/// when the princess is not being carried.
/// <para>
/// Phase 2: velocity-based movement in open space (no mesh pathfinding).
/// Phase 4+: replace TargetPosition with a proper NavMesh/A* query.
/// </para>
/// </summary>
public struct NavAgentComponent : IComponent
{
    /// <summary>Current AI state.</summary>
    public PrincessAIState CurrentState;

    /// <summary>World-space position the agent is walking toward.</summary>
    public Vector3 TargetPosition;

    /// <summary>Horizontal walk speed in units per second.</summary>
    public float MoveSpeed;

    /// <summary>True when the agent has reached TargetPosition this frame.</summary>
    public bool HasReachedTarget;

    /// <summary>Countdown until the next wander decision (seconds).</summary>
    public float WanderTimer;

    /// <summary>Seconds between wander decisions.</summary>
    public float WanderInterval;

    /// <summary>Maximum horizontal distance from current position for a wander target.</summary>
    public float WanderRadius;

    /// <summary>
    /// When false this instance skips all AI logic (non-authoritative client).
    /// The server is the only authoritative owner of princess state.
    /// </summary>
    public bool IsAuthoritative;

    public static NavAgentComponent Default => new()
    {
        CurrentState    = PrincessAIState.Idle,
        MoveSpeed       = 2f,
        WanderInterval  = 5f,
        WanderRadius    = 4f,
        IsAuthoritative = true,
    };
}
