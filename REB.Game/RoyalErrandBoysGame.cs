using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using REB.Engine.Audio.Systems;
using REB.Engine.UI;
using REB.Engine.Boss.Systems;
using REB.Engine.Combat.Components;
using REB.Engine.Combat.Systems;
using REB.Engine.KingsCourt.Components;
using REB.Engine.KingsCourt.Systems;
using REB.Engine.QA.Systems;
using REB.Engine.RunManagement.Systems;
using REB.Engine.Tavern.Components;
using REB.Engine.Tavern.Systems;
using REB.Engine.ECS;
using REB.Engine.Enemy.Systems;
using REB.Engine.Hazards.Systems;
using REB.Engine.Input;
using REB.Engine.Loot.Components;
using REB.Engine.Loot.Systems;
using REB.Engine.Multiplayer.Systems;
using REB.Engine.Physics.Components;
using REB.Engine.Physics.Systems;
using REB.Engine.Player.Princess.Components;
using REB.Engine.Player.Princess.Systems;
using REB.Engine.Player.Systems;
using REB.Engine.Rendering.Components;
using REB.Engine.Rendering.Systems;
using REB.Engine.Settings;
using REB.Engine.Spatial.Systems;
using REB.Engine.UI.Components;
using REB.Engine.UI.Systems;
using REB.Engine.World;
using REB.Engine.World.Systems;

namespace REB.Game;

