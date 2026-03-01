using Microsoft.Xna.Framework;
using REB.Engine.ECS;
using REB.Engine.Physics;
using REB.Engine.Physics.Components;
using REB.Engine.Rendering;
using REB.Engine.Rendering.Components;
using REB.Engine.World.Components;

namespace REB.Engine.World.Systems;

/// <summary>
/// Generates a tower floor on initialization using Binary Space Partitioning (BSP).
/// <para>Generation pipeline:</para>
/// <list type="number">
///   <item>Recursively partition a fixed-size grid into BSP leaf nodes.</item>
///   <item>Place one room inside each leaf, randomised within the node bounds.</item>
///   <item>Connect sibling node pairs with L-shaped corridors.</item>
///   <item>Grow walls around all floor tiles.</item>
///   <item>Assign special roles (Entrance, Princess Chamber, Treasure).</item>
///   <item>Spawn ECS entities for each room and wall tile.</item>
/// </list>
/// After <see cref="GameSystem.OnInitialize"/> the generated layout is accessible
/// through <see cref="RoomEntities"/>, <see cref="GetTile"/>, and debug draw.
/// </summary>
public sealed class ProceduralFloorGeneratorSystem : GameSystem
{
    // -------------------------------------------------------------------------
    //  Public constants
    // -------------------------------------------------------------------------

    /// <summary>World units per grid tile. Multiply any tile coord by this to get world position.</summary>
    public const float TileSize = 2f;

    // -------------------------------------------------------------------------
    //  Configuration
    // -------------------------------------------------------------------------

    private int        _seed;
    private FloorTheme _theme;
    private readonly int        _gridWidth;
    private readonly int        _gridHeight;

    // -------------------------------------------------------------------------
    //  Generation state
    // -------------------------------------------------------------------------

    private Random     _rng    = null!;
    private TileType[,] _grid  = null!;

    // Leaf data kept alongside the BSP tree for entity spawning.
    private readonly List<LeafData> _leaves = new();

    // -------------------------------------------------------------------------
    //  Public results (valid after OnInitialize)
    // -------------------------------------------------------------------------

    /// <summary>Floor seed used for generation. Identical seeds produce identical layouts.</summary>
    public int Seed => _seed;

    /// <summary>Theme applied to all rooms in this floor.</summary>
    public FloorTheme Theme => _theme;

    /// <summary>Grid width in tiles.</summary>
    public int GridWidth => _gridWidth;

    /// <summary>Grid height in tiles.</summary>
    public int GridHeight => _gridHeight;

    /// <summary>All room entities spawned during generation.</summary>
    public IReadOnlyList<Entity> RoomEntities => _roomEntities;

    private readonly List<Entity> _roomEntities = new();

    // -------------------------------------------------------------------------
    //  Constructor
    // -------------------------------------------------------------------------

    /// <param name="seed">RNG seed. Same seed → same layout every time.</param>
    /// <param name="theme">Visual and gameplay theme applied to all rooms.</param>
    /// <param name="gridWidth">Tile columns. Must be at least 20.</param>
    /// <param name="gridHeight">Tile rows. Must be at least 20.</param>
    public ProceduralFloorGeneratorSystem(
        int        seed,
        FloorTheme theme      = FloorTheme.Dungeon,
        int        gridWidth  = 48,
        int        gridHeight = 48)
    {
        _seed       = seed;
        _theme      = theme;
        _gridWidth  = Math.Max(gridWidth,  20);
        _gridHeight = Math.Max(gridHeight, 20);
    }

    // =========================================================================
    //  Lifecycle
    // =========================================================================

    protected override void OnInitialize()
    {
        _rng  = new Random(_seed);
        _grid = new TileType[_gridWidth, _gridHeight];
        GenerateFloor();
    }

    /// <summary>
    /// Destroys all existing room and wall entities, then regenerates the floor
    /// using <paramref name="newSeed"/> and <paramref name="newTheme"/>.
    /// Safe to call at run-start; used by
    /// <see cref="REB.Engine.RunManagement.Systems.RunManagerSystem"/>.
    /// </summary>
    public void Regenerate(int newSeed, FloorTheme newTheme)
    {
        // Destroy all previously-generated room and wall entities.
        var toDestroy = new List<Entity>();
        foreach (var e in World.GetEntitiesWithTag("Room")) toDestroy.Add(e);
        foreach (var e in World.GetEntitiesWithTag("Wall")) toDestroy.Add(e);
        foreach (var e in World.GetEntitiesWithTag("Entrance")) { /* tag-only; entity already queued */ }
        foreach (var e in toDestroy)
        {
            if (World.IsAlive(e)) World.DestroyEntity(e);
        }

        _roomEntities.Clear();
        _leaves.Clear();

        // Apply new seed and theme, then regenerate.
        _seed  = newSeed;
        _theme = newTheme;
        _rng   = new Random(_seed);
        _grid  = new TileType[_gridWidth, _gridHeight];
        GenerateFloor();
    }

