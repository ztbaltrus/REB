using Microsoft.Xna.Framework;
using REB.Engine.ECS;
using REB.Engine.Multiplayer;
using REB.Engine.Multiplayer.Components;
using REB.Engine.Multiplayer.Systems;
using REB.Engine.Player;
using REB.Engine.Player.Components;
using Xunit;

namespace REB.Tests.Multiplayer;

// ---------------------------------------------------------------------------
//  LobbySystem + SessionManagerSystem tests
//
//  InputSystem polls real hardware (SDL3) and is therefore NOT registered here.
//  SessionManagerSystem and LobbySystem use TryGetSystem<InputSystem> so they
//  gracefully skip input reading when InputSystem is absent; phase transitions
//  driven by component state (IsReady) still work correctly.
// ---------------------------------------------------------------------------

public sealed class LobbySystemTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static (World world, SessionManagerSystem session, LobbySystem lobby)
        BuildWorld()
    {
        var world   = new World();
        var session = new SessionManagerSystem();
        var lobby   = new LobbySystem();
        var netSync = new NetworkSyncSystem();

        world.RegisterSystem(session);
        world.RegisterSystem(lobby);
        world.RegisterSystem(netSync);
        return (world, session, lobby);
    }

    // -------------------------------------------------------------------------
    //  SessionManagerSystem — JoinPlayer
    // -------------------------------------------------------------------------

    [Fact]
    public void JoinPlayer_CreatesEntityWithRequiredComponents()
    {
        var (world, session, _) = BuildWorld();
        var entity = session.JoinPlayer(0);

        Assert.True(world.IsAlive(entity));
        Assert.True(world.HasComponent<CharacterControllerComponent>(entity));
        Assert.True(world.HasComponent<PlayerInputComponent>(entity));
        Assert.True(world.HasComponent<RoleComponent>(entity));
        Assert.True(world.HasComponent<CarryComponent>(entity));
        Assert.True(world.HasComponent<AnimationComponent>(entity));
        Assert.True(world.HasComponent<PlayerSessionComponent>(entity));
        Assert.True(world.HasComponent<NetworkSyncComponent>(entity));
        world.Dispose();
    }

    [Fact]
    public void JoinPlayer_Slot0_IsHost()
    {
        var (world, session, _) = BuildWorld();
        var entity = session.JoinPlayer(0);

        Assert.Equal(SessionRole.Host,
            world.GetComponent<PlayerSessionComponent>(entity).SessionRole);
        world.Dispose();
    }

    [Fact]
    public void JoinPlayer_Slot1_IsClient()
    {
        var (world, session, _) = BuildWorld();
        session.JoinPlayer(0);
        var entity = session.JoinPlayer(1);

        Assert.Equal(SessionRole.Client,
            world.GetComponent<PlayerSessionComponent>(entity).SessionRole);
        world.Dispose();
    }

    [Fact]
    public void JoinPlayer_Duplicate_Throws()
    {
        var (world, session, _) = BuildWorld();
        session.JoinPlayer(0);
        Assert.Throws<InvalidOperationException>(() => session.JoinPlayer(0));
        world.Dispose();
    }

    [Fact]
    public void JoinPlayer_SetsPlayerTag()
    {
        var (world, session, _) = BuildWorld();
        var entity = session.JoinPlayer(0);

        Assert.True(world.HasTag(entity, "Player"));
        Assert.True(world.HasTag(entity, "Player1"));
        world.Dispose();
    }

    [Fact]
    public void JoinPlayer_IsSlotJoined_ReturnsTrue()
    {
        var (world, session, _) = BuildWorld();
        Assert.False(session.IsSlotJoined(0));
        session.JoinPlayer(0);
        Assert.True(session.IsSlotJoined(0));
        world.Dispose();
    }

    [Fact]
    public void JoinPlayer_Keyboard_Slot0_UsesKeyboard()
    {
        var (world, session, _) = BuildWorld();
        var entity = session.JoinPlayer(0);

        Assert.True(world.GetComponent<PlayerInputComponent>(entity).UseKeyboard);
        world.Dispose();
    }

    [Fact]
    public void JoinPlayer_Gamepad_Slot1_UsesGamepad()
    {
        var (world, session, _) = BuildWorld();
        session.JoinPlayer(0);
        var entity = session.JoinPlayer(1);

        var pinput = world.GetComponent<PlayerInputComponent>(entity);
        Assert.False(pinput.UseKeyboard);
        Assert.Equal(PlayerIndex.Two, pinput.GamepadSlot);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  LobbySystem — phase transitions
    //  (InputSystem absent → HandleLobbyInput skipped; AllPlayersReady checked.)
    // -------------------------------------------------------------------------

    [Fact]
    public void InitialPhase_IsLobby()
    {
        var (world, _, lobby) = BuildWorld();
        Assert.Equal(SessionPhase.Lobby, lobby.Phase);
        world.Dispose();
    }

    [Fact]
    public void StartImmediate_SetsInGamePhase()
    {
        var (world, _, lobby) = BuildWorld();
        lobby.StartImmediate();
        Assert.Equal(SessionPhase.InGame, lobby.Phase);
        world.Dispose();
    }

    [Fact]
    public void EndRun_SetsEndOfRunPhase()
    {
        var (world, _, lobby) = BuildWorld();
        lobby.StartImmediate();
        lobby.EndRun();
        Assert.Equal(SessionPhase.EndOfRun, lobby.Phase);
        world.Dispose();
    }

    [Fact]
    public void AllReady_TransitionsToLoading()
    {
        var (world, session, lobby) = BuildWorld();
        var entity = session.JoinPlayer(0);

        ref var sess = ref world.GetComponent<PlayerSessionComponent>(entity);
        sess.IsReady = true;

        world.Update(0.016f);

        Assert.Equal(SessionPhase.Loading, lobby.Phase);
        world.Dispose();
    }

    [Fact]
    public void Loading_TransitionsToInGame_AfterCountdown()
    {
        var (world, session, lobby) = BuildWorld();
        var entity = session.JoinPlayer(0);

        ref var sess = ref world.GetComponent<PlayerSessionComponent>(entity);
        sess.IsReady = true;

        world.Update(0.016f);
        Assert.Equal(SessionPhase.Loading, lobby.Phase);

        // Run well past the 3-second countdown.
        for (int i = 0; i < 200; i++)
            world.Update(0.02f);

        Assert.Equal(SessionPhase.InGame, lobby.Phase);
        world.Dispose();
    }

    [Fact]
    public void NotAllReady_StaysInLobby()
    {
        var (world, session, lobby) = BuildWorld();
        session.JoinPlayer(0);
        session.JoinPlayer(1);
        // Neither player ready.
        world.Update(0.016f);

        Assert.Equal(SessionPhase.Lobby, lobby.Phase);
        world.Dispose();
    }

    [Fact]
    public void TwoPlayers_BothMustBeReady()
    {
        var (world, session, lobby) = BuildWorld();
        var e0 = session.JoinPlayer(0);
        var e1 = session.JoinPlayer(1);

        // Only player 0 ready → still lobby.
        ref var sess0 = ref world.GetComponent<PlayerSessionComponent>(e0);
        sess0.IsReady = true;
        world.Update(0.016f);
        Assert.Equal(SessionPhase.Lobby, lobby.Phase);

        // Player 1 also ready → transitions to Loading.
        ref var sess1 = ref world.GetComponent<PlayerSessionComponent>(e1);
        sess1.IsReady = true;
        world.Update(0.016f);
        Assert.Equal(SessionPhase.Loading, lobby.Phase);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Role and network sync
    // -------------------------------------------------------------------------

    [Fact]
    public void JoinedPlayer_DefaultRole_IsNone()
    {
        var (world, session, _) = BuildWorld();
        var entity = session.JoinPlayer(0);

        Assert.Equal(PlayerRole.None,
            world.GetComponent<RoleComponent>(entity).Role);
        world.Dispose();
    }

    [Fact]
    public void NetworkSync_OwnerSlot_MatchesPlayerSlot()
    {
        var (world, session, _) = BuildWorld();
        session.JoinPlayer(0);
        var e1 = session.JoinPlayer(1);

        Assert.Equal((byte)1,
            world.GetComponent<NetworkSyncComponent>(e1).OwnerSlot);
        world.Dispose();
    }
}
