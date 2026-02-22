namespace REB.Engine.ECS;

/// <summary>
/// Marker interface for all ECS components.
/// Components are pure data bags — no logic, no methods beyond property accessors.
/// Implement as structs for cache-friendly, GC-pressure-free storage.
/// </summary>
public interface IComponent { }
