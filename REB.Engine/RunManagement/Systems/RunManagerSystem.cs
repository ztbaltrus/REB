using REB.Engine.ECS;
using REB.Engine.KingsCourt;
using REB.Engine.KingsCourt.Components;
using REB.Engine.KingsCourt.Systems;
using REB.Engine.Loot.Components;
using REB.Engine.Loot.Systems;
using REB.Engine.Player.Princess.Components;
using REB.Engine.RunManagement.Components;
using REB.Engine.RunManagement.Events;
using REB.Engine.Tavern;
using REB.Engine.Tavern.Components;
using REB.Engine.Tavern.Systems;
using REB.Engine.World;
using REB.Engine.World.Systems;

namespace REB.Engine.RunManagement.Systems;

/// <summary>
/// Orchestrates the full procedural run loop — the central system for Story 10.4.
/// <para>Responsibilities:</para>
/// <list type="number">
///   <item>Derives unique, deterministic seeds for each run from a master seed + run counter.</item>
///   <item>Calls <see cref="ProceduralFloorGeneratorSystem.Regenerate"/> to rebuild the floor.</item>
///   <item>Calls <see cref="LootSpawnSystem.Reseed"/> to repopulate loot on the new floor.</item>
///   <item>Resets the princess, run summary, treasure ledger, and king state for each run.</item>
///   <item>Publishes <see cref="RunStartedEvent"/> and <see cref="RunCompletedEvent"/>.</item>
///   <item>Watches the Tavern close → triggers the next run automatically.</item>
/// </list>
/// <para>
/// Seed derivation uses Murmur3-inspired integer mixing so that runs with adjacent
/// numbers produce completely different layouts.  The master seed itself can be a fixed
/// value (deterministic playthroughs) or <c>Environment.TickCount</c> (random each session).
/// </para>
/// </summary>
[RunAfter(typeof(TavernSceneSystem))]
[RunAfter(typeof(KingsCourtSceneSystem))]
public sealed class RunManagerSystem : GameSystem
{
    // =========================================================================
    //  Construction
    // =========================================================================

    private readonly int _masterSeed;

    /// <param name="masterSeed">
    /// Root seed for all run-seed derivation.  Pass 0 to use a random value each
    /// session (<c>Environment.TickCount</c>). Pass any other value for a fully
    /// deterministic run sequence (useful for QA and speedruns).
    /// </param>
    public RunManagerSystem(int masterSeed = 0)
    {
        _masterSeed = masterSeed == 0
            ? Environment.TickCount
            : masterSeed;
    }

    // =========================================================================
    //  Public telemetry
    // =========================================================================

    /// <summary>1-based index of the current (or most recently started) run.</summary>
    public int RunNumber => _runNumber;

    /// <summary>Current high-level phase of the run cycle.</summary>
    public RunPhase Phase => _phase;

    /// <summary>Floor seed used for the current run's layout.</summary>
    public int CurrentFloorSeed { get; private set; }

    /// <summary>Loot seed used for the current run's item placement.</summary>
    public int CurrentLootSeed  { get; private set; }

    /// <summary>Enemy seed available for current-run AI variance.</summary>
    public int CurrentEnemySeed { get; private set; }

    /// <summary>Floor theme selected for the current run.</summary>
    public FloorTheme CurrentTheme { get; private set; }

    /// <summary>Difficulty level (1–10) for the current run.</summary>
    public int CurrentDifficulty { get; private set; }

    /// <summary>Run-start events published this frame. Cleared each update.</summary>
    public IReadOnlyList<RunStartedEvent> RunStartedEvents => _startedEvents;

    /// <summary>Run-complete events published this frame. Cleared each update.</summary>
    public IReadOnlyList<RunCompletedEvent> RunCompletedEvents => _completedEvents;

    // =========================================================================
    //  Private state
    // =========================================================================

    private readonly List<RunStartedEvent>   _startedEvents   = new();
    private readonly List<RunCompletedEvent> _completedEvents = new();

    private int      _runNumber;
    private RunPhase _phase          = RunPhase.Idle;
    private bool     _firstFrame     = true;
    private bool     _wasTavernOpen;

    // =========================================================================
    //  Public API
    // =========================================================================

    /// <summary>
    /// Immediately starts the next run, incrementing the run counter.
    /// Called automatically when the Tavern closes; can also be invoked manually
    /// from tests or menu systems.
    /// </summary>
    public void StartNextRun()
    {
        _runNumber++;
        StartRun();
    }

