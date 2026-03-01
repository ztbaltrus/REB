using Microsoft.Xna.Framework;
using REB.Engine.Boss.Systems;
using REB.Engine.Combat.Systems;
using REB.Engine.ECS;
using REB.Engine.Player.Princess.Components;
using REB.Engine.Player.Systems;
using REB.Engine.Tavern.Systems;
using REB.Engine.UI.Components;

namespace REB.Engine.UI.Systems;

/// <summary>
/// Aggregates impactful game events into typed <see cref="HitFeedbackEvent"/> records,
/// then drives screen-shake and gamepad-rumble responses.
/// <para>
/// Reads each frame:
/// <list type="bullet">
///   <item><see cref="CombatSystem.CombatEvents"/> → HitEnemy / HitPlayer / HitPrincess</item>
///   <item><see cref="BossSystem.DefeatedEvents"/>   → BossDefeated</item>
///   <item><see cref="UpgradeTreeSystem.PurchasedEvents"/> → UpgradePurchased</item>
///   <item>Princess <see cref="PrincessStateComponent.IsBeingCarried"/> flip → PrincessDropped</item>
/// </list>
/// </para>
/// <para>
/// For each event the system calls <see cref="ScreenShakeSystem.Trigger"/> and writes
/// <see cref="GamepadRumbleComponent"/> onto player entities.
/// </para>
/// </summary>
[RunAfter(typeof(CombatSystem))]
[RunAfter(typeof(BossSystem))]
[RunAfter(typeof(CarrySystem))]
[RunAfter(typeof(UpgradeTreeSystem))]
public sealed class HitFeedbackSystem : GameSystem
{
    private readonly List<HitFeedbackEvent> _feedbackEvents = new();

    private bool _wasCarried;

    /// <summary>Feedback events generated this frame. Cleared at the start of each update.</summary>
    public IReadOnlyList<HitFeedbackEvent> FeedbackEvents => _feedbackEvents;

    public override void Update(float deltaTime)
    {
        _feedbackEvents.Clear();

        // Tick existing rumble timers on player entities.
        TickRumbles(deltaTime);

        // ── Combat hits ───────────────────────────────────────────────────────
        if (World.TryGetSystem<CombatSystem>(out var combatSys))
        {
            foreach (var ev in combatSys.CombatEvents)
            {
                var feedbackType = FeedbackType.HitEnemy;

                if (World.IsAlive(ev.Target))
                {
                    if (World.HasTag(ev.Target, "Player"))   feedbackType = FeedbackType.HitPlayer;
                    if (World.HasTag(ev.Target, "Princess")) feedbackType = FeedbackType.HitPrincess;
                }

                float intensity = feedbackType == FeedbackType.HitPlayer ? 0.6f : 0.3f;
                Fire(feedbackType, ev.HitPoint, intensity);
            }
        }

        // ── Boss defeated ─────────────────────────────────────────────────────
        if (World.TryGetSystem<BossSystem>(out var bossSys))
        {
            foreach (var ev in bossSys.DefeatedEvents)
                Fire(FeedbackType.BossDefeated, ev.DeathPosition, 1.0f);
        }

        // ── Princess dropped ──────────────────────────────────────────────────
        bool isCarried = false;
        foreach (var e in World.GetEntitiesWithTag("Princess"))
        {
            if (World.HasComponent<PrincessStateComponent>(e))
                isCarried = World.GetComponent<PrincessStateComponent>(e).IsBeingCarried;
            break;
        }

        if (_wasCarried && !isCarried)
            Fire(FeedbackType.PrincessDropped, Vector3.Zero, 0.5f);

        _wasCarried = isCarried;

        // ── Upgrade purchased ─────────────────────────────────────────────────
        if (World.TryGetSystem<UpgradeTreeSystem>(out var upgradesSys))
        {
            foreach (var _ in upgradesSys.PurchasedEvents)
                Fire(FeedbackType.UpgradePurchased, Vector3.Zero, 0.2f);
        }
    }

    // =========================================================================
    //  Helpers
    // =========================================================================

    private void Fire(FeedbackType type, Vector3 position, float intensity)
    {
        _feedbackEvents.Add(new HitFeedbackEvent(type, position, intensity));

        // Write rumble onto active player entities (runtime input layer reads this).
        foreach (var e in World.GetEntitiesWithTag("Player"))
        {
            if (!World.HasComponent<GamepadRumbleComponent>(e)) continue;

            ref var rumble = ref World.GetComponent<GamepadRumbleComponent>(e);
            // Highest intensity of concurrent events wins.
            if (intensity >= rumble.LowFrequency || rumble.TimeRemaining <= 0f)
            {
                rumble.LowFrequency  = intensity;
                rumble.HighFrequency = intensity * 0.5f;
                rumble.Duration      = 0.3f;
                rumble.TimeRemaining = 0.3f;
            }
        }

        // Trigger screen shake (proportional to event intensity).
        if (World.TryGetSystem<ScreenShakeSystem>(out var shaker))
            shaker.Trigger(intensity * 8f, 0.25f);
    }

    private void TickRumbles(float deltaTime)
    {
        foreach (var e in World.Query<GamepadRumbleComponent>())
        {
            ref var rumble = ref World.GetComponent<GamepadRumbleComponent>(e);
            if (rumble.TimeRemaining > 0f)
            {
                rumble.TimeRemaining -= deltaTime;
                if (rumble.TimeRemaining < 0f) rumble.TimeRemaining = 0f;
            }
        }
    }
}
