using Microsoft.Xna.Framework;
using REB.Engine.ECS;
using REB.Engine.Loot;
using REB.Engine.Loot.Components;
using REB.Engine.Loot.Systems;
using REB.Engine.Rendering.Components;
using Xunit;

namespace REB.Tests.Loot;

// ---------------------------------------------------------------------------
//  LootSpawnSystem tests
//
//  No PhysicsSystem or InputSystem needed — LootSpawnSystem only requires the World
//  to create entities. PhysicsSystem would normally act on RigidBodyComponents, but
//  its absence does not prevent entity creation.
// ---------------------------------------------------------------------------

public sealed class LootSpawnTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static World BuildWorld(int seed = 42, int difficulty = 1)
    {
        var world = new World();
        world.RegisterSystem(new LootSpawnSystem(seed, difficulty));
        return world;
    }

    private static int CountItems(World world)
    {
        int n = 0;
        foreach (var _ in world.Query<ItemComponent>()) n++;
        return n;
    }

    // -------------------------------------------------------------------------
    //  Initial spawn
    // -------------------------------------------------------------------------

    [Fact]
    public void InitialSpawn_CreatesItems_AfterFirstUpdate()
    {
        var world = BuildWorld(seed: 1, difficulty: 1);

        world.Update(0.016f);

        // difficulty 1 → 5 + 1*2 = 7 items
        Assert.Equal(7, CountItems(world));
        world.Dispose();
    }

    [Fact]
    public void HigherDifficulty_SpawnsMoreItems()
    {
        var worldLow  = BuildWorld(seed: 1, difficulty: 1);
        var worldHigh = BuildWorld(seed: 1, difficulty: 5);

        worldLow.Update(0.016f);
        worldHigh.Update(0.016f);

        int lowCount  = CountItems(worldLow);
        int highCount = CountItems(worldHigh);

        Assert.True(highCount > lowCount,
            $"Expected more items at difficulty 5 ({highCount}) than difficulty 1 ({lowCount}).");

        worldLow.Dispose();
        worldHigh.Dispose();
    }

    [Fact]
    public void SecondUpdate_DoesNotSpawnAdditionalInitialItems()
    {
        var world = BuildWorld(seed: 1, difficulty: 1);

        world.Update(0.016f);
        int afterFirst = CountItems(world);

        world.Update(0.016f);
        int afterSecond = CountItems(world);

        Assert.Equal(afterFirst, afterSecond);
        world.Dispose();
    }

    [Fact]
    public void SeedBased_SameRuns_ProduceSameCount()
    {
        var world1 = BuildWorld(seed: 999, difficulty: 3);
        var world2 = BuildWorld(seed: 999, difficulty: 3);

        world1.Update(0.016f);
        world2.Update(0.016f);

        Assert.Equal(CountItems(world1), CountItems(world2));

        world1.Dispose();
        world2.Dispose();
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentRarities()
    {
        // Run a large sample and verify the rarity weighted table is biased
        // toward higher rarity at high difficulty.
        const int Samples   = 200;
        const int Difficulty = 10;

        int legendaryHighDiff = 0;
        int legendaryLowDiff  = 0;

        for (int i = 0; i < Samples; i++)
        {
            var worldHigh = BuildWorld(seed: i, difficulty: Difficulty);
            var worldLow  = BuildWorld(seed: i, difficulty: 1);

            worldHigh.Update(0.016f);
            worldLow.Update(0.016f);

            foreach (var item in worldHigh.Query<ItemComponent>())
                if (worldHigh.GetComponent<ItemComponent>(item).Rarity == ItemRarity.Legendary)
                    legendaryHighDiff++;

            foreach (var item in worldLow.Query<ItemComponent>())
                if (worldLow.GetComponent<ItemComponent>(item).Rarity == ItemRarity.Legendary)
                    legendaryLowDiff++;

            worldHigh.Dispose();
            worldLow.Dispose();
        }

        Assert.True(legendaryHighDiff > legendaryLowDiff,
            $"Expected more Legendary items at difficulty {Difficulty} " +
            $"({legendaryHighDiff}) than difficulty 1 ({legendaryLowDiff}).");
    }

    // -------------------------------------------------------------------------
    //  SpawnLoot public API
    // -------------------------------------------------------------------------

    [Fact]
    public void SpawnLoot_CreatesExactCount()
    {
        var world = new World();
        var sys   = new LootSpawnSystem(seed: 1, floorDifficulty: 1);
        world.RegisterSystem(sys);

        // Skip initial spawn by running one update first, then count.
        world.Update(0.016f);
        int before = CountItems(world);

        sys.SpawnLoot(count: 3, floorDifficulty: 1, seed: 77, origin: Vector3.Zero);

        Assert.Equal(before + 3, CountItems(world));
        world.Dispose();
    }

    [Fact]
    public void SpawnLoot_ItemsHaveItemComponent()
    {
        var world = new World();
        var sys   = new LootSpawnSystem();
        world.RegisterSystem(sys);
        world.Update(0.016f);  // initial spawn

        // Spawn one extra item and verify it's tagged and has a component.
        sys.SpawnLoot(count: 1, floorDifficulty: 1, seed: 1, origin: Vector3.Zero);

        int tagged = 0;
        foreach (var _ in world.GetEntitiesWithTag("Item")) tagged++;
        Assert.True(tagged > 0);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Container loot
    // -------------------------------------------------------------------------

    [Fact]
    public void OpenedContainer_SpawnsLootOnNextFrame()
    {
        var world = BuildWorld(seed: 1, difficulty: 1);
        world.Update(0.016f);  // initial spawn
        int before = CountItems(world);

        // Create a chest and mark it as opened.
        var container = world.CreateEntity();
        world.AddComponent(container, new TransformComponent
        {
            Position    = Vector3.Zero,
            Rotation    = Quaternion.Identity,
            Scale       = Vector3.One,
            WorldMatrix = Matrix.Identity,
        });
        var lc = LootContainerComponent.Chest(seed: 100, difficulty: 2);
        lc.IsOpened = true;
        world.AddComponent(container, lc);

        world.Update(0.016f);  // LootSpawnSystem should pick up the opened container

        // Chest at difficulty 2 → 2 + 2/3 = 2 items
        Assert.Equal(before + 2, CountItems(world));
        world.Dispose();
    }

    [Fact]
    public void ClosedContainer_DoesNotSpawnLoot()
    {
        var world = BuildWorld(seed: 1, difficulty: 1);
        world.Update(0.016f);
        int before = CountItems(world);

        var container = world.CreateEntity();
        world.AddComponent(container, new TransformComponent
        {
            Position    = Vector3.Zero,
            Rotation    = Quaternion.Identity,
            Scale       = Vector3.One,
            WorldMatrix = Matrix.Identity,
        });
        world.AddComponent(container, LootContainerComponent.Chest(seed: 100));  // IsOpened = false

        world.Update(0.016f);

        Assert.Equal(before, CountItems(world));
        world.Dispose();
    }
}