    // =========================================================================
    //  Update
    // =========================================================================

    public override void Update(float deltaTime)
    {
        _startedEvents.Clear();
        _completedEvents.Clear();

        // First frame only: kick off run 1 without waiting for a tavern close.
        if (_firstFrame)
        {
            _firstFrame = false;
            _runNumber  = 1;
            StartRun();
            return;
        }

        // ── Tavern-close watch: Open → Inactive transition starts the next run ──
        WatchTavern();

        // ── RunSummary completion watch: transition InRun → KingsCourt ──
        if (_phase == RunPhase.InRun)
            WatchRunSummary();

        // ── King Dismissed watch: transition KingsCourt → Tavern ──
        if (_phase == RunPhase.KingsCourt)
            WatchKingDismissed();
    }

    // =========================================================================
    //  Run lifecycle
    // =========================================================================

    private void StartRun()
    {
        // ── Derive per-run seeds ──────────────────────────────────────────────
        CurrentFloorSeed  = DeriveKey(_masterSeed, _runNumber, slot: 0);
        CurrentLootSeed   = DeriveKey(_masterSeed, _runNumber, slot: 1);
        CurrentEnemySeed  = DeriveKey(_masterSeed, _runNumber, slot: 2);
        int princessSeed  = DeriveKey(_masterSeed, _runNumber, slot: 3);

        // ── Select theme — cycle through all themes, rotating each run ────────
        var themes = Enum.GetValues<FloorTheme>();
        CurrentTheme = themes[Math.Abs(_runNumber - 1) % themes.Length];

        // ── Ramp difficulty every 3 runs (capped at 10) ───────────────────────
        CurrentDifficulty = Math.Min(1 + (_runNumber - 1) / 3, 10);

        // ── Rebuild floor layout ──────────────────────────────────────────────
        if (World.TryGetSystem<ProceduralFloorGeneratorSystem>(out var floorGen))
            floorGen.Regenerate(CurrentFloorSeed, CurrentTheme);

        // ── Re-seed and re-scatter loot ───────────────────────────────────────
        if (World.TryGetSystem<LootSpawnSystem>(out var lootSpawn))
            lootSpawn.Reseed(CurrentLootSeed, CurrentDifficulty);

        // ── Reset per-run state ───────────────────────────────────────────────
        ResetPrincess(princessSeed, floorGen);
        ResetRunSummary();
        ResetTreasureLedger();
        ResetKingState();

        // ── Publish RunConfig ──────────────────────────────────────────────────
        var config = new RunConfigComponent
        {
            RunNumber       = _runNumber,
            MasterSeed      = _masterSeed,
            FloorSeed       = CurrentFloorSeed,
            LootSeed        = CurrentLootSeed,
            EnemySeed       = CurrentEnemySeed,
            PrincessSeed    = princessSeed,
            Theme           = CurrentTheme,
            FloorDifficulty = CurrentDifficulty,
        };

        UpsertRunConfig(config);

        _phase         = RunPhase.InRun;
        _wasTavernOpen = false;

        _startedEvents.Add(new RunStartedEvent(_runNumber, config));
    }

    // =========================================================================
    //  State reset helpers
    // =========================================================================

    private void ResetPrincess(int princessSeed, ProceduralFloorGeneratorSystem? floorGen)
    {
        foreach (var e in World.GetEntitiesWithTag("Princess"))
        {
            if (World.HasComponent<PrincessStateComponent>(e))
                World.SetComponent(e, PrincessStateComponent.Default);

            if (World.HasComponent<REB.Engine.Combat.Components.HealthComponent>(e))
                World.SetComponent(e, REB.Engine.Combat.Components.HealthComponent.For(100f));

            if (World.HasComponent<PrincessGoodwillComponent>(e))
                World.SetComponent(e, PrincessGoodwillComponent.Default);

            if (World.HasComponent<PrincessTraitComponent>(e))
                World.SetComponent(e, PrincessTraitComponent.Random(princessSeed));

            if (World.HasComponent<NavAgentComponent>(e))
                World.SetComponent(e, NavAgentComponent.Default);

            // Move princess to the PrincessChamber room if one exists.
            if (floorGen != null && World.HasComponent<REB.Engine.Rendering.Components.TransformComponent>(e))
                PlacePrincessInChamber(e, floorGen);
        }
    }

