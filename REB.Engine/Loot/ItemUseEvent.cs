using REB.Engine.ECS;

namespace REB.Engine.Loot;

/// <summary>
/// Published by <see cref="REB.Engine.Loot.Systems.UseItemSystem"/> whenever a player
/// activates an item. Consuming systems can react to the effect on the same frame.
/// </summary>
public readonly record struct ItemUseEvent(
    Entity   UserEntity,
    Entity   ItemEntity,
    ItemType ItemType,
    bool     WasConsumed);
