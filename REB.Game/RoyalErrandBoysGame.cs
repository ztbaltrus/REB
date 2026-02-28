using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using REB.Engine.Audio.Systems;
using REB.Engine.ECS;
using REB.Engine.Input;
using REB.Engine.Loot.Components;
using REB.Engine.Loot.Systems;
using REB.Engine.Multiplayer.Systems;
using REB.Engine.Physics.Systems;
using REB.Engine.Player.Princess.Components;
using REB.Engine.Player.Systems;
using REB.Engine.Rendering.Components;
using REB.Engine.Rendering.Systems;
using REB.Engine.Settings;
using REB.Engine.Spatial.Systems;
using REB.Engine.World;
using REB.Engine.World.Systems;

namespace REB.Game;

/// <summary>
/// Root <see cref="Microsoft.Xna.Framework.Game"/> subclass for Royal Errand Boys.
/// Bootstraps the ECS <see cref="World"/>, registers all systems through Epic 4,
/// and wires the FNA game loop into the ECS update/draw cycle.
/// </summary>
public sealed class RoyalErrandBoysGame : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;
    private World                  _world          = null!;
    private SessionManagerSystem   _sessionManager = null!;

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
        IsMouseVisible         = true;
        Window.Title           = "Royal Errand Boys";
    }

    // -------------------------------------------------------------------------
    //  Initialization
    // -------------------------------------------------------------------------

    protected override void Initialize()
    {
        _world = new World();

        // ── Phase 1: Core Engine Services (Story 1.4) ────────────────────────
        _world.RegisterSystem(new InputSystem());
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

        // Auto-join the keyboard player so gameplay starts immediately.
        _sessionManager.JoinPlayer(0);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        // Per-scene asset loading deferred to later epics.
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
    }
}
