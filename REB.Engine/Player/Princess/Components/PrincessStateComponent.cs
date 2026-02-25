using REB.Engine.ECS;

namespace REB.Engine.Player.Princess.Components;

/// <summary>
/// Health, mood, and carry state of the princess entity.
/// Updated by <see cref="REB.Engine.Player.Systems.CarrySystem"/>.
/// </summary>
public struct PrincessStateComponent : IComponent
{
    /// <summary>Current health (0–100). Drains while carried; her mood degrades with it.</summary>
    public float Health;

    /// <summary>Current mood tier, derived from <see cref="Health"/>.</summary>
    public PrincessMoodLevel MoodLevel;

    /// <summary>Base mood (health) drain in points per second while being carried.</summary>
    public float MoodDecayRate;

    /// <summary>True when a carrier entity currently holds the princess.</summary>
    public bool IsBeingCarried;

    /// <summary>The entity currently carrying the princess, or <see cref="Entity.Null"/>.</summary>
    public Entity CarrierEntity;

    /// <summary>True during a struggle burst (triggered when mood reaches Furious).</summary>
    public bool IsStruggling;

    /// <summary>Seconds remaining in the current struggle burst.</summary>
    public float StruggleTimer;

    public static PrincessStateComponent Default => new()
    {
        Health         = 100f,
        MoodLevel      = PrincessMoodLevel.Calm,
        MoodDecayRate  = 2f,
        IsBeingCarried = false,
        CarrierEntity  = Entity.Null,
        IsStruggling   = false,
        StruggleTimer  = 0f,
    };
}
