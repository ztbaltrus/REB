using REB.Engine.ECS;

namespace REB.Engine.UI.Components;

/// <summary>
/// Lightweight particle emitter descriptor.
/// Attach to any world entity; <see cref="Systems.ParticleSystem"/> ticks it down.
/// Destroy the entity (or set IsActive = false) to stop emission early.
/// </summary>
public struct ParticleEmitterComponent : IComponent
{
    /// <summary>When false the system skips this emitter entirely.</summary>
    public bool IsActive;

    /// <summary>Number of particles spawned per second.</summary>
    public int ParticleCount;

    /// <summary>Seconds until the emitter auto-deactivates (counts down).</summary>
    public float LifeRemaining;

    /// <summary>
    /// Logical tag used by the renderer to choose the correct particle sprite / shader.
    /// Examples: "hit_spark", "boss_explosion", "gold_pickup".
    /// </summary>
    public string EmitterTag;

    public static ParticleEmitterComponent Default => new()
    {
        IsActive      = true,
        ParticleCount = 10,
        LifeRemaining = 1.0f,
        EmitterTag    = string.Empty,
    };
}
