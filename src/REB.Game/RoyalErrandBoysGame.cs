using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using REB.Engine.Audio.Systems;
using REB.Engine.ECS;
using REB.Engine.Input;
using REB.Engine.Rendering.Components;
using REB.Engine.Rendering.Systems;
using REB.Engine.Settings;

namespace REB.Game;

/// <summary>
/// Root <see cref="Microsoft.Xna.Framework.Game"/> subclass for Royal Errand Boys.
/// Bootstraps the ECS <see cref="World"/>, registers all Phase-1 systems, and wires
/// the FNA game loop into the ECS update/draw cycle.
/// </summary>
public sealed class RoyalErrandBoysGame : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;
    private World _world = null!;

    public RoyalErrandBoysGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth  = 1920,
            PreferredBackBufferHeight = 1080,
            IsFullScreen              = false,
            SynchronizeWithVerticalRetrace = true,
        };

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "Royal Errand Boys";
    }

    // -------------------------------------------------------------------------
    //  Initialization
    // -------------------------------------------------------------------------

    protected override void Initialize()
    {
        _world = new World();

        // Story 1.4 — Core Engine Services
        _world.RegisterSystem(new InputSystem());
        _world.RegisterSystem(new AudioSystem());
        _world.RegisterSystem(new SettingsSystem(_graphics));

        // Story 1.3 — 3D Rendering System
        _world.RegisterSystem(new RenderSystem(GraphicsDevice));

        // Spawn a default camera so the render system has something to work with.
        SpawnDefaultCamera();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        // Assets are loaded per-scene in later epics.
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
    //  Helpers
    // -------------------------------------------------------------------------

    private void SpawnDefaultCamera()
    {
        var camera = _world.CreateEntity();

        _world.AddComponent(camera, new TransformComponent
        {
            Position    = new Vector3(0f, 5f, 10f),
            Rotation    = Quaternion.Identity,
            Scale       = Vector3.One,
            WorldMatrix = Matrix.Identity,
        });

        _world.AddComponent(camera, CameraComponent.Default);
        _world.AddTag(camera, "MainCamera");
    }
}
