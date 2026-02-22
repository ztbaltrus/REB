using REB.Engine.ECS;
using Xunit;

namespace REB.Tests.ECS;

// ---------------------------------------------------------------------------
//  Test component stubs
// ---------------------------------------------------------------------------

public struct HealthComponent : IComponent
{
    public int Current;
    public int Max;
}

public struct PositionComponent : IComponent
{
    public float X;
    public float Y;
}

public struct VelocityComponent : IComponent
{
    public float Vx;
    public float Vy;
}

public struct TagMarkerComponent : IComponent { }

// ---------------------------------------------------------------------------
//  Test system stubs
// ---------------------------------------------------------------------------

public class OrderTrackerSystem : GameSystem
{
    public static readonly List<string> Log = new();
    private readonly string _name;
    public OrderTrackerSystem(string name) => _name = name;
    public override void Update(float dt) => Log.Add(_name);
}

[RunAfter(typeof(OrderTrackerSystem))]
public class DependentSystem : GameSystem
{
    public static readonly List<string> Log = new();
    public override void Update(float dt) => Log.Add("Dependent");
}

// ---------------------------------------------------------------------------
//  Entity tests
// ---------------------------------------------------------------------------

public class EntityLifecycleTests
{
    [Fact]
    public void CreateEntity_ReturnsValidEntity()
    {
        var world  = new World();
        var entity = world.CreateEntity();
        Assert.True(entity.IsValid);
        Assert.True(world.IsAlive(entity));
    }

    [Fact]
    public void DestroyEntity_EntityBecomesStale()
    {
        var world  = new World();
        var entity = world.CreateEntity();
        world.DestroyEntity(entity);
        Assert.False(world.IsAlive(entity));
    }

    [Fact]
    public void RecycledSlot_GetsNewVersion()
    {
        var world = new World();
        var e1    = world.CreateEntity();
        world.DestroyEntity(e1);

        var e2 = world.CreateEntity(); // reuses the slot
        Assert.Equal(e1.Index, e2.Index);
        Assert.NotEqual(e1.Version, e2.Version);
        Assert.True(world.IsAlive(e2));
        Assert.False(world.IsAlive(e1));
    }

    [Fact]
    public void NullEntity_IsNotAlive()
    {
        var world = new World();
        Assert.False(world.IsAlive(Entity.Null));
    }

    [Fact]
    public void CreateMany_AllUnique()
    {
        var world    = new World();
        var entities = Enumerable.Range(0, 256).Select(_ => world.CreateEntity()).ToList();
        Assert.Equal(256, entities.Distinct().Count());
    }
}

// ---------------------------------------------------------------------------
//  Component tests
// ---------------------------------------------------------------------------

public class ComponentTests
{
    [Fact]
    public void AddAndGet_ReturnsCorrectValue()
    {
        var world  = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new HealthComponent { Current = 50, Max = 100 });

        ref var hp = ref world.GetComponent<HealthComponent>(entity);
        Assert.Equal(50, hp.Current);
        Assert.Equal(100, hp.Max);
    }

    [Fact]
    public void GetRef_MutationPersists()
    {
        var world  = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new HealthComponent { Current = 100, Max = 100 });

        world.GetComponent<HealthComponent>(entity).Current = 75;
        Assert.Equal(75, world.GetComponent<HealthComponent>(entity).Current);
    }

    [Fact]
    public void HasComponent_TrueAfterAdd_FalseAfterRemove()
    {
        var world  = new World();
        var entity = world.CreateEntity();

        Assert.False(world.HasComponent<HealthComponent>(entity));
        world.AddComponent(entity, new HealthComponent { Current = 1, Max = 1 });
        Assert.True(world.HasComponent<HealthComponent>(entity));
        world.RemoveComponent<HealthComponent>(entity);
        Assert.False(world.HasComponent<HealthComponent>(entity));
    }

    [Fact]
    public void TryGetComponent_ReturnsFalseWhenAbsent()
    {
        var world  = new World();
        var entity = world.CreateEntity();
        Assert.False(world.TryGetComponent<HealthComponent>(entity, out _));
    }

    [Fact]
    public void SetComponent_OverwritesExistingValue()
    {
        var world  = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new HealthComponent { Current = 10, Max = 10 });
        world.SetComponent(entity, new HealthComponent { Current = 99, Max = 100 });
        Assert.Equal(99, world.GetComponent<HealthComponent>(entity).Current);
    }

    [Fact]
    public void DestroyEntity_RemovesAllComponents()
    {
        var world  = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new HealthComponent { Current = 1, Max = 1 });
        world.AddComponent(entity, new PositionComponent { X = 1 });
        world.DestroyEntity(entity);

        // Pool should not report the entity as having any components.
        var other = world.CreateEntity(); // reuses slot
        Assert.False(world.HasComponent<HealthComponent>(other));
        Assert.False(world.HasComponent<PositionComponent>(other));
    }
}

// ---------------------------------------------------------------------------
//  Component pool tests
// ---------------------------------------------------------------------------

