using Microsoft.Xna.Framework.Input;
using REB.Engine.ECS;
using REB.Engine.Input;
using REB.Engine.Multiplayer.Components;
using REB.Engine.Player;
using REB.Engine.Player.Components;

namespace REB.Engine.Multiplayer.Systems;

/// <summary>
/// Drives the session phase state machine and manages in-lobby interactions:
/// role selection via D-Pad/Q-R keys and ready toggling via Start/Enter.
/// <para>Phase flow: Lobby → Loading (3 s countdown) → InGame → EndOfRun.</para>
/// </summary>
[RunAfter(typeof(SessionManagerSystem))]
public sealed class LobbySystem : GameSystem
{
    private SessionPhase _phase           = SessionPhase.Lobby;
    private float        _countdownTimer  = 0f;
    private const float  CountdownSeconds = 3f;

    /// <summary>The current session phase.</summary>
    public SessionPhase Phase => _phase;

    public override void Update(float deltaTime)
    {
        if (_phase is SessionPhase.InGame or SessionPhase.EndOfRun) return;

        if (_phase == SessionPhase.Lobby)
        {
            if (World.TryGetSystem<InputSystem>(out var input))
                HandleLobbyInput(input);

            if (AllPlayersReady())
            {
                _phase          = SessionPhase.Loading;
                _countdownTimer = CountdownSeconds;
            }
        }

        if (_phase == SessionPhase.Loading)
        {
            _countdownTimer -= deltaTime;
            if (_countdownTimer <= 0f)
                _phase = SessionPhase.InGame;
        }
    }

    // =========================================================================
    //  Public API
    // =========================================================================

    /// <summary>Bypass the lobby and start immediately (useful for solo play / testing).</summary>
    public void StartImmediate() => _phase = SessionPhase.InGame;

    /// <summary>Transition to EndOfRun when the run concludes.</summary>
    public void EndRun() => _phase = SessionPhase.EndOfRun;

    // =========================================================================
    //  Internal
    // =========================================================================

    private void HandleLobbyInput(InputSystem input)
    {
        foreach (var entity in
            World.Query<PlayerSessionComponent, RoleComponent, PlayerInputComponent>())
        {
            ref var session = ref World.GetComponent<PlayerSessionComponent>(entity);
            ref var role    = ref World.GetComponent<RoleComponent>(entity);
            ref var pinput  = ref World.GetComponent<PlayerInputComponent>(entity);

            bool cycleLeft, cycleRight, toggleReady;

            if (pinput.UseKeyboard)
            {
                cycleLeft   = input.IsKeyPressed(Keys.Q);
                cycleRight  = input.IsKeyPressed(Keys.R);
                toggleReady = input.IsKeyPressed(Keys.Enter);
            }
            else
            {
                var pad    = pinput.GamepadSlot;
                cycleLeft   = input.IsButtonPressed(pad, Buttons.DPadLeft);
                cycleRight  = input.IsButtonPressed(pad, Buttons.DPadRight);
                toggleReady = input.IsButtonPressed(pad, Buttons.Start);
            }

            if (cycleLeft)  role.Role = CycleRole(role.Role, -1);
            if (cycleRight) role.Role = CycleRole(role.Role, +1);
            if (toggleReady) session.IsReady = !session.IsReady;
        }
    }

    private bool AllPlayersReady()
    {
        int total = 0, ready = 0;
        foreach (var entity in World.Query<PlayerSessionComponent>())
        {
            var s = World.GetComponent<PlayerSessionComponent>(entity);
            if (!s.IsConnected) continue;
            total++;
            if (s.IsReady) ready++;
        }
        return total > 0 && ready == total;
    }

    private static readonly PlayerRole[] Roles =
    [
        PlayerRole.Carrier,
        PlayerRole.Scout,
        PlayerRole.Treasurer,
        PlayerRole.Negotiator,
    ];

    private static PlayerRole CycleRole(PlayerRole current, int delta)
    {
        int idx = Array.IndexOf(Roles, current);
        if (idx < 0) idx = 0;
        return Roles[(idx + delta + Roles.Length) % Roles.Length];
    }
}
