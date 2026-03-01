using REB.Engine.ECS;

namespace REB.Engine.Combat.Components;

/// <summary>
/// Marks an entity as capable of dealing melee or ranged damage.
/// Attack timing and range are managed here; <see cref="Systems.CombatSystem"/>
/// handles hit detection and applies the damage.
/// </summary>
public struct DamageComponent : IComponent
{
    /// <summary>Base damage per successful hit.</summary>
    public float Damage;

    /// <summary>Reach (world units) within which a melee hit registers.</summary>
    public float MeleeRange;

    /// <summary>Distance (world units) at which a ranged attack connects.</summary>
    public float RangedRange;

    /// <summary>True if this attacker uses ranged range instead of melee range.</summary>
    public bool IsRanged;

    /// <summary>Minimum seconds between consecutive attacks.</summary>
    public float AttackCooldown;

    /// <summary>Seconds remaining until the next attack is allowed. Decremented each frame.</summary>
    public float AttackTimer;

    /// <summary>
    /// Set to true by AI or input systems to request an attack this frame.
    /// Consumed and cleared by CombatSystem.
    /// </summary>
    public bool AttackPressed;

    /// <summary>Impulse magnitude applied to targets on hit (knockback force).</summary>
    public float KnockbackForce;

    public static DamageComponent MeleeDefault => new()
    {
        Damage         = 10f,
        MeleeRange     = 1.5f,
        RangedRange    = 8f,
        IsRanged       = false,
        AttackCooldown = 1.0f,
        AttackTimer    = 0f,
        AttackPressed  = false,
        KnockbackForce = 4f,
    };

    public static DamageComponent RangedDefault => new()
    {
        Damage         = 8f,
        MeleeRange     = 1.5f,
        RangedRange    = 10f,
        IsRanged       = true,
        AttackCooldown = 1.5f,
        AttackTimer    = 0f,
        AttackPressed  = false,
        KnockbackForce = 2f,
    };
}
