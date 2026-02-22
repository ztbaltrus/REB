using Microsoft.Xna.Framework;
using REB.Engine.ECS;
using REB.Engine.Physics.Components;
using REB.Engine.Physics.Systems;
using REB.Engine.Rendering.Components;

namespace REB.Engine.Spatial.Systems;

/// <summary>
/// Maintains an <see cref="Octree{T}"/> of all entities that have both a
/// <see cref="ColliderComponent"/> and a <see cref="TransformComponent"/>.
/// <para>
/// The octree is rebuilt from scratch every frame so that dynamic bodies are
/// always accurately positioned. In Phase 4+ this can be made incremental
/// (update only moved entities) if profiling identifies it as a bottleneck.
/// </para>
/// Runs before <see cref="PhysicsSystem"/> so physics can use spatial queries
/// for broad-phase candidate selection.
/// </summary>
[RunAfter(typeof(Input.InputSystem))]
public sealed class SpatialSystem : GameSystem
{
    private Octree<Entity> _octree = null!;

    private readonly float _worldSize;

    /// <param name="worldSize">
    /// Side length of the octree root cube in world units.
    /// Should comfortably contain the entire generated floor.
    /// </param>
    public SpatialSystem(float worldSize = 512f)
    {
        _worldSize = worldSize;
    }

    protected override void OnInitialize()
    {
        float half = _worldSize * 0.5f;
        _octree = new Octree<Entity>(
            new BoundingBox(new Vector3(-half, -half, -half),
                            new Vector3( half,  half,  half)),
            maxDepth:        6,
            maxItemsPerNode: 8);
    }

    public override void Update(float deltaTime)
    {
        _octree.Clear();

        foreach (var entity in World.Query<ColliderComponent, TransformComponent>())
        {
            var col = World.GetComponent<ColliderComponent>(entity);
            var tf  = World.GetComponent<TransformComponent>(entity);

            var box = new BoundingBox(
                tf.Position - col.HalfExtents,
                tf.Position + col.HalfExtents);

            _octree.Insert(entity, box);
        }
    }

    // -------------------------------------------------------------------------
    //  Query API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns all entities whose AABB overlaps <paramref name="box"/>.
    /// Results are appended to <paramref name="results"/>; clear it first if needed.
    /// </summary>
    public void QueryBox(BoundingBox box, ICollection<Entity> results) =>
        _octree.Query(box, results);

    /// <summary>
    /// Returns all entities whose AABB overlaps <paramref name="sphere"/>.
    /// Results are appended to <paramref name="results"/>; clear it first if needed.
    /// </summary>
    public void QuerySphere(BoundingSphere sphere, ICollection<Entity> results) =>
        _octree.Query(sphere, results);
}
