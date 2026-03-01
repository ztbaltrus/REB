using Microsoft.Xna.Framework;
using REB.Engine.ECS;

namespace REB.Engine.Boss;

/// <summary>
/// Fired by <see cref="Systems.BossSystem"/> on the frame the boss's health reaches zero.
/// Consumed by floor-unlock logic and loot-spawn systems.
/// </summary>
public readonly struct BossDefeatedEvent
{
    /// <summary>Entity that held the <see cref="Components.BossComponent"/> (already marked Defeated).</summary>
    public readonly Entity BossEntity;

    /// <summary>World-space position where the boss died (used for loot drop placement).</summary>
    public readonly Vector3 DeathPosition;

    /// <summary>Seed passed to the loot-generation system for deterministic boss drops.</summary>
    public readonly int LootSeed;

    public BossDefeatedEvent(Entity bossEntity, Vector3 deathPos, int lootSeed)
    {
        BossEntity    = bossEntity;
        DeathPosition = deathPos;
        LootSeed      = lootSeed;
    }
}
