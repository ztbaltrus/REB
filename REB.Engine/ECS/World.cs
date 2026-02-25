using System.Reflection;
using Microsoft.Xna.Framework;

namespace REB.Engine.ECS;

/// <summary>
/// Central ECS container. Owns all entities, component pools, tags, and systems.
/// <para>Usage flow: create entities → attach components → register systems → call Update/Draw each frame.</para>
/// </summary>
public sealed class World : IDisposable
{
    // -------------------------------------------------------------------------
    //  Entity management
    // -------------------------------------------------------------------------

    private const int MaxEntities = 65_536;

    /// <summary>Per-slot version counter. Increments on each entity reuse.</summary>
    private readonly uint[] _versions = new uint[MaxEntities];

    /// <summary>Next unallocated slot (1-based; slot 0 = Entity.Null).</summary>
    private uint _nextIndex = 1;

    private readonly Queue<uint> _freeList = new();

    // -------------------------------------------------------------------------
    //  Component storage
    // -------------------------------------------------------------------------

    private readonly Dictionary<Type, IComponentPool> _pools = new();

    // -------------------------------------------------------------------------
    //  Tag system
    // -------------------------------------------------------------------------

    /// <summary>tag-name → set of entity indices that carry it.</summary>
    private readonly Dictionary<string, HashSet<uint>> _tagToEntities = new();

    /// <summary>entity-index → set of tags it carries (for fast remove-all on destroy).</summary>
    private readonly Dictionary<uint, HashSet<string>> _entityToTags = new();

    // -------------------------------------------------------------------------
    //  Systems
    // -------------------------------------------------------------------------

    private readonly List<GameSystem> _systems = new();
    private readonly Dictionary<Type, GameSystem> _systemByType = new();

    /// <summary>Topologically sorted system list, rebuilt when stale.</summary>
    private List<GameSystem>? _orderedSystems;

    private bool _disposed;

    // =========================================================================
    //  Entities
    // =========================================================================

    public Entity CreateEntity()
    {
        uint index;
        if (!_freeList.TryDequeue(out index))
        {
            if (_nextIndex >= MaxEntities)
                throw new InvalidOperationException(
                    $"World entity limit ({MaxEntities}) reached.");
            index = _nextIndex++;
        }
        return new Entity(index, _versions[index]);
    }

    public void DestroyEntity(Entity entity)
    {
        ThrowIfDead(entity);

        // Strip all components.
        foreach (var pool in _pools.Values)
            pool.Remove(entity.Index);

        // Strip all tags.
        if (_entityToTags.TryGetValue(entity.Index, out var tagSet))
        {
            foreach (var tag in tagSet)
            {
                if (_tagToEntities.TryGetValue(tag, out var bucket))
                    bucket.Remove(entity.Index);
            }
            _entityToTags.Remove(entity.Index);
        }

        _versions[entity.Index]++;
        _freeList.Enqueue(entity.Index);
    }

    public bool IsAlive(Entity entity) =>
        entity.IsValid &&
        entity.Index < _nextIndex &&
        _versions[entity.Index] == entity.Version;

    // =========================================================================
    //  Components
    // =========================================================================

    public void AddComponent<T>(Entity entity, in T component) where T : struct, IComponent
    {
        ThrowIfDead(entity);
        GetOrCreatePool<T>().Set(entity.Index, component);
    }

    /// <summary>Alias for <see cref="AddComponent{T}"/>; overwrites existing value if present.</summary>
    public void SetComponent<T>(Entity entity, in T component) where T : struct, IComponent
        => AddComponent(entity, component);

    /// <summary>
    /// Returns a direct reference into the dense component array.
    /// Efficient for in-place mutation. Do NOT structurally modify the pool (add/remove)
    /// while holding this reference.
    /// </summary>
    public ref T GetComponent<T>(Entity entity) where T : struct, IComponent
    {
        ThrowIfDead(entity);
        return ref GetOrCreatePool<T>().GetRef(entity.Index);
    }

