using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using REB.Engine.Audio.Systems;
using REB.Engine.ECS;
using REB.Engine.Input;
using REB.Engine.Physics.Systems;
using REB.Engine.Rendering.Components;
using REB.Engine.Rendering.Systems;
using REB.Engine.Settings;
using REB.Engine.Spatial.Systems;
using REB.Engine.World;
using REB.Engine.World.Systems;

namespace REB.Game;

/// <summary>
/// Root <see cref="Microsoft.Xna.Framework.Game"/> subclass for Royal Errand Boys.
/// Bootstraps the ECS <see cref="World"/>, registers all Phase-1 and Phase-2 systems,
/// and wires the FNA game loop into the ECS update/draw cycle.
/// </summary>
public sealed class RoyalErrandBoysGame : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;
    private World _world = null!;

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

        // ---- Phase 1: Core Engine Services (Story 1.4) ----
        _world.RegisterSystem(new InputSystem());
        _world.RegisterSystem(new AudioSystem());
        _world.RegisterSystem(new SettingsSystem(_graphics));

        // ---- Phase 2: Spatial + Physics (Stories 2.2 & 2.4) ----
        // SpatialSystem builds the octree before PhysicsSystem queries it.
        _world.RegisterSystem(new SpatialSystem(worldSize: 512f));
        _world.RegisterSystem(new PhysicsSystem());

        // ---- Phase 2: Lighting (Story 2.3) ----
        _world.RegisterSystem(new LightingSystem());

        // ---- Phase 2: Performance overlay (Story 2.4) ----
        _world.RegisterSystem(new PerformanceOverlaySystem());

        // ---- Phase 1: 3D Rendering (Story 1.3) ----
        // RunAfter attributes on RenderSystem ensure it executes after physics & lighting.
        _world.RegisterSystem(new RenderSystem(GraphicsDevice));

        // ---- Phase 2: Procedural floor generation (Story 2.1) ----
        // Seed 1 → deterministic first-run layout. Randomise per-run in Epic 3.
        _world.RegisterSystem(new ProceduralFloorGeneratorSystem(
            seed:       1,
            theme:      FloorTheme.Dungeon,
            gridWidth:  48,
            gridHeight: 48));

        // Default scene entities.
        SpawnDefaultCamera();
        SpawnDefaultLighting();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        // Assets loaded per-scene in later epics.
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

        // Positioned for a top-down overview of the generated 48×48 tile floor
        // (world extent ≈ 96×96 units with TileSize=2).
        _world.AddComponent(camera, new TransformComponent
        {
            Position    = new Vector3(48f, 40f, 48f),
            Rotation    = Quaternion.CreateFromAxisAngle(Vector3.Right, -MathHelper.PiOver4),
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
}
