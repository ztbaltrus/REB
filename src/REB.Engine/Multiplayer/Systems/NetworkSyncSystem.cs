using REB.Engine.ECS;
using REB.Engine.Multiplayer.Components;

namespace REB.Engine.Multiplayer.Systems;

/// <summary>
/// Replicates networked entity state between peers.
/// <para>
/// <b>Phase 2 (local co-op):</b> all players share one in-process <see cref="World"/>;
/// this system is a no-op and exists only to mark the integration point for Phase 3.
/// </para>
/// <para>
/// <b>Phase 3+ (online):</b> iterate entities with <see cref="NetworkSyncComponent"/>
/// where <c>SyncTransform == true</c>, serialise <c>TransformComponent</c> and
/// <c>RigidBodyComponent</c>, and transmit/receive over the chosen transport (e.g. ENet).
/// Apply received snapshots with interpolation to remote-owned entities
/// (<c>OwnerSlot != localSlot</c>).
/// </para>
/// </summary>
[RunAfter(typeof(SessionManagerSystem))]
[RunAfter(typeof(LobbySystem))]
public sealed class NetworkSyncSystem : GameSystem
{
    public override void Update(float deltaTime)
    {
        // Phase 2: local co-op — nothing to transmit.
        // Phase 3+: serialise and send/receive here.
    }
}
