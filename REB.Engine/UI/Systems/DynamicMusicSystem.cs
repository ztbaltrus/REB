using REB.Engine.Boss;
using REB.Engine.Boss.Components;
using REB.Engine.Boss.Systems;
using REB.Engine.Combat.Systems;
using REB.Engine.ECS;
using REB.Engine.KingsCourt;
using REB.Engine.KingsCourt.Components;
using REB.Engine.Tavern;
using REB.Engine.Tavern.Components;
using REB.Engine.Tavern.Systems;
using REB.Engine.UI.Components;

namespace REB.Engine.UI.Systems;

/// <summary>
/// Priority-based dynamic music selection.
/// <para>
/// Each frame the system determines the highest-priority track warranted by the
/// current game state, then crossfades to it over <see cref="DynamicMusicComponent.TransitionDuration"/>
/// seconds. Priority (highest wins):
/// </para>
/// <list type="number">
///   <item>BossEncounter — any non-Defeated boss entity present.</item>
///   <item>KingsCourt — King is in an active (non-Inactive, non-Dismissed) phase.</item>
///   <item>Tavern — Tavern scene is Open.</item>
///   <item>Combat — CombatSystem generated at least one hit this frame.</item>
///   <item>Exploration — a Player entity exists (default in-run ambient).</item>
///   <item>None — no active run.</item>
/// </list>
/// <para>
/// Fires an <see cref="AudioTriggerEvent"/> in <see cref="AudioEvents"/> whenever
/// <see cref="DynamicMusicComponent.CurrentTrack"/> changes.
/// </para>
/// </summary>
[RunAfter(typeof(CombatSystem))]
[RunAfter(typeof(BossSystem))]
[RunAfter(typeof(TavernSceneSystem))]
public sealed class DynamicMusicSystem : GameSystem
{
    private readonly List<AudioTriggerEvent> _audioEvents = new();

    private MusicTrack _currentTrack = MusicTrack.None;

    /// <summary>Audio track-change events published this frame. Cleared each update.</summary>
    public IReadOnlyList<AudioTriggerEvent> AudioEvents => _audioEvents;

    /// <summary>The track that is currently playing.</summary>
    public MusicTrack CurrentTrack => _currentTrack;

    public override void Update(float deltaTime)
    {
        _audioEvents.Clear();

        MusicTrack desired = DetermineDesiredTrack();

        // Tick transition timer on the component (if the entity exists).
        foreach (var e in World.GetEntitiesWithTag("DynamicMusic"))
        {
            if (!World.HasComponent<DynamicMusicComponent>(e)) break;

            ref var dm = ref World.GetComponent<DynamicMusicComponent>(e);

            if (desired != dm.TargetTrack)
            {
                dm.TargetTrack      = desired;
                dm.TransitionTimer  = 0f;
            }

            if (dm.TargetTrack != dm.CurrentTrack)
            {
                dm.TransitionTimer += deltaTime;

                if (dm.TransitionTimer >= dm.TransitionDuration)
                {
                    dm.CurrentTrack    = dm.TargetTrack;
                    dm.TransitionTimer = dm.TransitionDuration;
                    PublishIfChanged(dm.CurrentTrack);
                }
            }

            break;
        }

        // Stateless mode: track changes take effect immediately (no entity required).
        if (desired != _currentTrack)
        {
            _currentTrack = desired;
            PublishIfChanged(_currentTrack);
        }
    }

    // =========================================================================
    //  Priority logic
    // =========================================================================

    private MusicTrack DetermineDesiredTrack()
    {
        // Boss (highest priority).
        foreach (var e in World.Query<BossComponent>())
        {
            var boss = World.GetComponent<BossComponent>(e);
            if (boss.Phase != BossPhase.Defeated)
                return MusicTrack.BossEncounter;
        }

        // King's Court active.
        foreach (var e in World.GetEntitiesWithTag("King"))
        {
            if (World.HasComponent<KingStateComponent>(e))
            {
                var ks = World.GetComponent<KingStateComponent>(e);
                if (ks.Phase != KingsCourtPhase.Inactive && ks.Phase != KingsCourtPhase.Dismissed)
                    return MusicTrack.KingsCourt;
            }
            break;
        }

        // Tavern open.
        foreach (var e in World.GetEntitiesWithTag("Tavern"))
        {
            if (World.HasComponent<TavernStateComponent>(e))
            {
                var ts = World.GetComponent<TavernStateComponent>(e);
                if (ts.Phase == TavernPhase.Open)
                    return MusicTrack.Tavern;
            }
            break;
        }

        // Active combat this frame.
        if (World.TryGetSystem<CombatSystem>(out var combatSys) &&
            combatSys.CombatEvents.Count > 0)
        {
            return MusicTrack.Combat;
        }

        // Default in-run ambient.
        foreach (var _ in World.GetEntitiesWithTag("Player"))
            return MusicTrack.Exploration;

        return MusicTrack.None;
    }

    private void PublishIfChanged(MusicTrack track)
    {
        _audioEvents.Add(new AudioTriggerEvent(track));
    }
}
