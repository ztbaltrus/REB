namespace REB.Engine.ECS;

/// <summary>
/// Sparse-set component store for a single component type.
/// Provides O(1) add, remove, and lookup, with dense iteration for cache locality.
/// </summary>
/// <remarks>
/// Internal layout:
///   _dense[i]        — component value at dense slot i
///   _denseToEntity[i] — entity index that owns dense slot i
///   _sparseToDense[e] — dense index for entity e, or -1 if absent
/// </remarks>
public sealed class ComponentPool<T> : IComponentPool
    where T : struct, IComponent
{
    private const int InitialCapacity = 64;

    private T[]    _dense;
    private uint[] _denseToEntity;
    private int[]  _sparseToDense;
    private int    _count;

    public ComponentPool()
    {
        _dense         = new T[InitialCapacity];
        _denseToEntity = new uint[InitialCapacity];
        _sparseToDense = new int[InitialCapacity];
        Array.Fill(_sparseToDense, -1);
    }

    public int Count => _count;

    public bool Has(uint entityIndex) =>
        entityIndex < (uint)_sparseToDense.Length && _sparseToDense[entityIndex] >= 0;

    // -------------------------------------------------------------------------
    //  Read
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns a direct reference into the dense array.
    /// Do NOT add or remove components on this pool while holding this ref.
    /// </summary>
    public ref T GetRef(uint entityIndex)
    {
        if (!Has(entityIndex))
            throw new InvalidOperationException(
                $"Entity {entityIndex} does not have component {typeof(T).Name}.");
        return ref _dense[_sparseToDense[entityIndex]];
    }

    public T Get(uint entityIndex)
    {
        if (!Has(entityIndex))
            throw new InvalidOperationException(
                $"Entity {entityIndex} does not have component {typeof(T).Name}.");
        return _dense[_sparseToDense[entityIndex]];
    }

    public bool TryGet(uint entityIndex, out T component)
    {
        if (Has(entityIndex))
        {
            component = _dense[_sparseToDense[entityIndex]];
            return true;
        }
        component = default;
        return false;
    }

    // -------------------------------------------------------------------------
    //  Write
    // -------------------------------------------------------------------------

    public void Set(uint entityIndex, in T component)
    {
        if (Has(entityIndex))
        {
            _dense[_sparseToDense[entityIndex]] = component;
            return;
        }

        EnsureDenseCapacity(_count + 1);
        EnsureSparseCapacity(entityIndex + 1);

        int slot = _count++;
        _dense[slot]            = component;
        _denseToEntity[slot]    = entityIndex;
        _sparseToDense[entityIndex] = slot;
    }

    public bool Remove(uint entityIndex)
    {
        if (!Has(entityIndex)) return false;

        int slot = _sparseToDense[entityIndex];
        int last = _count - 1;

        if (slot < last)
        {
            // Swap-remove: copy last element into the vacated slot.
            _dense[slot]       = _dense[last];
            uint swapped       = _denseToEntity[last];
            _denseToEntity[slot] = swapped;
            _sparseToDense[swapped] = slot;
        }

        _sparseToDense[entityIndex] = -1;
        _count--;
        return true;
    }

    public void Clear()
    {
        for (int i = 0; i < _count; i++)
            _sparseToDense[_denseToEntity[i]] = -1;
        _count = 0;
    }

    // -------------------------------------------------------------------------
    //  Iteration
    // -------------------------------------------------------------------------

    /// <summary>Dense span of entity indices — iterate directly for the fastest inner loops.</summary>
    public ReadOnlySpan<uint> EntityIndicesSpan => new(_denseToEntity, 0, _count);

    IEnumerable<uint> IComponentPool.EntityIndices
    {
        get
        {
            for (int i = 0; i < _count; i++)
                yield return _denseToEntity[i];
        }
    }

    // -------------------------------------------------------------------------
    //  Capacity helpers
    // -------------------------------------------------------------------------

    private void EnsureDenseCapacity(int required)
    {
        if (required <= _dense.Length) return;
        int next = Math.Max(_dense.Length * 2, required);
        Array.Resize(ref _dense, next);
        Array.Resize(ref _denseToEntity, next);
    }

    private void EnsureSparseCapacity(uint required)
    {
        if (required <= (uint)_sparseToDense.Length) return;
        int oldLen = _sparseToDense.Length;
        int newLen = Math.Max(oldLen * 2, (int)required);
        Array.Resize(ref _sparseToDense, newLen);
        Array.Fill(_sparseToDense, -1, oldLen, newLen - oldLen);
    }
}