    /// <summary>Draws a top-down wireframe of all rooms using <see cref="DebugDraw"/>.</summary>
    public override void Draw(GameTime gameTime)
    {
        foreach (var entity in World.Query<RoomComponent>())
        {
            ref var room = ref World.GetComponent<RoomComponent>(entity);

            var color = room.Type switch
            {
                RoomType.EntranceHall    => Color.LimeGreen,
                RoomType.PrincessChamber => Color.HotPink,
                RoomType.TreasureRoom    => Color.Gold,
                RoomType.BossArena       => Color.OrangeRed,
                _                        => Color.CornflowerBlue,
            };

            float x0 = room.GridX * TileSize;
            float z0 = room.GridY * TileSize;
            float x1 = (room.GridX + room.Width)  * TileSize;
            float z1 = (room.GridY + room.Height) * TileSize;
            const float y = 0.15f;

            DebugDraw.DrawLine(new Vector3(x0, y, z0), new Vector3(x1, y, z0), color);
            DebugDraw.DrawLine(new Vector3(x1, y, z0), new Vector3(x1, y, z1), color);
            DebugDraw.DrawLine(new Vector3(x1, y, z1), new Vector3(x0, y, z1), color);
            DebugDraw.DrawLine(new Vector3(x0, y, z1), new Vector3(x0, y, z0), color);
        }
    }

    // -------------------------------------------------------------------------
    //  Public query helpers
    // -------------------------------------------------------------------------

    /// <summary>Returns the tile type at grid position (x, y), or <see cref="TileType.Empty"/> if out of bounds.</summary>
    public TileType GetTile(int x, int y)
    {
        if (x < 0 || y < 0 || x >= _gridWidth || y >= _gridHeight) return TileType.Empty;
        return _grid[x, y];
    }

    /// <summary>Returns the world-space centre position of tile (x, y) at floor level (Y = 0).</summary>
    public static Vector3 TileToWorld(int x, int y) =>
        new(x * TileSize + TileSize * 0.5f, 0f, y * TileSize + TileSize * 0.5f);

    // =========================================================================
    //  Floor generation
    // =========================================================================

    private void GenerateFloor()
    {
        // 1. BSP partition
        var root = new BspNode(1, 1, _gridWidth - 2, _gridHeight - 2);
        Split(root, 0);

        // 2. Place rooms inside leaf nodes; carve floor tiles
        PlaceRooms(root);

        // 3. Connect sibling node centres with L-corridors
        ConnectSiblings(root);

        // 4. Border floor regions with wall tiles
        BuildWalls();

        // 5. Assign special room types
        AssignRoomTypes();

        // 6. Spawn ECS entities
        SpawnEntities();
    }

    // -------------------------------------------------------------------------
    //  BSP split
    // -------------------------------------------------------------------------

    private const int MinNodeSize  = 8;  // smallest a BSP node can be
    private const int MinRoomSize  = 4;  // smallest a room can be (tiles)
    private const int MaxDepth     = 5;

    private void Split(BspNode node, int depth)
    {
        if (depth >= MaxDepth) return;
        if (node.W < MinNodeSize * 2 && node.H < MinNodeSize * 2) return;

        bool splitH;
        if      (node.W < MinNodeSize * 2) splitH = true;
        else if (node.H < MinNodeSize * 2) splitH = false;
        else                               splitH = _rng.Next(2) == 0;

        if (splitH)
        {
            int split     = _rng.Next(MinNodeSize, node.H - MinNodeSize + 1);
            node.Left     = new BspNode(node.X, node.Y,          node.W, split);
            node.Right    = new BspNode(node.X, node.Y + split,  node.W, node.H - split);
        }
        else
        {
            int split     = _rng.Next(MinNodeSize, node.W - MinNodeSize + 1);
            node.Left     = new BspNode(node.X,         node.Y, split,          node.H);
            node.Right    = new BspNode(node.X + split, node.Y, node.W - split, node.H);
        }

        Split(node.Left!,  depth + 1);
        Split(node.Right!, depth + 1);
    }

