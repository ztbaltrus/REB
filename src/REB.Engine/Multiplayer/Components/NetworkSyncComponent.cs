using REB.Engine.ECS;

namespace REB.Engine.Multiplayer.Components;

/// <summary>
/// Marks an entity for network replication.
/// <para>
/// Phase 2 (local co-op): marker only — all players share one in-process <see cref="World"/>,
/// so no actual transmission occurs.
/// </para>
/// <para>
/// Phase 3+ (online): <see cref="REB.Engine.Multiplayer.Systems.NetworkSyncSystem"/> will
/// serialise <c>TransformComponent</c> + <c>RigidBodyComponent</c> for flagged entities and
/// transmit them over the chosen transport layer.
/// </para>
/// </summary>
public struct NetworkSyncComponent : IComponent
{
    /// <summary>
    /// Local slot index (0–3) that owns this entity.
    /// 255 indicates server-authoritative (no specific player owns it).
    /// </summary>
    public byte OwnerSlot;

    /// <summary>Sequence number of the last authoritative snapshot applied to this entity.</summary>
    public uint LastSequence;

    /// <summary>
    /// When true the entity's transform and velocity are broadcast to remote peers each frame.
    /// Set false for purely local or static entities.
    /// </summary>
    public bool SyncTransform;

    public static NetworkSyncComponent ForPlayer(byte slot) => new()
    {
        OwnerSlot     = slot,
        LastSequence  = 0,
        SyncTransform = true,
    };
}