    public bool TryGetComponent<T>(Entity entity, out T component) where T : struct, IComponent
    {
        if (!IsAlive(entity)) { component = default; return false; }
        if (_pools.TryGetValue(typeof(T), out var raw) && raw is ComponentPool<T> pool)
            return pool.TryGet(entity.Index, out component);
        component = default;
        return false;
    }

    public bool HasComponent<T>(Entity entity) where T : struct, IComponent
    {
        if (!IsAlive(entity)) return false;
        return _pools.TryGetValue(typeof(T), out var pool) && pool.Has(entity.Index);
    }

    public bool RemoveComponent<T>(Entity entity) where T : struct, IComponent
    {
        ThrowIfDead(entity);
        if (_pools.TryGetValue(typeof(T), out var pool))
            return pool.Remove(entity.Index);
        return false;
    }

    // =========================================================================
    //  Tags
    // =========================================================================

    public void AddTag(Entity entity, string tag)
    {
        ThrowIfDead(entity);

        if (!_tagToEntities.TryGetValue(tag, out var bucket))
            _tagToEntities[tag] = bucket = new HashSet<uint>();
        bucket.Add(entity.Index);

        if (!_entityToTags.TryGetValue(entity.Index, out var entityTags))
            _entityToTags[entity.Index] = entityTags = new HashSet<string>();
        entityTags.Add(tag);
    }

    public void RemoveTag(Entity entity, string tag)
    {
        ThrowIfDead(entity);
        if (_tagToEntities.TryGetValue(tag, out var bucket))
            bucket.Remove(entity.Index);
        if (_entityToTags.TryGetValue(entity.Index, out var entityTags))
            entityTags.Remove(tag);
    }

    public bool HasTag(Entity entity, string tag) =>
        IsAlive(entity) &&
        _tagToEntities.TryGetValue(tag, out var bucket) &&
        bucket.Contains(entity.Index);

    /// <summary>Returns all alive entities that carry the given tag.</summary>
    public IEnumerable<Entity> GetEntitiesWithTag(string tag)
    {
        if (!_tagToEntities.TryGetValue(tag, out var bucket)) yield break;
        foreach (var index in bucket)
            yield return new Entity(index, _versions[index]);
    }

    // =========================================================================
    //  Queries
    // =========================================================================

    /// <summary>All entities with component T1.</summary>
    public IEnumerable<Entity> Query<T1>()
        where T1 : struct, IComponent
    {
        foreach (uint index in ((IComponentPool)GetOrCreatePool<T1>()).EntityIndices)
            yield return new Entity(index, _versions[index]);
    }

    /// <summary>All entities with both T1 and T2.</summary>
    public IEnumerable<Entity> Query<T1, T2>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
    {
        var p1 = GetOrCreatePool<T1>();
        var p2 = GetOrCreatePool<T2>();

        IComponentPool smaller = p1.Count <= p2.Count ? (IComponentPool)p1 : p2;
        IComponentPool larger  = p1.Count <= p2.Count ? (IComponentPool)p2 : p1;

        foreach (uint index in smaller.EntityIndices)
        {
            if (larger.Has(index))
                yield return new Entity(index, _versions[index]);
        }
    }

    /// <summary>All entities with T1, T2, and T3.</summary>
    public IEnumerable<Entity> Query<T1, T2, T3>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
    {
        IComponentPool[] pools =
        [
            GetOrCreatePool<T1>(), GetOrCreatePool<T2>(), GetOrCreatePool<T3>()
        ];
        Array.Sort(pools, static (a, b) => a.Count - b.Count);

        foreach (uint index in pools[0].EntityIndices)
        {
            if (pools[1].Has(index) && pools[2].Has(index))
                yield return new Entity(index, _versions[index]);
        }
    }