    private void PlaceRooms(BspNode node)
    {
        if (node.IsLeaf)
        {
            int pad  = 1;
            int maxW = Math.Max(MinRoomSize, node.W - pad * 2);
            int maxH = Math.Max(MinRoomSize, node.H - pad * 2);

            int w = _rng.Next(MinRoomSize, maxW + 1);
            int h = _rng.Next(MinRoomSize, maxH + 1);

            int ox = node.W - pad * 2 - w;
            int oy = node.H - pad * 2 - h;
            int x  = node.X + pad + (ox > 0 ? _rng.Next(0, ox + 1) : 0);
            int y  = node.Y + pad + (oy > 0 ? _rng.Next(0, oy + 1) : 0);

            node.Room = new RectI(x, y, w, h);

            for (int ty = y; ty < y + h; ty++)
            for (int tx = x; tx < x + w; tx++)
                _grid[tx, ty] = TileType.Floor;

            _leaves.Add(new LeafData(node, new RectI(x, y, w, h)));
        }
        else
        {
            if (node.Left  != null) PlaceRooms(node.Left);
            if (node.Right != null) PlaceRooms(node.Right);

            // Parent room used only for corridor routing.
            node.Room = (node.Left, node.Right) switch
            {
                (not null, not null) => RectI.Union(node.Left.Room, node.Right.Room),
                (not null, null)     => node.Left.Room,
                (null,     not null) => node.Right.Room,
                _                   => default,
            };
        }
    }

    private void ConnectSiblings(BspNode node)
    {
        if (node.IsLeaf || node.Left == null || node.Right == null) return;

        ConnectSiblings(node.Left);
        ConnectSiblings(node.Right);

        // L-shaped corridor between the two child room centres.
        var a = node.Left.Room.Center;
        var b = node.Right.Room.Center;

        if (_rng.Next(2) == 0)
        {
            CarveRow(a.x, b.x, a.y);
            CarveCol(a.y, b.y, b.x);
        }
        else
        {
            CarveCol(a.y, b.y, a.x);
            CarveRow(a.x, b.x, b.y);
        }
    }

    private void CarveRow(int x1, int x2, int y)
    {
        int lo = Math.Min(x1, x2), hi = Math.Max(x1, x2);
        for (int x = lo; x <= hi; x++)
            if (_grid[x, y] == TileType.Empty)
                _grid[x, y] = TileType.Floor;
    }

    private void CarveCol(int y1, int y2, int x)
    {
        int lo = Math.Min(y1, y2), hi = Math.Max(y1, y2);
        for (int y = lo; y <= hi; y++)
            if (_grid[x, y] == TileType.Empty)
                _grid[x, y] = TileType.Floor;
    }

    private void BuildWalls()
    {
        for (int y = 0; y < _gridHeight; y++)
        for (int x = 0; x < _gridWidth;  x++)
        {
            if (_grid[x, y] != TileType.Empty) continue;
            if (HasAdjacentFloor(x, y)) _grid[x, y] = TileType.Wall;
        }
    }

