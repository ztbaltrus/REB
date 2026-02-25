using Microsoft.Xna.Framework;
using REB.Engine.ECS;
using REB.Engine.Spatial;
using Xunit;

namespace REB.Tests.Spatial;

// ---------------------------------------------------------------------------
//  Octree<T> unit tests
//  Tests pure spatial logic; no ECS world or GraphicsDevice required.
// ---------------------------------------------------------------------------

public sealed class OctreeTests
{
    private static Octree<int> BuildOctree(float size = 100f, int maxDepth = 5, int maxItems = 4) =>
        new(new BoundingBox(new Vector3(-size * 0.5f), new Vector3(size * 0.5f)), maxDepth, maxItems);

    private static BoundingBox PointBox(float x, float y, float z, float half = 0.5f) =>
        new(new Vector3(x - half, y - half, z - half),
            new Vector3(x + half, y + half, z + half));

    // -------------------------------------------------------------------------
    //  Insert and box query
    // -------------------------------------------------------------------------

    [Fact]
    public void Query_FindsInsertedItem()
    {
        var tree = BuildOctree();
        tree.Insert(42, PointBox(0f, 0f, 0f));

        var results = new List<int>();
        tree.Query(new BoundingBox(new Vector3(-1f), new Vector3(1f)), results);

        Assert.Contains(42, results);
    }

    [Fact]
    public void Query_DoesNotFindDistantItem()
    {
        var tree = BuildOctree();
        tree.Insert(42, PointBox(40f, 0f, 0f));

        var results = new List<int>();
        tree.Query(new BoundingBox(new Vector3(-1f), new Vector3(1f)), results);

        Assert.DoesNotContain(42, results);
    }

    [Fact]
    public void Query_FindsMultipleItems()
    {
        var tree = BuildOctree();
        tree.Insert(1, PointBox(-1f, 0f, 0f));
        tree.Insert(2, PointBox( 0f, 0f, 0f));
        tree.Insert(3, PointBox( 1f, 0f, 0f));

        var results = new List<int>();
        tree.Query(new BoundingBox(new Vector3(-2f, -1f, -1f), new Vector3(2f, 1f, 1f)), results);

        Assert.Contains(1, results);
        Assert.Contains(2, results);
        Assert.Contains(3, results);
    }

    // -------------------------------------------------------------------------
    //  Sphere query
    // -------------------------------------------------------------------------

    [Fact]
    public void SphereQuery_FindsItemInsideSphere()
    {
        var tree = BuildOctree();
        tree.Insert(10, PointBox(1f, 0f, 0f));

        var results = new List<int>();
        tree.Query(new BoundingSphere(Vector3.Zero, 3f), results);

        Assert.Contains(10, results);
    }

    [Fact]
    public void SphereQuery_DoesNotFindItemOutsideSphere()
    {
        var tree = BuildOctree();
        tree.Insert(10, PointBox(30f, 0f, 0f));

        var results = new List<int>();
        tree.Query(new BoundingSphere(Vector3.Zero, 3f), results);

        Assert.DoesNotContain(10, results);
    }

    // -------------------------------------------------------------------------
    //  Clear
    // -------------------------------------------------------------------------

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var tree = BuildOctree();
        tree.Insert(1, PointBox(0f, 0f, 0f));
        tree.Insert(2, PointBox(1f, 0f, 0f));

        tree.Clear();

        var results = new List<int>();
        tree.Query(new BoundingBox(new Vector3(-50f), new Vector3(50f)), results);

        Assert.Empty(results);
    }

    [Fact]
    public void Insert_AfterClear_Works()
    {
        var tree = BuildOctree();
        tree.Insert(1, PointBox(0f, 0f, 0f));
        tree.Clear();
        tree.Insert(2, PointBox(0f, 0f, 0f));

        var results = new List<int>();
        tree.Query(new BoundingBox(new Vector3(-1f), new Vector3(1f)), results);

        Assert.DoesNotContain(1, results);
        Assert.Contains(2, results);
    }

    // -------------------------------------------------------------------------
    //  Subdivision stress test
    // -------------------------------------------------------------------------

    [Fact]
    public void ManyItems_AllRetrievableByFullWorldQuery()
    {
        var tree   = BuildOctree(maxItems: 4);
        var random = new Random(1234);
        var ids    = new HashSet<int>();

        for (int i = 0; i < 200; i++)
        {
            float x = (float)(random.NextDouble() * 80 - 40);
            float y = (float)(random.NextDouble() * 80 - 40);
            float z = (float)(random.NextDouble() * 80 - 40);
            tree.Insert(i, PointBox(x, y, z));
            ids.Add(i);
        }

        var results = new List<int>();
        tree.Query(new BoundingBox(new Vector3(-50f), new Vector3(50f)), results);

        // All inserted items should appear in a full-world query.
        foreach (var id in ids)
            Assert.Contains(id, results);
    }

    // -------------------------------------------------------------------------
    //  Edge cases
    // -------------------------------------------------------------------------

    [Fact]
    public void EmptyTree_QueryReturnsNoResults()
    {
        var tree    = BuildOctree();
        var results = new List<int>();
        tree.Query(new BoundingBox(new Vector3(-50f), new Vector3(50f)), results);
        Assert.Empty(results);
    }

    [Fact]
    public void Item_StradlesChildBoundary_StillRetrievable()
    {
        // A large item that straddles octant boundaries should still be queryable.
        var tree = BuildOctree(maxItems: 1); // force immediate subdivision
        tree.Insert(99, new BoundingBox(new Vector3(-20f, -0.5f, -20f),
                                        new Vector3( 20f,  0.5f,  20f)));

        var results = new List<int>();
        tree.Query(new BoundingBox(new Vector3(-1f), new Vector3(1f)), results);

        Assert.Contains(99, results);
    }
}
