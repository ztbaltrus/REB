namespace REB.Engine.Physics;

/// <summary>
/// Bitmask layers used to filter collision between entities.
/// Assign a <c>Layer</c> to each entity's collider and a <c>LayerMask</c>
/// specifying which layers it should interact with.
/// Two entities collide only when <c>(A.Layer &amp; B.LayerMask) != 0</c>.
/// </summary>
[Flags]
public enum CollisionLayer : uint
{
    None    = 0,
    Default = 1 << 0,
    Player  = 1 << 1,
    Enemy   = 1 << 2,
    Terrain = 1 << 3,
    Loot    = 1 << 4,
    Trigger = 1 << 5,
    All     = uint.MaxValue,
}
