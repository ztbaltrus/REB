using REB.Engine.ECS;
using REB.Engine.UI.Components;

namespace REB.Engine.UI.Systems;

/// <summary>
/// Manages <see cref="ParticleEmitterComponent"/> lifetimes.
/// <para>
/// Each frame the system decrements <c>LifeRemaining</c> on all active emitters.
/// When <c>LifeRemaining</c> reaches zero the emitter is deactivated
/// (<c>IsActive = false</c>). The entity is <em>not</em> destroyed automatically;
/// callers may reuse or destroy it.
/// </para>
/// </summary>
public sealed class ParticleSystem : GameSystem
{
    /// <summary>Number of emitters deactivated (expired) this frame.</summary>
    public int ExpiredThisFrame { get; private set; }

    public override void Update(float deltaTime)
    {
        ExpiredThisFrame = 0;

        foreach (var e in World.Query<ParticleEmitterComponent>())
        {
            ref var emitter = ref World.GetComponent<ParticleEmitterComponent>(e);

            if (!emitter.IsActive) continue;

            emitter.LifeRemaining -= deltaTime;

            if (emitter.LifeRemaining <= 0f)
            {
                emitter.IsActive      = false;
                emitter.LifeRemaining = 0f;
                ExpiredThisFrame++;
            }
        }
    }
}
