using REB.Engine.ECS;

namespace REB.Engine.World.Components;

/// <summary>
/// Marks a room entity as visually highlighted for a limited duration.
/// Added by the Scout role ability; ticked down by
/// <see cref="REB.Engine.Player.Systems.RoleAbilitySystem"/>.
/// The <see cref="REB.Engine.UI.Systems.UIRenderSystem"/> draws a coloured
/// outline box around highlighted rooms via DebugDraw.
/// </summary>
public struct RoomHighlightComponent : IComponent
{
    /// <summary>Seconds remaining before the highlight expires. ≤ 0 = inactive.</summary>
    public float TimeRemaining;
}
