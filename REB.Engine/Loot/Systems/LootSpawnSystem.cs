using Microsoft.Xna.Framework;
using REB.Engine.ECS;
using REB.Engine.Loot.Components;
using REB.Engine.Physics;
using REB.Engine.Physics.Components;
using REB.Engine.Rendering.Components;
using REB.Engine.World.Components;

namespace REB.Engine.Loot.Systems;

/// <summary>
/// Spawns loot items into the world on initial load and whenever containers are opened.
/// <para>Pipeline:</para>
/// <list type="number">
///   <item>First <see cref="Update"/> call: scatters items across room entities
///         (or around the origin when running headlessly in tests).</item>
///   <item>Every update: spawns contents for newly-opened
///         <see cref="LootContainerComponent"/> entities.</item>
/// </list>
/// </summary>
public sealed class LootSpawnSystem : GameSystem
{
    private readonly int _globalSeed;
    private readonly int _floorDifficulty;
    private          bool _initialSpawnDone;
    private          int  _spawnSequence;

    public LootSpawnSystem(int seed = 42, int floorDifficulty = 1)
    {
        _globalSeed      = seed;
        _floorDifficulty = floorDifficulty;
    }

    public override void Update(float deltaTime)
    {
        if (!_initialSpawnDone)
        {
            InitialSpawn();
            _initialSpawnDone = true;
        }

        SpawnOpenedContainerLoot();
    }

    // =========================================================================
    //  Public API
    // =========================================================================

    /// <summary>
    /// Spawns <paramref name="count"/> items near <paramref name="origin"/> using
    /// a difficulty-scaled weighted table. Safe to call from tests directly.
    /// </summary>
    public void SpawnLoot(int count, int floorDifficulty, int seed, Vector3 origin)
    {
        // Mix the seed with a global sequence counter so repeated calls differ.
        var rng = new Random(unchecked(seed ^ (_spawnSequence++ * (int)2654435761u)));
        for (int i = 0; i < count; i++)
        {
            var offset = new Vector3(
                (float)(rng.NextDouble() * 2.0 - 1.0),
                0f,
                (float)(rng.NextDouble() * 2.0 - 1.0));
            SpawnItem(origin + offset, floorDifficulty, rng);
        }
    }

    // =========================================================================
    //  Internal
    // =========================================================================

    private void InitialSpawn()
    {
        // Gather room centres; fall back to the world origin in headless contexts.
        var origins = new List<Vector3>();
        foreach (var room in World.Query<RoomComponent, TransformComponent>())
            origins.Add(World.GetComponent<TransformComponent>(room).Position);

        if (origins.Count == 0)
            origins.Add(Vector3.Zero);

        var rng   = new Random(_globalSeed);
        int count = 5 + _floorDifficulty * 2;

        for (int i = 0; i < count; i++)
        {
            var origin = origins[rng.Next(origins.Count)];
            var offset = new Vector3(
                (float)(rng.NextDouble() * 8.0 - 4.0),
                0f,
                (float)(rng.NextDouble() * 8.0 - 4.0));
            SpawnItem(origin + offset, _floorDifficulty, rng);
        }
    }

    private void SpawnOpenedContainerLoot()
    {
        foreach (var container in World.Query<LootContainerComponent, TransformComponent>())
        {
            ref var lc = ref World.GetComponent<LootContainerComponent>(container);
            if (!lc.IsOpened || lc.LootCount > 0) continue;

            var ctf   = World.GetComponent<TransformComponent>(container);
            int count = lc.ContainerType switch
            {
                LootContainerType.Chest  => 2 + lc.FloorDifficulty / 3,
                LootContainerType.Shrine => 1,
                LootContainerType.Corpse => 1,
                _                        => 1,
            };

            SpawnLoot(count, lc.FloorDifficulty, lc.Seed, ctf.Position);
            lc.LootCount = count;
        }
    }

    private void SpawnItem(Vector3 position, int floorDifficulty, Random rng)
    {
        var item = World.CreateEntity();
        World.AddTag(item, "Item");

        World.AddComponent(item, new TransformComponent
        {
            Position    = position,
            Rotation    = Quaternion.Identity,
            Scale       = Vector3.One,
            WorldMatrix = Matrix.Identity,
        });

        World.AddComponent(item, PickItemComponent(floorDifficulty, rng));

        World.AddComponent(item, new RigidBodyComponent
        {
            Velocity    = Vector3.Zero,
            Mass        = 1f,
            UseGravity  = true,
            LinearDrag  = 2f,
            IsKinematic = false,
        });

        World.AddComponent(item, ColliderComponent.Box(
            halfExtents: new Vector3(0.2f, 0.2f, 0.2f),
            layer:       CollisionLayer.Loot,
            mask:        CollisionLayer.Terrain,
            isStatic:    false));
    }

    /// <summary>
    /// Selects an item preset from a difficulty-scaled weighted table.
    /// <code>
    /// difficulty 1 : Common ~70 %, Rare ~20 %, Legendary ~5 %, Cursed ~5 %
    /// difficulty 10: Common ~16 %, Rare ~38 %, Legendary ~41 %, Cursed ~5 %
    /// </code>
    /// </summary>
    private static ItemComponent PickItemComponent(int floorDifficulty, Random rng)
    {
        int roll  = rng.Next(100);
        int shift = (floorDifficulty - 1) * 4;   // 0 – 36

        int cursedBound    = 5;
        int legendaryBound = cursedBound + 5 + shift;
        int rareBound      = legendaryBound + 20 + shift / 2;

        if (roll < cursedBound)    return ItemComponent.CursedRelic;
        if (roll < legendaryBound) return ItemComponent.Artifact;
        if (roll < rareBound)      return ItemComponent.Gem;
        return ItemComponent.Coin;
    }
}
