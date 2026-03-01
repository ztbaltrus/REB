using REB.Engine.ECS;

namespace REB.Engine.Loot.Components;

/// <summary>
/// Marks a loot item entity as visually highlighted through walls for a limited duration.
/// Added by the Treasurer role ability; ticked down by
/// <see cref="REB.Engine.Player.Systems.RoleAbilitySystem"/>.
/// The <see cref="REB.Engine.UI.Systems.UIRenderSystem"/> draws a sphere marker
/// at each highlighted item's world position via DebugDraw.
/// </summary>
public struct LootHighlightComponent : IComponent
{
    /// <summary>Seconds remaining before the highlight expires. ≤ 0 = inactive.</summary>
    public float TimeRemaining;
}
