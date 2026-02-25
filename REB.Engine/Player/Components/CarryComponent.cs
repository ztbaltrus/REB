using REB.Engine.ECS;

namespace REB.Engine.Player.Components;

/// <summary>
/// Enables an entity to pick up and carry another entity (typically the princess).
/// Managed by <see cref="REB.Engine.Player.Systems.CarrySystem"/>.
/// </summary>
public struct CarryComponent : IComponent
{
    /// <summary>True while this entity is actively carrying <see cref="CarriedEntity"/>.</summary>
    public bool IsCarrying;

    /// <summary>The entity currently being carried, or <see cref="Entity.Null"/>.</summary>
    public Entity CarriedEntity;

    /// <summary>Maximum distance (world units) at which the carrier can pick up or hand off.</summary>
    public float InteractRange;

    /// <summary>Y-axis offset above the carrier's position where the carried entity is positioned.</summary>
    public float CarryOffsetY;

    public static CarryComponent Default => new()
    {
        IsCarrying    = false,
        CarriedEntity = Entity.Null,
        InteractRange = 1.5f,
        CarryOffsetY  = 1.0f,
    };
}