    private void PlacePrincessInChamber(Entity princess, ProceduralFloorGeneratorSystem floorGen)
    {
        foreach (var room in World.Query<REB.Engine.World.Components.RoomComponent>())
        {
            var r = World.GetComponent<REB.Engine.World.Components.RoomComponent>(room);
            if (r.Type != REB.Engine.World.RoomType.PrincessChamber) continue;

            ref var t = ref World.GetComponent<REB.Engine.Rendering.Components.TransformComponent>(princess);
            t.Position = ProceduralFloorGeneratorSystem.TileToWorld(
                r.GridX + r.Width  / 2,
                r.GridY + r.Height / 2);
            return;
        }
    }

    private void ResetRunSummary()
    {
        foreach (var e in World.GetEntitiesWithTag("RunSummary"))
            World.SetComponent(e, new KingsCourt.Components.RunSummaryComponent());
    }

    private void ResetTreasureLedger()
    {
        foreach (var e in World.GetEntitiesWithTag("TreasureLedger"))
        {
            if (World.HasComponent<TreasureLedgerComponent>(e))
                World.SetComponent(e, TreasureLedgerComponent.Default);
        }
    }

    private void ResetKingState()
    {
        foreach (var e in World.GetEntitiesWithTag("King"))
        {
            if (World.HasComponent<KingStateComponent>(e))
                World.SetComponent(e, KingStateComponent.Default);
        }
    }

    // =========================================================================
    //  Transition watches
    // =========================================================================

    private void WatchTavern()
    {
        Entity tavern = FindTagged("Tavern");
        if (!World.IsAlive(tavern) || !World.HasComponent<TavernStateComponent>(tavern)) return;

        var ts = World.GetComponent<TavernStateComponent>(tavern);

        if (ts.Phase == TavernPhase.Open)
        {
            _wasTavernOpen = true;
            _phase         = RunPhase.Tavern;
        }
        else if (ts.Phase == TavernPhase.Inactive && _wasTavernOpen && _phase == RunPhase.Tavern)
        {
            // Tavern just closed → begin the next run.
            _wasTavernOpen = false;
            _runNumber++;
            StartRun();
        }
    }

    private void WatchRunSummary()
    {
        Entity summary = FindTagged("RunSummary");
        if (!World.IsAlive(summary) || !World.HasComponent<KingsCourt.Components.RunSummaryComponent>(summary)) return;

        var rs = World.GetComponent<KingsCourt.Components.RunSummaryComponent>(summary);
        if (!rs.IsComplete) return;

        _completedEvents.Add(new RunCompletedEvent(_runNumber, rs.PrincessDeliveredSafely));
        _phase = RunPhase.KingsCourt;
    }

    private void WatchKingDismissed()
    {
        Entity king = FindTagged("King");
        if (!World.IsAlive(king) || !World.HasComponent<KingStateComponent>(king)) return;

        var ks = World.GetComponent<KingStateComponent>(king);
        if (ks.Phase == KingsCourtPhase.Dismissed)
            _phase = RunPhase.Tavern;
    }

    // =========================================================================
    //  Entity helpers
    // =========================================================================

    private void UpsertRunConfig(RunConfigComponent config)
    {
        foreach (var e in World.GetEntitiesWithTag("RunConfig"))
        {
            World.SetComponent(e, config);
            return;
        }

        var entity = World.CreateEntity();
        World.AddTag(entity, "RunConfig");
        World.AddComponent(entity, config);
    }

    private Entity FindTagged(string tag)
    {
        foreach (var e in World.GetEntitiesWithTag(tag))
            return e;
        return Entity.Null;
    }

    // =========================================================================
    //  Seed derivation — Murmur3-inspired integer mixing
    // =========================================================================

    /// <summary>
    /// Derives a sub-seed from <paramref name="masterSeed"/> + <paramref name="runNumber"/>
    /// + <paramref name="slot"/>. Adjacent run numbers and adjacent slots produce
    /// completely different values.
    /// </summary>
    private static int DeriveKey(int masterSeed, int runNumber, int slot)
    {
        unchecked
        {
            int h = masterSeed;
            h ^= runNumber * (int)2654435761u;
            h ^= slot      * (int)2246822519u;
            h ^= h >>> 16;
            h *= (int)2246822519u;
            h ^= h >>> 13;
            h *= (int)2654435761u;
            h ^= h >>> 16;
            return h;
        }
    }
}