    /// <summary>All entities with T1, T2, T3, and T4.</summary>
    public IEnumerable<Entity> Query<T1, T2, T3, T4>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
        where T4 : struct, IComponent
    {
        IComponentPool[] pools =
        [
            GetOrCreatePool<T1>(), GetOrCreatePool<T2>(),
            GetOrCreatePool<T3>(), GetOrCreatePool<T4>()
        ];
        Array.Sort(pools, static (a, b) => a.Count - b.Count);

        foreach (uint index in pools[0].EntityIndices)
        {
            if (pools[1].Has(index) && pools[2].Has(index) && pools[3].Has(index))
                yield return new Entity(index, _versions[index]);
        }
    }

    // =========================================================================
    //  Systems
    // =========================================================================

    public void RegisterSystem(GameSystem system)
    {
        var type = system.GetType();
        if (_systemByType.ContainsKey(type))
            throw new InvalidOperationException($"System {type.Name} is already registered.");

        _systems.Add(system);
        _systemByType[type] = system;
        _orderedSystems = null; // invalidate sorted cache
        system.Initialize(this);
    }

    public T GetSystem<T>() where T : GameSystem
    {
        if (_systemByType.TryGetValue(typeof(T), out var system))
            return (T)system;
        throw new KeyNotFoundException($"System {typeof(T).Name} is not registered in this world.");
    }

    public bool TryGetSystem<T>(out T? system) where T : GameSystem
    {
        if (_systemByType.TryGetValue(typeof(T), out var raw))
        {
            system = (T)raw;
            return true;
        }
        system = null;
        return false;
    }

    /// <summary>Runs one update tick. Systems execute in topological dependency order.</summary>
    public void Update(float deltaTime)
    {
        EnsureSystemsOrdered();
        foreach (var s in _orderedSystems!)
            s.Update(deltaTime);
    }

    /// <summary>Runs the draw pass. Render systems should override <see cref="GameSystem.Draw"/>.</summary>
    public void Draw(GameTime gameTime)
    {
        EnsureSystemsOrdered();
        foreach (var s in _orderedSystems!)
            s.Draw(gameTime);
    }

    // =========================================================================
    //  IDisposable
    // =========================================================================

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var s in _systems) s.OnShutdown();
        _systems.Clear();
        _systemByType.Clear();
        _pools.Clear();
    }

    // =========================================================================
    //  Internal helpers
    // =========================================================================

    private ComponentPool<T> GetOrCreatePool<T>() where T : struct, IComponent
    {
        if (!_pools.TryGetValue(typeof(T), out var raw))
            _pools[typeof(T)] = raw = new ComponentPool<T>();
        return (ComponentPool<T>)raw;
    }

    private void EnsureSystemsOrdered()
    {
        if (_orderedSystems != null) return;
        _orderedSystems = TopologicalSort(_systems);
    }

    private static List<GameSystem> TopologicalSort(List<GameSystem> systems)
    {
        var inDegree = new Dictionary<GameSystem, int>(systems.Count);
        var edges    = new Dictionary<GameSystem, List<GameSystem>>(systems.Count);
        var byType   = systems.ToDictionary(s => s.GetType());

        foreach (var s in systems)
        {
            inDegree[s] = 0;
            edges[s]    = [];
        }

        foreach (var s in systems)
        {
            foreach (var attr in s.GetType().GetCustomAttributes<RunAfterAttribute>(false))
            {
                if (!byType.TryGetValue(attr.DependencyType, out var dep)) continue;
                edges[dep].Add(s);
                inDegree[s]++;
            }
        }

        var queue  = new Queue<GameSystem>(systems.Where(s => inDegree[s] == 0));
        var result = new List<GameSystem>(systems.Count);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);
            foreach (var next in edges[current])
                if (--inDegree[next] == 0)
                    queue.Enqueue(next);
        }

        if (result.Count != systems.Count)
            throw new InvalidOperationException(
                "Cycle detected in system dependency graph. Check RunAfter attributes.");

        return result;
    }

    private void ThrowIfDead(Entity entity)
    {
        if (!IsAlive(entity))
            throw new ArgumentException($"{entity} is not alive.", nameof(entity));
    }
}
