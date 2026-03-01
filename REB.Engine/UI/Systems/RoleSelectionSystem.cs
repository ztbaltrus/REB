using REB.Engine.ECS;
using REB.Engine.Player;
using REB.Engine.UI.Components;

namespace REB.Engine.UI.Systems;

/// <summary>
/// Validates role selections during the pre-run lobby screen.
/// <para>
/// Reads all entities tagged "PlayerSlot" that carry a
/// <see cref="RoleSelectionComponent"/>. Sets <see cref="AllPlayersReady"/> when
/// every slot has locked in a unique, non-<see cref="PlayerRole.None"/> role.
/// </para>
/// </summary>
public sealed class RoleSelectionSystem : GameSystem
{
    /// <summary>True when every connected player is ready with a unique role.</summary>
    public bool AllPlayersReady   { get; private set; }

    /// <summary>True when two or more players have chosen the same role.</summary>
    public bool HasDuplicateRoles { get; private set; }

    public override void Update(float deltaTime)
    {
        var  roles = new List<PlayerRole>();
        int  total = 0;
        int  ready = 0;

        foreach (var e in World.GetEntitiesWithTag("PlayerSlot"))
        {
            if (!World.HasComponent<RoleSelectionComponent>(e)) continue;

            var rs = World.GetComponent<RoleSelectionComponent>(e);
            total++;

            if (rs.IsReady) ready++;

            if (rs.SelectedRole != PlayerRole.None)
                roles.Add(rs.SelectedRole);
        }

        HasDuplicateRoles = roles.Count != roles.Distinct().Count();
        AllPlayersReady   = total > 0 && ready == total && !HasDuplicateRoles;
    }
}