    private bool HasAdjacentFloor(int x, int y)
    {
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = x + dx, ny = y + dy;
            if (nx < 0 || ny < 0 || nx >= _gridWidth || ny >= _gridHeight) continue;
            if (_grid[nx, ny] == TileType.Floor) return true;
        }
        return false;
    }

    // -------------------------------------------------------------------------
    //  Room type assignment
    // -------------------------------------------------------------------------

    private void AssignRoomTypes()
    {
        if (_leaves.Count == 0) return;

        // Find the two rooms that are furthest apart — use them for entrance and princess.
        int entranceIdx = 0, princessIdx = 0;
        float maxDist = -1f;

        for (int i = 0; i < _leaves.Count; i++)
        for (int j = i + 1; j < _leaves.Count; j++)
        {
            var ci = _leaves[i].Room.Center;
            var cj = _leaves[j].Room.Center;
            float d = MathF.Sqrt(
                (ci.x - cj.x) * (ci.x - cj.x) +
                (ci.y - cj.y) * (ci.y - cj.y));
            if (d <= maxDist) continue;
            maxDist     = d;
            entranceIdx = i;
            princessIdx = j;
        }

        _leaves[entranceIdx].Node.AssignedType = RoomType.EntranceHall;
        _leaves[princessIdx].Node.AssignedType = RoomType.PrincessChamber;

        for (int i = 0; i < _leaves.Count; i++)
        {
            if (i == entranceIdx || i == princessIdx) continue;
            _leaves[i].Node.AssignedType = _rng.Next(10) < 2
                ? RoomType.TreasureRoom
                : RoomType.Chamber;
        }
    }

    // -------------------------------------------------------------------------
    //  Entity spawning
    // -------------------------------------------------------------------------

    private const float WallHeight = TileSize * 1.5f;

    private void SpawnEntities()
    {
        SpawnRooms();
        SpawnWalls();
    }

    private void SpawnRooms()
    {
        foreach (var leaf in _leaves)
        {
            var room   = leaf.Room;
            var type   = leaf.Node.AssignedType;
            var entity = World.CreateEntity();

            World.AddComponent(entity, new RoomComponent
            {
                GridX     = room.X,
                GridY     = room.Y,
                Width     = room.W,
                Height    = room.H,
                Theme     = _theme,
                Type      = type,
            });

            float cx = (room.X + room.W * 0.5f) * TileSize;
            float cz = (room.Y + room.H * 0.5f) * TileSize;

            World.AddComponent(entity, new TransformComponent
            {
                Position    = new Vector3(cx, 0f, cz),
                Rotation    = Quaternion.Identity,
                Scale       = Vector3.One,
                WorldMatrix = Matrix.Identity,
            });

            // Floor-level static AABB collider for the room footprint.
            World.AddComponent(entity, ColliderComponent.Box(
                halfExtents: new Vector3(room.W * TileSize * 0.5f, 0.1f, room.H * TileSize * 0.5f),
                layer:       CollisionLayer.Terrain,
                mask:        CollisionLayer.All,
                isStatic:    true));

            World.AddTag(entity, "Room");
            if (type == RoomType.EntranceHall)    World.AddTag(entity, "Entrance");
            if (type == RoomType.PrincessChamber) World.AddTag(entity, "PrincessChamber");

            _roomEntities.Add(entity);
        }
    }

    private void SpawnWalls()
    {
        float half = TileSize * 0.5f;

        for (int y = 0; y < _gridHeight; y++)
        for (int x = 0; x < _gridWidth;  x++)
        {
            if (_grid[x, y] != TileType.Wall) continue;

            var entity = World.CreateEntity();

            float wx = x * TileSize + half;
            float wz = y * TileSize + half;

            World.AddComponent(entity, new TransformComponent
            {
                Position    = new Vector3(wx, WallHeight * 0.5f, wz),
                Rotation    = Quaternion.Identity,
                Scale       = Vector3.One,
                WorldMatrix = Matrix.Identity,
            });

            World.AddComponent(entity, ColliderComponent.Box(
                halfExtents: new Vector3(half, WallHeight * 0.5f, half),
                layer:       CollisionLayer.Terrain,
                mask:        CollisionLayer.All,
                isStatic:    true));

            World.AddTag(entity, "Wall");
        }
    }

    // =========================================================================
    //  Tile type enum (public so external systems can interpret GetTile results)
    // =========================================================================

    /// <summary>Grid cell classification used by the internal tile map.</summary>
    public enum TileType : byte
    {
        Empty  = 0,
        Floor  = 1,
        Wall   = 2,
        Door   = 3,
        Stairs = 4,
    }

    // =========================================================================
    //  BSP helper types
    // =========================================================================

    private sealed class BspNode(int x, int y, int w, int h)
    {
        public readonly int X = x, Y = y, W = w, H = h;
        public BspNode? Left, Right;
        public RectI    Room;
        public RoomType AssignedType = RoomType.Chamber;

        public bool IsLeaf => Left == null && Right == null;
    }

    private sealed class LeafData(BspNode node, RectI room)
    {
        public readonly BspNode Node = node;
        public readonly RectI   Room = room;
    }

    /// <summary>Integer rectangle for grid-space arithmetic.</summary>
    private readonly record struct RectI(int X, int Y, int W, int H)
    {
        public (int x, int y) Center => (X + W / 2, Y + H / 2);

        public static RectI Union(RectI a, RectI b)
        {
            int minX = Math.Min(a.X, b.X);
            int minY = Math.Min(a.Y, b.Y);
            int maxX = Math.Max(a.X + a.W, b.X + b.W);
            int maxY = Math.Max(a.Y + a.H, b.Y + b.H);
            return new(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