public class ComponentPoolTests
{
    [Fact]
    public void SparseSet_StressInsertRemove()
    {
        var pool = new ComponentPool<HealthComponent>();

        for (uint i = 1; i <= 500; i++)
            pool.Set(i, new HealthComponent { Current = (int)i, Max = 100 });

        Assert.Equal(500, pool.Count);

        // Remove even-indexed entries.
        for (uint i = 2; i <= 500; i += 2)
            pool.Remove(i);

        Assert.Equal(250, pool.Count);

        // Remaining entries should still be correct.
        for (uint i = 1; i <= 499; i += 2)
            Assert.Equal((int)i, pool.Get(i).Current);
    }

    [Fact]
    public void Pool_GrowsBeyondInitialCapacity()
    {
        var pool = new ComponentPool<PositionComponent>();
        for (uint i = 1; i <= 1024; i++)
            pool.Set(i, new PositionComponent { X = i, Y = i * 2 });

        Assert.Equal(1024, pool.Count);
        Assert.Equal(512f, pool.Get(512).X);
    }
}

// ---------------------------------------------------------------------------
//  Query tests
// ---------------------------------------------------------------------------

public class QueryTests
{
    [Fact]
    public void Query1_ReturnsAllWithComponent()
    {
        var world = new World();
        for (int i = 0; i < 10; i++)
        {
            var e = world.CreateEntity();
            if (i % 2 == 0)
                world.AddComponent(e, new HealthComponent { Current = i, Max = 10 });
        }

        var results = world.Query<HealthComponent>().ToList();
        Assert.Equal(5, results.Count);
    }

    [Fact]
    public void Query2_ReturnsIntersection()
    {
        var world = new World();
        var eA = world.CreateEntity();
        var eB = world.CreateEntity();
        var eC = world.CreateEntity();

        world.AddComponent(eA, new HealthComponent { Current = 1, Max = 1 });
        world.AddComponent(eA, new PositionComponent { X = 1 });

        world.AddComponent(eB, new HealthComponent { Current = 2, Max = 2 });
        // eB has no PositionComponent

        world.AddComponent(eC, new PositionComponent { X = 3 });
        // eC has no HealthComponent

        var results = world.Query<HealthComponent, PositionComponent>().ToList();
        Assert.Single(results);
        Assert.Equal(eA, results[0]);
    }

    [Fact]
    public void Query3_ReturnsTripleIntersection()
    {
        var world = new World();
        var full  = world.CreateEntity();
        var part  = world.CreateEntity();

        world.AddComponent(full, new HealthComponent { Current = 1, Max = 1 });
        world.AddComponent(full, new PositionComponent { X = 1 });
        world.AddComponent(full, new VelocityComponent { Vx = 1 });

        world.AddComponent(part, new HealthComponent { Current = 1, Max = 1 });
        world.AddComponent(part, new PositionComponent { X = 2 });

        var results = world.Query<HealthComponent, PositionComponent, VelocityComponent>().ToList();
        Assert.Single(results);
        Assert.Equal(full, results[0]);
    }
}

// ---------------------------------------------------------------------------
//  Tag tests
// ---------------------------------------------------------------------------

public class TagTests
{
    [Fact]
    public void AddTag_HasTag_RemoveTag()
    {
        var world  = new World();
        var entity = world.CreateEntity();

        world.AddTag(entity, "player");
        Assert.True(world.HasTag(entity, "player"));

        world.RemoveTag(entity, "player");
        Assert.False(world.HasTag(entity, "player"));
    }

    [Fact]
    public void GetEntitiesWithTag_ReturnsCorrectSet()
    {
        var world = new World();
        var e1 = world.CreateEntity();
        var e2 = world.CreateEntity();
        var e3 = world.CreateEntity();

        world.AddTag(e1, "enemy");
        world.AddTag(e2, "enemy");
        world.AddTag(e3, "player");

        var enemies = world.GetEntitiesWithTag("enemy").ToHashSet();
        Assert.Contains(e1, enemies);
        Assert.Contains(e2, enemies);
        Assert.DoesNotContain(e3, enemies);
    }

    [Fact]
    public void DestroyEntity_RemovesTags()
    {
        var world  = new World();
        var entity = world.CreateEntity();
        world.AddTag(entity, "loot");
        world.DestroyEntity(entity);
        Assert.Empty(world.GetEntitiesWithTag("loot"));
    }
}

// ---------------------------------------------------------------------------
//  System ordering tests
// ---------------------------------------------------------------------------

public class SystemOrderingTests
{
    [Fact]
    public void RegisterSystem_ExecutesInOrder()
    {
        OrderTrackerSystem.Log.Clear();
        DependentSystem.Log.Clear();

        var world = new World();
        // Register dependent BEFORE its dependency to test topological sort.
        world.RegisterSystem(new DependentSystem());
        world.RegisterSystem(new OrderTrackerSystem("Base"));

        world.Update(0f);

        Assert.Equal("Base",      OrderTrackerSystem.Log[0]);
        Assert.Equal("Dependent", DependentSystem.Log[0]);
    }

    [Fact]
    public void GetSystem_ReturnsRegisteredSystem()
    {
        var world = new World();
        var input = new OrderTrackerSystem("input");
        world.RegisterSystem(input);
        Assert.Same(input, world.GetSystem<OrderTrackerSystem>());
    }

    [Fact]
    public void RegisterSameTwice_Throws()
    {
        var world = new World();
        world.RegisterSystem(new OrderTrackerSystem("a"));
        Assert.Throws<InvalidOperationException>(() =>
            world.RegisterSystem(new OrderTrackerSystem("b")));
    }
}
