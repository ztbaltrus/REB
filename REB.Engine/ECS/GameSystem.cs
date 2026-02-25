using Microsoft.Xna.Framework;

namespace REB.Engine.ECS;

/// <summary>
/// Base class for all ECS systems. All game logic lives here; components are pure data.
/// Register systems with <see cref="World.RegisterSystem"/> and express ordering with
/// <see cref="RunAfterAttribute"/>.
/// </summary>
public abstract class GameSystem
{
    private World? _world;

    /// <summary>The world this system belongs to. Throws if accessed before initialization.</summary>
    protected World World =>
        _world ?? throw new InvalidOperationException($"{GetType().Name} has not been initialized.");

    // Called by World after adding the system.
    internal void Initialize(World world)
    {
        _world = world;
        OnInitialize();
    }

    /// <summary>Override to run one-time setup logic after the system is registered.</summary>
    protected virtual void OnInitialize() { }

    /// <summary>Called every frame during the update pass.</summary>
    public virtual void Update(float deltaTime) { }

    /// <summary>Called every frame during the draw pass, after all Update calls complete.</summary>
    public virtual void Draw(GameTime gameTime) { }

    /// <summary>Called when the world is disposed. Release unmanaged resources here.</summary>
    public virtual void OnShutdown() { }
}
