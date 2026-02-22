namespace REB.Engine.ECS;

/// <summary>
/// Declares that this system must execute after the specified system type each frame.
/// Multiple attributes may be stacked to express complex ordering requirements.
/// The <see cref="World"/> performs a topological sort of all registered systems on first use.
/// </summary>
/// <example>
/// [RunAfter(typeof(InputSystem))]
/// [RunAfter(typeof(PhysicsSystem))]
/// public class PlayerControllerSystem : GameSystem { ... }
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class RunAfterAttribute : Attribute
{
    public Type DependencyType { get; }

    public RunAfterAttribute(Type dependencyType) => DependencyType = dependencyType;
}
