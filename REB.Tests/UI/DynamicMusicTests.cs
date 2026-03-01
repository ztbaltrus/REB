using REB.Engine.Boss;
using REB.Engine.Boss.Components;
using REB.Engine.Boss.Systems;
using REB.Engine.Combat.Components;
using REB.Engine.Combat.Systems;
using REB.Engine.ECS;
using REB.Engine.KingsCourt;
using REB.Engine.KingsCourt.Components;
using REB.Engine.Player.Systems;
using REB.Engine.Rendering.Components;
using REB.Engine.Tavern;
using REB.Engine.Tavern.Components;
using REB.Engine.Tavern.Systems;
using REB.Engine.UI;
using REB.Engine.UI.Components;
using REB.Engine.UI.Systems;
using Xunit;

namespace REB.Tests.UI;

// ---------------------------------------------------------------------------
//  DynamicMusicSystem tests
//
//  Each test verifies that the correct track is selected given a particular
//  combination of game-state entities. Priority (highest to lowest):
//    BossEncounter > KingsCourt > Tavern > Combat > Exploration > None
// ---------------------------------------------------------------------------

public sealed class DynamicMusicTests
{
    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static (World world, DynamicMusicSystem music) BuildWorld()
    {
        var world  = new World();
        var combat = new CombatSystem();
        var boss   = new BossSystem();
        var tavern = new TavernSceneSystem();
        var music  = new DynamicMusicSystem();
        world.RegisterSystem(new PlayerControllerSystem());
        world.RegisterSystem(combat);
        world.RegisterSystem(boss);
        world.RegisterSystem(tavern);
        world.RegisterSystem(music);
        return (world, music);
    }

