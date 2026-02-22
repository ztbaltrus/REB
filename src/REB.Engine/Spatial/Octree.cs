using Microsoft.Xna.Framework;

namespace REB.Engine.Spatial;

/// <summary>
/// A loose octree for broad-phase spatial queries.
/// Stores items by their AABB and supports intersection queries by box or sphere.
/// Items that straddle node boundaries are stored in the nearest ancestor node.
/// </summary>
/// <typeparam name="T">The type stored in the tree (typically <see cref="REB.Engine.ECS.Entity"/>).</typeparam>
public sealed class Octree<T>
{
    private readonly OctreeNode _root;
    private readonly int        _maxDepth;
    private readonly int        _maxItemsPerNode;

    public Octree(BoundingBox worldBounds, int maxDepth = 6, int maxItemsPerNode = 8)
    {
        _root            = new OctreeNode(worldBounds);
        _maxDepth        = maxDepth;
        _maxItemsPerNode = maxItemsPerNode;
    }

    // -------------------------------------------------------------------------
    //  Public API
    // -------------------------------------------------------------------------

    /// <summary>Inserts an item with the given world-space AABB into the tree.</summary>
    public void Insert(T item, BoundingBox bounds) =>
        _root.Insert(item, bounds, 0, _maxDepth, _maxItemsPerNode);

    /// <summary>Collects all items whose AABB intersects <paramref name="queryBox"/>.</summary>
    public void Query(BoundingBox queryBox, ICollection<T> results) =>
        _root.Query(queryBox, results);

    /// <summary>Collects all items whose AABB intersects <paramref name="sphere"/>.</summary>
    public void Query(BoundingSphere sphere, ICollection<T> results) =>
        _root.Query(sphere, results);

    /// <summary>Removes all items from the tree without deallocating nodes.</summary>
    public void Clear() => _root.Clear();

    // =========================================================================
    //  Internal node
    // =========================================================================

    private sealed class OctreeNode
    {
        public readonly BoundingBox Bounds;
        public OctreeNode[]? Children;
        public readonly List<(T Item, BoundingBox Box)> Items = new();

        public OctreeNode(BoundingBox bounds) => Bounds = bounds;

        // -------------------------------------------------------------------------
        //  Insert
        // -------------------------------------------------------------------------

        public void Insert(T item, BoundingBox box, int depth, int maxDepth, int maxItems)
        {
            // Subdivide if overfull and within depth budget.
            if (depth < maxDepth && Children == null && Items.Count >= maxItems)
                Subdivide();

            // Try to push into a single child that fully contains the item.
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    if (child.Bounds.Contains(box) == ContainmentType.Contains)
                    {
                        child.Insert(item, box, depth + 1, maxDepth, maxItems);
                        return;
                    }
                }
            }

            // Item straddles children or this is a leaf — store here.
            Items.Add((item, box));
        }

        // -------------------------------------------------------------------------
        //  Query — box
        // -------------------------------------------------------------------------

        public void Query(BoundingBox queryBox, ICollection<T> results)
        {
            if (!Bounds.Intersects(queryBox)) return;

            foreach (var (item, box) in Items)
                if (box.Intersects(queryBox))
                    results.Add(item);

            if (Children == null) return;
            foreach (var child in Children)
                child.Query(queryBox, results);
        }

        // -------------------------------------------------------------------------
        //  Query — sphere
        // -------------------------------------------------------------------------

        public void Query(BoundingSphere sphere, ICollection<T> results)
        {
            if (Bounds.Contains(sphere) == ContainmentType.Disjoint) return;

            foreach (var (item, box) in Items)
                if (box.Intersects(sphere))
                    results.Add(item);

            if (Children == null) return;
            foreach (var child in Children)
                child.Query(sphere, results);
        }

        // -------------------------------------------------------------------------
        //  Clear
        // -------------------------------------------------------------------------

        public void Clear()
        {
            Items.Clear();
            if (Children == null) return;
            foreach (var child in Children)
                child.Clear();
            Children = null;
        }

        // -------------------------------------------------------------------------
        //  Subdivide into 8 child octants
        // -------------------------------------------------------------------------

        private void Subdivide()
        {
            var min = Bounds.Min;
            var max = Bounds.Max;
            var cen = (min + max) * 0.5f;

            Children =
            [
                new(new BoundingBox(min,                                      cen)),
                new(new BoundingBox(new Vector3(cen.X, min.Y, min.Z),        new Vector3(max.X, cen.Y, cen.Z))),
                new(new BoundingBox(new Vector3(min.X, min.Y, cen.Z),        new Vector3(cen.X, cen.Y, max.Z))),
                new(new BoundingBox(new Vector3(cen.X, min.Y, cen.Z),        new Vector3(max.X, cen.Y, max.Z))),
                new(new BoundingBox(new Vector3(min.X, cen.Y, min.Z),        new Vector3(cen.X, max.Y, cen.Z))),
                new(new BoundingBox(new Vector3(cen.X, cen.Y, min.Z),        new Vector3(max.X, max.Y, cen.Z))),
                new(new BoundingBox(new Vector3(min.X, cen.Y, cen.Z),        new Vector3(cen.X, max.Y, max.Z))),
                new(new BoundingBox(cen,                                      max)),
            ];
        }
    }
}
