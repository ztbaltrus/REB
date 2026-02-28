using Microsoft.Xna.Framework;
using REB.Engine.ECS;
using REB.Engine.Player.Princess.Components;

namespace REB.Engine.Player.Princess.Systems;

/// <summary>
/// Tracks the princess's goodwill (trust) score and updates it each frame based
/// on how she is being treated.
/// <para>
/// Goodwill rules (points per second unless noted):
/// <list type="bullet">
///   <item>Passive decay    — always −0.2 /s</item>
///   <item>Calm carry gain  — +1.0 /s while carried and MoodLevel == Calm</item>
///   <item>Struggle drain   — −3.0 /s during a struggle burst</item>
///   <item>Drop penalty     — instant −5 when princess transitions from carried to not-carried</item>
/// </list>
/// </para>
/// </summary>
[RunAfter(typeof(TraitBehaviorSystem))]
public sealed class MoodSystem : GameSystem
{
    private const float PassiveDecayRate  = 0.2f;
    private const float CalmCarryGain     = 1.0f;
    private const float StruggleDrainRate = 3.0f;
    private const float DropPenalty       = 5.0f;

    // Tracks the carry state from the previous frame to detect drops.
    private bool _wasCarriedLastFrame;

    public override void Update(float deltaTime)
    {
        Entity princess = FindPrincess();
        if (!World.IsAlive(princess)) return;
        if (!World.HasComponent<PrincessStateComponent>(princess)) return;
        if (!World.HasComponent<PrincessGoodwillComponent>(princess)) return;

        ref var ps = ref World.GetComponent<PrincessStateComponent>(princess);
        ref var gw = ref World.GetComponent<PrincessGoodwillComponent>(princess);

        // Drop detection: was carried last frame and is now free.
        if (_wasCarriedLastFrame && !ps.IsBeingCarried)
            gw.Goodwill -= DropPenalty;

        // Passive decay — always running.
        gw.Goodwill -= PassiveDecayRate * deltaTime;

        // Carry-based adjustments.
        if (ps.IsBeingCarried)
        {
            if (ps.IsStruggling)
                gw.Goodwill -= StruggleDrainRate * deltaTime;
            else if (ps.MoodLevel == PrincessMoodLevel.Calm)
                gw.Goodwill += CalmCarryGain * deltaTime;
        }

        gw.Goodwill = MathHelper.Clamp(gw.Goodwill, 0f, 100f);

        // Tick dialogue cooldown.
        if (gw.DialogueCooldown > 0f)
            gw.DialogueCooldown -= deltaTime;

        _wasCarriedLastFrame = ps.IsBeingCarried;
    }

    // =========================================================================
    //  Helper
    // =========================================================================

    private Entity FindPrincess()
    {
        foreach (var e in World.GetEntitiesWithTag("Princess"))
            return e;
        return Entity.Null;
    }
}