    private static Entity AddPlayer(World world)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "Player");
        return e;
    }

    private static Entity AddOpenTavern(World world)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "Tavern");
        var ts = TavernStateComponent.Default;
        ts.Phase       = TavernPhase.Open;
        ts.SceneActive = true;
        world.AddComponent(e, ts);
        return e;
    }

    private static Entity AddKingInCourt(World world, KingsCourtPhase phase = KingsCourtPhase.Review)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "King");
        var ks = KingStateComponent.Default;
        ks.Phase       = phase;
        ks.SceneActive = true;
        world.AddComponent(e, ks);
        return e;
    }

    private static Entity AddBoss(World world, BossPhase phase = BossPhase.Phase1)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "Boss");
        var boss = BossComponent.Default;
        boss.Phase = phase;
        world.AddComponent(e, boss);
        world.AddComponent(e, HealthComponent.For(200f));
        return e;
    }

    private static Entity AddCombatPair(World world)
    {
        // Attacker.
        var attacker = world.CreateEntity();
        world.AddTag(attacker, "Enemy");
        world.AddComponent(attacker, new TransformComponent { Position = Microsoft.Xna.Framework.Vector3.Zero });
        var dmg = DamageComponent.MeleeDefault;
        dmg.AttackPressed = true;
        dmg.AttackTimer   = 0f;
        dmg.MeleeRange    = 5f;
        world.AddComponent(attacker, dmg);

        // Target within range.
        var target = world.CreateEntity();
        world.AddTag(target, "Player");
        world.AddComponent(target, new TransformComponent
        {
            Position = new Microsoft.Xna.Framework.Vector3(1f, 0f, 0f)
        });
        world.AddComponent(target, HealthComponent.For(100f));

        return attacker;
    }

    // -------------------------------------------------------------------------
    //  No entities → None
    // -------------------------------------------------------------------------

    [Fact]
    public void NoEntities_CurrentTrack_IsNone()
    {
        var (world, music) = BuildWorld();

        world.Update(0.016f);

        Assert.Equal(MusicTrack.None, music.CurrentTrack);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Exploration when player present
    // -------------------------------------------------------------------------

    [Fact]
    public void PlayerPresent_CurrentTrack_IsExploration()
    {
        var (world, music) = BuildWorld();
        AddPlayer(world);

        world.Update(0.016f);

        Assert.Equal(MusicTrack.Exploration, music.CurrentTrack);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Combat overrides Exploration
    // -------------------------------------------------------------------------

    [Fact]
    public void CombatOccurring_CurrentTrack_IsCombat()
    {
        var (world, music) = BuildWorld();
        AddCombatPair(world);

        world.Update(0.016f);

        Assert.Equal(MusicTrack.Combat, music.CurrentTrack);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Tavern overrides Combat
    // -------------------------------------------------------------------------

    [Fact]
    public void TavernOpen_CurrentTrack_IsTavern()
    {
        var (world, music) = BuildWorld();
        AddPlayer(world);
        AddOpenTavern(world);

        world.Update(0.016f);

        Assert.Equal(MusicTrack.Tavern, music.CurrentTrack);
        world.Dispose();
    }

    [Fact]
    public void TavernOpen_OverridesCombat()
    {
        var (world, music) = BuildWorld();
        AddCombatPair(world);
        AddOpenTavern(world);

        world.Update(0.016f);

        Assert.Equal(MusicTrack.Tavern, music.CurrentTrack);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  KingsCourt overrides Tavern
    // -------------------------------------------------------------------------

    [Fact]
    public void KingsCourtActive_CurrentTrack_IsKingsCourt()
    {
        var (world, music) = BuildWorld();
        AddPlayer(world);
        AddKingInCourt(world, KingsCourtPhase.Review);

        world.Update(0.016f);

        Assert.Equal(MusicTrack.KingsCourt, music.CurrentTrack);
        world.Dispose();
    }

    [Fact]
    public void KingsCourt_OverridesTavern()
    {
        var (world, music) = BuildWorld();
        AddPlayer(world);
        AddOpenTavern(world);
        AddKingInCourt(world, KingsCourtPhase.Negotiation);

        world.Update(0.016f);

        Assert.Equal(MusicTrack.KingsCourt, music.CurrentTrack);
        world.Dispose();
    }

    [Fact]
    public void KingInactivePhase_DoesNotTriggerKingsCourt()
    {
        var (world, music) = BuildWorld();
        AddPlayer(world);
        AddKingInCourt(world, KingsCourtPhase.Inactive);

        world.Update(0.016f);

        Assert.Equal(MusicTrack.Exploration, music.CurrentTrack);
        world.Dispose();
    }

    [Fact]
    public void KingDismissedPhase_DoesNotTriggerKingsCourt()
    {
        var (world, music) = BuildWorld();
        AddPlayer(world);
        AddKingInCourt(world, KingsCourtPhase.Dismissed);

        world.Update(0.016f);

        Assert.Equal(MusicTrack.Exploration, music.CurrentTrack);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Boss overrides everything
    // -------------------------------------------------------------------------

    [Fact]
    public void ActiveBoss_CurrentTrack_IsBossEncounter()
    {
        var (world, music) = BuildWorld();
        AddPlayer(world);
        AddBoss(world, BossPhase.Phase1);

        world.Update(0.016f);

        Assert.Equal(MusicTrack.BossEncounter, music.CurrentTrack);
        world.Dispose();
    }

    [Fact]
    public void ActiveBoss_OverridesKingsCourt()
    {
        var (world, music) = BuildWorld();
        AddPlayer(world);
        AddKingInCourt(world, KingsCourtPhase.Review);
        AddBoss(world, BossPhase.Phase2);

        world.Update(0.016f);

        Assert.Equal(MusicTrack.BossEncounter, music.CurrentTrack);
        world.Dispose();
    }

    [Fact]
    public void DefeatedBoss_DoesNotTriggerBossEncounter()
    {
        var (world, music) = BuildWorld();
        AddPlayer(world);
        AddBoss(world, BossPhase.Defeated);

        world.Update(0.016f);

        Assert.Equal(MusicTrack.Exploration, music.CurrentTrack);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  AudioTriggerEvent fired on track change
    // -------------------------------------------------------------------------

    [Fact]
    public void AudioEvent_Fired_WhenTrackChanges()
    {
        var (world, music) = BuildWorld();
        AddPlayer(world);           // triggers Exploration on first update

        world.Update(0.016f);

        Assert.NotEmpty(music.AudioEvents);
        world.Dispose();
    }

    [Fact]
    public void AudioEvent_HasCorrectTrack()
    {
        var (world, music) = BuildWorld();
        AddPlayer(world);

        world.Update(0.016f);

        Assert.Contains(music.AudioEvents, ev => ev.Track == MusicTrack.Exploration);
        world.Dispose();
    }

    [Fact]
    public void AudioEvent_NotFired_WhenTrackUnchanged()
    {
        var (world, music) = BuildWorld();
        AddPlayer(world);

        world.Update(0.016f);   // None → Exploration (event fires)
        world.Update(0.016f);   // still Exploration (no event)

        Assert.Empty(music.AudioEvents);
        world.Dispose();
    }
}