/// <summary>
/// Root <see cref="Microsoft.Xna.Framework.Game"/> subclass for Royal Errand Boys.
/// Bootstraps the ECS <see cref="World"/>, registers all systems through Epic 9,
/// and wires the FNA game loop into the ECS update/draw cycle.
/// </summary>
public sealed class RoyalErrandBoysGame : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;
    private World                  _world          = null!;
    private SessionManagerSystem   _sessionManager = null!;
    private UIRenderSystem         _uiRender       = null!;
    private MusicPlaybackSystem    _musicPlayback  = null!;
    private DialogueSubtitleSystem _dialogueSubs   = null!;

    public RoyalErrandBoysGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth       = 1920,
            PreferredBackBufferHeight      = 1080,
            IsFullScreen                   = false,
            SynchronizeWithVerticalRetrace = true,
        };

        Content.RootDirectory = "Content";
        IsMouseVisible         = false;
        Window.Title           = "Royal Errand Boys";
    }

    // -------------------------------------------------------------------------
    //  Initialization
    // -------------------------------------------------------------------------

    protected override void Initialize()
    {
        _world = new World();

        // ── Phase 1: Core Engine Services (Story 1.4) ────────────────────────
        _world.RegisterSystem(new InputSystem(Window));
        _world.RegisterSystem(new AudioSystem());
        _world.RegisterSystem(new SettingsSystem(_graphics));

        // ── Phase 2: Spatial + Physics (Stories 2.2 & 2.4) ──────────────────
        _world.RegisterSystem(new SpatialSystem(worldSize: 512f));
        _world.RegisterSystem(new PhysicsSystem());

        // ── Phase 2: Lighting (Story 2.3) ────────────────────────────────────
        _world.RegisterSystem(new LightingSystem());

        // ── Phase 3: Multiplayer session (Stories 3.2 & 3.3) ─────────────────
        _sessionManager = new SessionManagerSystem();
        _world.RegisterSystem(_sessionManager);
        _world.RegisterSystem(new LobbySystem());
        _world.RegisterSystem(new NetworkSyncSystem());

        // ── Phase 3: Player systems (Stories 3.1, 3.3 & 3.4) ─────────────────
        _world.RegisterSystem(new PlayerControllerSystem());
        _world.RegisterSystem(new AnimationSystem());
        _world.RegisterSystem(new RoleAbilitySystem());
        _world.RegisterSystem(new CarrySystem());

        // ── Epic 5: Princess AI & Behavior (Stories 5.1 – 5.3) ───────────────
        _world.RegisterSystem(new TraitBehaviorSystem());
        _world.RegisterSystem(new MoodSystem());
        _world.RegisterSystem(new MoodReactionSystem());
        _world.RegisterSystem(new PrincessAISystem());

        // ── Epic 6: Enemies, Hazards & Combat (Stories 6.1 – 6.4) ────────────
        _world.RegisterSystem(new AggroSystem());
        _world.RegisterSystem(new EnemyAISystem());
        _world.RegisterSystem(new CombatSystem());
        _world.RegisterSystem(new HitReactionSystem());
        _world.RegisterSystem(new DeathSystem());
        _world.RegisterSystem(new TrapTriggerSystem());
        _world.RegisterSystem(new BossSystem());

        // ── Epic 7: King's Court & Reward System (Stories 7.1 – 7.4) ─────────
        _world.RegisterSystem(new KingsCourtSceneSystem());
        _world.RegisterSystem(new NegotiationMinigameSystem());
        _world.RegisterSystem(new PayoutCalculationSystem());
        _world.RegisterSystem(new KingRelationshipSystem());

        // ── Epic 8: Tavern & Upgrade Systems (Stories 8.1 – 8.4) ─────────────
        _world.RegisterSystem(new GoldCurrencySystem());
        _world.RegisterSystem(new TavernSceneSystem());
        _world.RegisterSystem(new UpgradeTreeSystem());
        _world.RegisterSystem(new GearUpgradeSystem());
        _world.RegisterSystem(new TavernkeeperSystem());
        _world.RegisterSystem(new SerializationSystem());

        // ── Epic 10: QA, Polish & Ship Readiness (Stories 10.1 – 10.4) ───────
        // RunManagerSystem: drives procedural per-run generation (masterSeed=0 → random).
        _world.RegisterSystem(new RunManagerSystem(masterSeed: 0));
        _world.RegisterSystem(new InvariantCheckerSystem());
        _world.RegisterSystem(new PerformanceProfilerSystem());

        // ── Epic 9: UI, HUD & Game Feel (Stories 9.1 – 9.4) ─────────────────
        _world.RegisterSystem(new HUDSystem());
        _world.RegisterSystem(new MenuSystem());
        _world.RegisterSystem(new RoleSelectionSystem());
        _world.RegisterSystem(new RunSummaryUISystem());
        _world.RegisterSystem(new DynamicMusicSystem());
        _world.RegisterSystem(new SpatialAudioSystem());
        _world.RegisterSystem(new ParticleSystem());
        _world.RegisterSystem(new ScreenShakeSystem());
        _world.RegisterSystem(new HitFeedbackSystem());

        // ── Rendering & Playback (UIRenderSystem, Music, Subtitles) ──────────
        _uiRender      = new UIRenderSystem(GraphicsDevice);
        _musicPlayback = new MusicPlaybackSystem();
        _dialogueSubs  = new DialogueSubtitleSystem();
        _world.RegisterSystem(_uiRender);
        _world.RegisterSystem(_musicPlayback);
        _world.RegisterSystem(_dialogueSubs);

        // ── Epic 4: Loot & Inventory (Stories 4.1 – 4.4) ─────────────────────
        _world.RegisterSystem(new LootSpawnSystem(seed: 1, floorDifficulty: 1));
        _world.RegisterSystem(new PickupInteractionSystem());
        _world.RegisterSystem(new InventorySystem());
        _world.RegisterSystem(new LootValuationSystem());
        _world.RegisterSystem(new UseItemSystem());

        // ── Phase 2: Performance overlay (Story 2.4) ─────────────────────────
        _world.RegisterSystem(new PerformanceOverlaySystem());

        // ── Phase 1: 3D Rendering (Story 1.3) ────────────────────────────────
        _world.RegisterSystem(new RenderSystem(GraphicsDevice));

        // ── Phase 2: Procedural floor generation (Story 2.1) ─────────────────
        // Seed 1 → deterministic first-run layout; randomise per-run in Epic 4.
        _world.RegisterSystem(new ProceduralFloorGeneratorSystem(
            seed:       1,
            theme:      FloorTheme.Dungeon,
            gridWidth:  48,
            gridHeight: 48));

        // ── Scene entities ────────────────────────────────────────────────────
        SpawnDefaultCamera();
        SpawnDefaultLighting();
        SpawnPrincess();
        SpawnTreasureLedger();
        SpawnKing();
        SpawnRunSummary();
        SpawnGoldLedger();
        SpawnTavern();
        SpawnTavernkeeper();
        SpawnHUDData();
        SpawnMenuManager();
        SpawnRunSummaryUI();
        SpawnDynamicMusic();
        SpawnAudioMixer();
        SpawnScreenShake();

        // Auto-join the keyboard player so gameplay starts immediately.
        _sessionManager.JoinPlayer(0);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        // Load fonts — game runs without crash if .xnb files aren't compiled yet.
        TryLoad(() =>
        {
            var hudFont  = Content.Load<SpriteFont>("Fonts/HudFont");
            var dlgFont  = Content.Load<SpriteFont>("Fonts/DialogueFont");
            var ttlFont  = Content.Load<SpriteFont>("Fonts/TitleFont");
            _uiRender.LoadFonts(hudFont, dlgFont, ttlFont);
            _dialogueSubs.LoadFont(dlgFont, GraphicsDevice);
        });

        // Load music — silently skipped if audio files aren't present.
        TryLoad(() =>
        {
            var songs = new Dictionary<MusicTrack, Song>
            {
                [MusicTrack.MainMenu]     = Content.Load<Song>("Audio/Music/main_menu"),
                [MusicTrack.Exploration]  = Content.Load<Song>("Audio/Music/exploration"),
                [MusicTrack.Combat]       = Content.Load<Song>("Audio/Music/combat"),
                [MusicTrack.BossEncounter]= Content.Load<Song>("Audio/Music/boss_encounter"),
                [MusicTrack.KingsCourt]   = Content.Load<Song>("Audio/Music/kings_court"),
                [MusicTrack.Tavern]       = Content.Load<Song>("Audio/Music/tavern"),
            };
            _musicPlayback.LoadSongs(songs);
        });
    }

    private static void TryLoad(Action load)
    {
        try { load(); }
        catch { /* asset not yet compiled — skip gracefully */ }
    }

    // -------------------------------------------------------------------------
    //  Game loop
    // -------------------------------------------------------------------------

    protected override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _world.Update(dt);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _world.Draw(gameTime);
        base.Draw(gameTime);
    }

    // -------------------------------------------------------------------------
    //  Cleanup
    // -------------------------------------------------------------------------

    protected override void UnloadContent()
    {
        _world.Dispose();
        base.UnloadContent();
    }

    // -------------------------------------------------------------------------
    //  Scene setup helpers
    // -------------------------------------------------------------------------

    private void SpawnDefaultCamera()
    {
        var camera = _world.CreateEntity();

        // Initial position; PlayerControllerSystem repositions this each frame
        // relative to Player1's location once the player entity exists.
        _world.AddComponent(camera, new TransformComponent
        {
            Position    = new Vector3(0f, 5f, -10f),
            Rotation    = Quaternion.Identity,
            Scale       = Vector3.One,
            WorldMatrix = Matrix.Identity,
        });

        _world.AddComponent(camera, CameraComponent.Default);
        _world.AddTag(camera, "MainCamera");
    }

    private void SpawnDefaultLighting()
    {
        // Ambient fill — dim dungeon atmosphere.
        var ambient = _world.CreateEntity();
        _world.AddComponent(ambient, LightComponent.DefaultAmbient);

        // Primary directional key light (cracks-in-the-ceiling sunlight).
        var sun = _world.CreateEntity();
        _world.AddComponent(sun, LightComponent.DefaultDirectional);
        _world.AddComponent(sun, new TransformComponent
        {
            Position    = Vector3.Zero,
            Rotation    = Quaternion.CreateFromAxisAngle(
                              Vector3.Right, MathHelper.ToRadians(-55f)),
            Scale       = Vector3.One,
            WorldMatrix = Matrix.Identity,
        });
    }

    private void SpawnTreasureLedger()
    {
        var ledger = _world.CreateEntity();
        _world.AddTag(ledger, "TreasureLedger");
        _world.AddComponent(ledger, TreasureLedgerComponent.Default);
    }

    private void SpawnPrincess()
    {
        var princess = _world.CreateEntity();
        _world.AddTag(princess, "Princess");

        // Placed near the centre of the floor grid until the ProceduralFloorGeneratorSystem
        // has run and the PrincessChamber spawn point is known (Epic 4 refinement).
        _world.AddComponent(princess, new TransformComponent
        {
            Position    = new Vector3(48f, 0.5f, 48f),
            Rotation    = Quaternion.Identity,
            Scale       = Vector3.One,
            WorldMatrix = Matrix.Identity,
        });

        _world.AddComponent(princess, PrincessStateComponent.Default);

        // ── Epic 5: personality, goodwill, and AI navigation ─────────────────
        // Seed 1 gives a deterministic trait on the first run; swap for a random
        // per-run seed once the save system is in place (Epic 8).
        _world.AddComponent(princess, PrincessTraitComponent.Random(seed: 1));
        _world.AddComponent(princess, PrincessGoodwillComponent.Default);
        _world.AddComponent(princess, NavAgentComponent.Default);

        // Physics body for AI-driven movement and drop impact.
        _world.AddComponent(princess, new RigidBodyComponent
        {
            Velocity    = Vector3.Zero,
            Mass        = 60f,
            UseGravity  = true,
            LinearDrag  = 5f,   // high drag so she stops quickly under AI control
            IsKinematic = false,
        });

        // ── Epic 6: health and hit-reaction so hazards/enemies can affect her ──
        _world.AddComponent(princess, HealthComponent.For(100f));
        _world.AddComponent(princess, HitReactionComponent.Default);
    }

    private void SpawnKing()
    {
        var king = _world.CreateEntity();
        _world.AddTag(king, "King");
        _world.AddComponent(king, KingStateComponent.Default);
        _world.AddComponent(king, DialogueChoiceComponent.Default);
        _world.AddComponent(king, KingDispositionComponent.Default);
        _world.AddComponent(king, KingRelationshipComponent.Default);
    }

    private void SpawnRunSummary()
    {
        var summary = _world.CreateEntity();
        _world.AddTag(summary, "RunSummary");
        // IsComplete starts false; LootValuationSystem / run-end logic sets it
        // true at end-of-run to trigger the King's Court scene (Epic 7).
        _world.AddComponent(summary, new RunSummaryComponent());
    }

    private void SpawnGoldLedger()
    {
        var ledger = _world.CreateEntity();
        _world.AddTag(ledger, "GoldLedger");
        _world.AddComponent(ledger, GoldCurrencyComponent.Default);
        _world.AddComponent(ledger, UpgradeTreeComponent.Default);
    }

    private void SpawnTavern()
    {
        var tavern = _world.CreateEntity();
        _world.AddTag(tavern, "Tavern");
        _world.AddComponent(tavern, TavernStateComponent.Default);
    }

    private void SpawnTavernkeeper()
    {
        var tk = _world.CreateEntity();
        _world.AddTag(tk, "Tavernkeeper");
        _world.AddComponent(tk, TavernkeeperNPCComponent.Default);
    }

    // ── Epic 9: UI, HUD & Game Feel ──────────────────────────────────────────

    private void SpawnHUDData()
    {
        var e = _world.CreateEntity();
        _world.AddTag(e, "HUDData");
        _world.AddComponent(e, HUDDataComponent.Default);
    }

    private void SpawnMenuManager()
    {
        var e = _world.CreateEntity();
        _world.AddTag(e, "MenuManager");
        _world.AddComponent(e, MenuManagerComponent.Default);
    }

    private void SpawnRunSummaryUI()
    {
        var e = _world.CreateEntity();
        _world.AddTag(e, "RunSummaryUI");
        _world.AddComponent(e, RunSummaryUIComponent.Default);
    }

    private void SpawnDynamicMusic()
    {
        var e = _world.CreateEntity();
        _world.AddTag(e, "DynamicMusic");
        _world.AddComponent(e, DynamicMusicComponent.Default);
    }

    private void SpawnAudioMixer()
    {
        var e = _world.CreateEntity();
        _world.AddTag(e, "AudioMixer");
        _world.AddComponent(e, AudioMixerComponent.Default);
    }

    private void SpawnScreenShake()
    {
        var e = _world.CreateEntity();
        _world.AddTag(e, "ScreenShake");
        _world.AddComponent(e, ScreenShakeComponent.Default);
    }
}
