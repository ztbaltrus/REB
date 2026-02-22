namespace REB.Engine.ECS;

/// <summary>
/// Non-generic interface to a typed component pool, used by <see cref="World"/> internals.
/// Callers that know the concrete type should use <see cref="ComponentPool{T}"/> directly.
/// </summary>
internal interface IComponentPool
{
    int Count { get; }
    bool Has(uint entityIndex);

    /// <summary>Removes the component for the given entity index (no-op if absent).</summary>
    bool Remove(uint entityIndex);

    /// <summary>Removes all components and resets the pool.</summary>
    void Clear();

    /// <summary>Enumerates the entity indices currently stored in this pool.</summary>
    IEnumerable<uint> EntityIndices { get; }
}
