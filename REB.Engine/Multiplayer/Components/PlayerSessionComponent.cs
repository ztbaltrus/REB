using REB.Engine.ECS;

namespace REB.Engine.Multiplayer.Components;

/// <summary>
/// Per-player session metadata: slot index, authority role, lobby ready state.
/// Managed by <see cref="REB.Engine.Multiplayer.Systems.SessionManagerSystem"/>
/// and <see cref="REB.Engine.Multiplayer.Systems.LobbySystem"/>.
/// </summary>
public struct PlayerSessionComponent : IComponent
{
    /// <summary>Local slot index 0–3, matching the gamepad or keyboard slot.</summary>
    public byte SlotIndex;

    /// <summary>Whether this player is the session host or a client.</summary>
    public SessionRole SessionRole;

    /// <summary>True once the player has confirmed ready in the lobby.</summary>
    public bool IsReady;

    /// <summary>True while the player is actively connected to the session.</summary>
    public bool IsConnected;

    /// <summary>Display name shown in the lobby UI, e.g. "Player 1".</summary>
    public string DisplayName;

    public static PlayerSessionComponent ForSlot(byte slot,
        SessionRole role = SessionRole.Client) => new()
    {
        SlotIndex   = slot,
        SessionRole = role,
        IsReady     = false,
        IsConnected = true,
        DisplayName = $"Player {slot + 1}",
    };
}
