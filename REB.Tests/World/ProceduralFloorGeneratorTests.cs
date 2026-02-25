using Microsoft.Xna.Framework;
using REB.Engine.ECS;
using REB.Engine.World;
using REB.Engine.World.Components;
using REB.Engine.World.Systems;
using Xunit;

namespace REB.Tests.WorldGeneration;

// ---------------------------------------------------------------------------
//  ProceduralFloorGeneratorSystem tests
//  These run entirely in-process — no GraphicsDevice required.
// ---------------------------------------------------------------------------

public sealed class ProceduralFloorGeneratorTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static (World world, ProceduralFloorGeneratorSystem gen) BuildFloor(
        int seed       = 42,
        int gridWidth  = 24,
        int gridHeight = 24)
    {
        var world = new World();
        var gen   = new ProceduralFloorGeneratorSystem(seed, FloorTheme.Dungeon, gridWidth, gridHeight);
        world.RegisterSystem(gen);
        return (world, gen);
    }

    // -------------------------------------------------------------------------
    //  Seed reproducibility
    // -------------------------------------------------------------------------

    [Fact]
    public void SameSeed_ProducesSameRoomCount()
    {
        var (w1, g1) = BuildFloor(seed: 99);
        var (w2, g2) = BuildFloor(seed: 99);

        Assert.Equal(g1.RoomEntities.Count, g2.RoomEntities.Count);
        w1.Dispose();
        w2.Dispose();
    }

    [Fact]
    public void DifferentSeeds_UsuallyProduceDifferentLayouts()
    {
        var (w1, g1) = BuildFloor(seed: 1);
        var (w2, g2) = BuildFloor(seed: 2);

        // Room counts may happen to match, but tile grids should differ in at
        // least one cell.  We sample a handful of interior cells.
        bool anyDifference = false;
        for (int y = 2; y < 22; y++)
        for (int x = 2; x < 22; x++)
        {
            if (g1.GetTile(x, y) != g2.GetTile(x, y))
            {
                anyDifference = true;
                break;
            }
        }

        Assert.True(anyDifference, "Different seeds produced identical tile grids.");
        w1.Dispose();
        w2.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Room spawning
    // -------------------------------------------------------------------------

    [Fact]
    public void AtLeastTwoRoomsSpawned()
    {
        var (world, gen) = BuildFloor();
        Assert.True(gen.RoomEntities.Count >= 2, "Expected at least 2 rooms (Entrance + Princess).");
        world.Dispose();
    }

    [Fact]
    public void ExactlyOneEntranceHall()
    {
        var (world, gen) = BuildFloor();

        int entrances = 0;
        foreach (var entity in world.Query<RoomComponent>())
        {
            var room = world.GetComponent<RoomComponent>(entity);
            if (room.Type == RoomType.EntranceHall) entrances++;
        }

        Assert.Equal(1, entrances);
        world.Dispose();
    }

    [Fact]
    public void ExactlyOnePrincessChamber()
    {
        var (world, gen) = BuildFloor();

        int count = 0;
        foreach (var entity in world.Query<RoomComponent>())
        {
            var room = world.GetComponent<RoomComponent>(entity);
            if (room.Type == RoomType.PrincessChamber) count++;
        }

        Assert.Equal(1, count);
        world.Dispose();
    }

    [Fact]
    public void AllRoomsHaveCorrectTheme()
    {
        var (world, gen) = BuildFloor();

        foreach (var entity in world.Query<RoomComponent>())
        {
            var room = world.GetComponent<RoomComponent>(entity);
            Assert.Equal(FloorTheme.Dungeon, room.Theme);
        }

        world.Dispose();
    }

    [Fact]
    public void RoomsHavePositiveSize()
    {
        var (world, gen) = BuildFloor();

        foreach (var entity in world.Query<RoomComponent>())
        {
            var room = world.GetComponent<RoomComponent>(entity);
            Assert.True(room.Width  > 0, "Room width must be positive.");
            Assert.True(room.Height > 0, "Room height must be positive.");
        }

        world.Dispose();
    }

    [Fact]
    public void RoomsAreTaggedCorrectly()
    {
        var (world, gen) = BuildFloor();

        // Every room entity must carry the "Room" tag.
        foreach (var entity in gen.RoomEntities)
            Assert.True(world.HasTag(entity, "Room"), "Room entity missing 'Room' tag.");

        // Exactly one "Entrance" tag.
        Assert.Single(world.GetEntitiesWithTag("Entrance"));

        // Exactly one "PrincessChamber" tag.
        Assert.Single(world.GetEntitiesWithTag("PrincessChamber"));

        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Tile grid
    // -------------------------------------------------------------------------

    [Fact]
    public void OutOfBoundsTileReturnsEmpty()
    {
        var (world, gen) = BuildFloor();

        Assert.Equal(ProceduralFloorGeneratorSystem.TileType.Empty, gen.GetTile(-1,  0));
        Assert.Equal(ProceduralFloorGeneratorSystem.TileType.Empty, gen.GetTile( 0, -1));
        Assert.Equal(ProceduralFloorGeneratorSystem.TileType.Empty, gen.GetTile(100, 0));

        world.Dispose();
    }

    [Fact]
    public void AtLeastSomeFloorTilesExist()
    {
        var (world, gen) = BuildFloor();

        int floorCount = 0;
        for (int y = 0; y < gen.GridHeight; y++)
        for (int x = 0; x < gen.GridWidth;  x++)
            if (gen.GetTile(x, y) == ProceduralFloorGeneratorSystem.TileType.Floor)
                floorCount++;

        Assert.True(floorCount > 0, "No floor tiles were carved.");
        world.Dispose();
    }

    [Fact]
    public void WallTilesOnlyAdjacentToFloor()
    {
        var (world, gen) = BuildFloor();

        for (int y = 1; y < gen.GridHeight - 1; y++)
        for (int x = 1; x < gen.GridWidth  - 1; x++)
        {
            if (gen.GetTile(x, y) != ProceduralFloorGeneratorSystem.TileType.Wall) continue;

            bool hasFloorNeighbor = false;
            for (int dy = -1; dy <= 1 && !hasFloorNeighbor; dy++)
            for (int dx = -1; dx <= 1 && !hasFloorNeighbor; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                if (gen.GetTile(x + dx, y + dy) == ProceduralFloorGeneratorSystem.TileType.Floor)
                    hasFloorNeighbor = true;
            }

            Assert.True(hasFloorNeighbor, $"Wall at ({x},{y}) has no adjacent floor tile.");
        }

        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  TileToWorld
    // -------------------------------------------------------------------------

    [Fact]
    public void TileToWorld_OriginTile_IsAtHalfTileSize()
    {
        const float ts   = ProceduralFloorGeneratorSystem.TileSize;
        var         pos  = ProceduralFloorGeneratorSystem.TileToWorld(0, 0);

        Assert.Equal(ts * 0.5f, pos.X, precision: 4);
        Assert.Equal(0f,        pos.Y, precision: 4);
        Assert.Equal(ts * 0.5f, pos.Z, precision: 4);
    }
}
