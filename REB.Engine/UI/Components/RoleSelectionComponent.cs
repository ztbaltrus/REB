using REB.Engine.ECS;
using REB.Engine.Player;

namespace REB.Engine.UI.Components;

/// <summary>
/// Per-player role-selection state during the pre-run lobby screen.
/// One entity tagged "PlayerSlot" per connected player carries this component.
/// Processed by <see cref="Systems.RoleSelectionSystem"/>.
/// </summary>
public struct RoleSelectionComponent : IComponent
{
    /// <summary>The co-op role this player has highlighted / chosen.</summary>
    public PlayerRole SelectedRole;

    /// <summary>True when the player has locked in their role choice.</summary>
    public bool IsReady;

    /// <summary>Slot index (0–3) of the owning player.</summary>
    public int PlayerId;

    public static RoleSelectionComponent Default => default;
}
