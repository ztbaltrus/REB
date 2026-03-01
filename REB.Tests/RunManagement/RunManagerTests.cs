using REB.Engine.ECS;
using REB.Engine.KingsCourt;
using REB.Engine.KingsCourt.Components;
using REB.Engine.Loot.Components;
using REB.Engine.Loot.Systems;
using REB.Engine.Player.Princess.Components;
using REB.Engine.RunManagement;
using REB.Engine.RunManagement.Components;
using REB.Engine.RunManagement.Events;
using REB.Engine.RunManagement.Systems;
using REB.Engine.Tavern;
using REB.Engine.Tavern.Components;
using REB.Engine.Tavern.Systems;
using REB.Engine.World;
using REB.Engine.World.Systems;
using Xunit;

namespace REB.Tests.RunManagement;

// ---------------------------------------------------------------------------
//  RunManagerSystem tests (Story 10.4 — Release Pipeline)
//  Verifies per-run seed derivation, floor regeneration, state resets,
//  and the procedural run loop (Tavern close → new run).
// ---------------------------------------------------------------------------

public sealed class RunManagerTests
{
    // =========================================================================
    //  Helpers
    // =========================================================================

    private static (World world, RunManagerSystem runManager) BuildMinimalWorld(
        int masterSeed = 99)
    {
        var world      = new World();
        var runManager = new RunManagerSystem(masterSeed);
        // Floor gen needed for Regenerate; small grid for speed.
        world.RegisterSystem(new ProceduralFloorGeneratorSystem(seed: 1, gridWidth: 24, gridHeight: 24));
        world.RegisterSystem(new LootSpawnSystem(seed: 1));
        world.RegisterSystem(runManager);
        return (world, runManager);
    }

    private static (World world, RunManagerSystem runManager) BuildFullWorld(
        int masterSeed = 99)
    {
        var world      = new World();
        var floorGen   = new ProceduralFloorGeneratorSystem(seed: 1, gridWidth: 24, gridHeight: 24);
        var lootSpawn  = new LootSpawnSystem(seed: 1);
        var runManager = new RunManagerSystem(masterSeed);

        world.RegisterSystem(floorGen);
        world.RegisterSystem(lootSpawn);
        world.RegisterSystem(new TavernSceneSystem());
        world.RegisterSystem(runManager);

        // Scene entities.
        SpawnKing(world);
        SpawnRunSummary(world);
        SpawnPrincess(world);
        SpawnTreasureLedger(world);
        SpawnTavern(world);

        return (world, runManager);
    }

    private static Entity SpawnKing(World world)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "King");
        world.AddComponent(e, KingStateComponent.Default);
        return e;
    }

    private static Entity SpawnRunSummary(World world)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "RunSummary");
        world.AddComponent(e, new RunSummaryComponent());
        return e;
    }

    private static Entity SpawnPrincess(World world)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "Princess");
        world.AddComponent(e, PrincessStateComponent.Default);
        world.AddComponent(e, PrincessGoodwillComponent.Default);
        world.AddComponent(e, PrincessTraitComponent.Random(1));
        world.AddComponent(e, NavAgentComponent.Default);
        return e;
    }

    private static Entity SpawnTreasureLedger(World world)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "TreasureLedger");
        world.AddComponent(e, TreasureLedgerComponent.Default);
        return e;
    }

    private static Entity SpawnTavern(World world)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "Tavern");
        world.AddComponent(e, TavernStateComponent.Default);
        return e;
    }

    // =========================================================================
    //  Run 1 starts on the first frame
    // =========================================================================

    [Fact]
    public void RunNumber_IsOne_AfterFirstUpdate()
    {
        var (world, mgr) = BuildMinimalWorld();

        world.Update(0.016f);

        Assert.Equal(1, mgr.RunNumber);
        world.Dispose();
    }

    [Fact]
    public void Phase_IsInRun_AfterFirstUpdate()
    {
        var (world, mgr) = BuildMinimalWorld();

        world.Update(0.016f);

        Assert.Equal(RunPhase.InRun, mgr.Phase);
        world.Dispose();
    }

    [Fact]
    public void RunStartedEvent_Published_OnFirstFrame()
    {
        var (world, mgr) = BuildMinimalWorld();

        world.Update(0.016f);

        Assert.Single(mgr.RunStartedEvents);
        Assert.Equal(1, mgr.RunStartedEvents[0].RunNumber);
        world.Dispose();
    }

    // =========================================================================
    //  Seed derivation
    // =========================================================================

    [Fact]
    public void SameMasterSeed_ProducesSameFloorSeedForSameRun()
    {
        var (w1, m1) = BuildMinimalWorld(masterSeed: 42);
        var (w2, m2) = BuildMinimalWorld(masterSeed: 42);

        w1.Update(0.016f);
        w2.Update(0.016f);

        Assert.Equal(m1.CurrentFloorSeed, m2.CurrentFloorSeed);
        w1.Dispose();
        w2.Dispose();
    }

    [Fact]
    public void DifferentMasterSeeds_ProduceDifferentFloorSeeds()
    {
        var (w1, m1) = BuildMinimalWorld(masterSeed: 1);
        var (w2, m2) = BuildMinimalWorld(masterSeed: 2);

        w1.Update(0.016f);
        w2.Update(0.016f);

        Assert.NotEqual(m1.CurrentFloorSeed, m2.CurrentFloorSeed);
        w1.Dispose();
        w2.Dispose();
    }

    [Fact]
    public void FloorSeed_LootSeed_EnemySeed_AreAllDistinct()
    {
        var (world, mgr) = BuildMinimalWorld();
        world.Update(0.016f);

        // With high probability, the three slots produce distinct values.
        var seeds = new[] { mgr.CurrentFloorSeed, mgr.CurrentLootSeed, mgr.CurrentEnemySeed };
        Assert.Equal(seeds.Length, seeds.Distinct().Count());
        world.Dispose();
    }

    // =========================================================================
    //  Theme cycling
    // =========================================================================

    [Fact]
    public void Run1_StartsWithDungeonTheme()
    {
        var (world, mgr) = BuildMinimalWorld();
        world.Update(0.016f);

        // Run 1 → index 0 of FloorTheme values = Dungeon.
        Assert.Equal(FloorTheme.Dungeon, mgr.CurrentTheme);
        world.Dispose();
    }

    [Fact]
    public void ThemeCycles_ThroughAllThemesOverSixRuns()
    {
        // Build a world with no tavern/king so we can call StartNextRun() manually.
        var (world, mgr) = BuildMinimalWorld(masterSeed: 1);
        world.Update(0.016f);   // run 1

        var themes = new List<FloorTheme> { mgr.CurrentTheme };
        for (int i = 0; i < 5; i++)
        {
            mgr.StartNextRun();
            themes.Add(mgr.CurrentTheme);
        }

        // All 6 FloorTheme values should appear exactly once.
        Assert.Equal(6, themes.Distinct().Count());
        world.Dispose();
    }

    // =========================================================================
    //  Difficulty scaling
    // =========================================================================

    [Fact]
    public void Difficulty_StartsAtOne()
    {
        var (world, mgr) = BuildMinimalWorld();
        world.Update(0.016f);

        Assert.Equal(1, mgr.CurrentDifficulty);
        world.Dispose();
    }

    [Fact]
    public void Difficulty_IncrementsEveryThreeRuns()
    {
        var (world, mgr) = BuildMinimalWorld();
        world.Update(0.016f);   // run 1 → difficulty 1

        mgr.StartNextRun(); mgr.StartNextRun(); mgr.StartNextRun();   // runs 2,3,4
        // Run 4 = 1 + (4-1)/3 = 1 + 1 = 2
        Assert.Equal(2, mgr.CurrentDifficulty);
        world.Dispose();
    }

    [Fact]
    public void Difficulty_CappedAtTen()
    {
        var (world, mgr) = BuildMinimalWorld();
        world.Update(0.016f);

        for (int i = 0; i < 50; i++) mgr.StartNextRun();

        Assert.Equal(10, mgr.CurrentDifficulty);
        world.Dispose();
    }

    // =========================================================================
    //  Floor regeneration
    // =========================================================================

    [Fact]
    public void RunConfig_Entity_CreatedWithTag()
    {
        var (world, mgr) = BuildMinimalWorld();
        world.Update(0.016f);

        Assert.Single(world.GetEntitiesWithTag("RunConfig"));
        world.Dispose();
    }

    [Fact]
    public void RunConfig_RunNumber_MatchesManagerRunNumber()
    {
        var (world, mgr) = BuildMinimalWorld();
        world.Update(0.016f);

        var cfgEntity = world.GetEntitiesWithTag("RunConfig").First();
        var cfg       = world.GetComponent<RunConfigComponent>(cfgEntity);

        Assert.Equal(mgr.RunNumber, cfg.RunNumber);
        world.Dispose();
    }

    [Fact]
    public void ConsecutiveRuns_ProduceDifferentFloorSeeds()
    {
        var (world, mgr) = BuildMinimalWorld(masterSeed: 7);
        world.Update(0.016f);
        int seed1 = mgr.CurrentFloorSeed;

        mgr.StartNextRun();
        int seed2 = mgr.CurrentFloorSeed;

        Assert.NotEqual(seed1, seed2);
        world.Dispose();
    }

    // =========================================================================
    //  Run-state resets
    // =========================================================================

    [Fact]
    public void PrincessState_Reset_OnNewRun()
    {
        var (world, mgr) = BuildFullWorld();
        world.Update(0.016f);   // run 1 starts

        // Dirty princess state.
        var princess = world.GetEntitiesWithTag("Princess").First();
        ref var ps   = ref world.GetComponent<PrincessStateComponent>(princess);
        ps.Health         = 25f;
        ps.IsBeingCarried = true;

        // Start run 2.
        mgr.StartNextRun();

        var ps2 = world.GetComponent<PrincessStateComponent>(princess);
        Assert.Equal(PrincessStateComponent.Default.Health, ps2.Health);
        Assert.False(ps2.IsBeingCarried);
        world.Dispose();
    }

    [Fact]
    public void GoodwillComponent_Reset_OnNewRun()
    {
        var (world, mgr) = BuildFullWorld();
        world.Update(0.016f);

        var princess = world.GetEntitiesWithTag("Princess").First();
        ref var gw   = ref world.GetComponent<PrincessGoodwillComponent>(princess);
        gw.Goodwill = 10f;   // very unhappy

        mgr.StartNextRun();

        Assert.Equal(PrincessGoodwillComponent.Default.Goodwill,
            world.GetComponent<PrincessGoodwillComponent>(princess).Goodwill);
        world.Dispose();
    }

    [Fact]
    public void RunSummary_IsComplete_Reset_OnNewRun()
    {
        var (world, mgr) = BuildFullWorld();
        world.Update(0.016f);

        var summaryEntity = world.GetEntitiesWithTag("RunSummary").First();
        ref var rs        = ref world.GetComponent<RunSummaryComponent>(summaryEntity);
        rs.IsComplete     = true;
        rs.LootGoldValue  = 999f;

        mgr.StartNextRun();

        var rs2 = world.GetComponent<RunSummaryComponent>(summaryEntity);
        Assert.False(rs2.IsComplete);
        Assert.Equal(0f, rs2.LootGoldValue);
        world.Dispose();
    }

    [Fact]
    public void TreasureLedger_Reset_OnNewRun()
    {
        var (world, mgr) = BuildFullWorld();
        world.Update(0.016f);

        var ledger = world.GetEntitiesWithTag("TreasureLedger").First();
        ref var tl = ref world.GetComponent<TreasureLedgerComponent>(ledger);
        tl.TotalValue = 1500;

        mgr.StartNextRun();

        Assert.Equal(0, world.GetComponent<TreasureLedgerComponent>(ledger).TotalValue);
        world.Dispose();
    }

    // =========================================================================
    //  Tavern close → new run
    // =========================================================================

    [Fact]
    public void RunNumber_Increments_AfterTavernCloses()
    {
        var (world, mgr) = BuildFullWorld();
        world.Update(0.016f);
        Assert.Equal(1, mgr.RunNumber);

        // Simulate tavern open then close.
        var tavern = world.GetEntitiesWithTag("Tavern").First();
        ref var ts = ref world.GetComponent<TavernStateComponent>(tavern);
        ts.Phase      = TavernPhase.Open;
        ts.SceneActive = true;

        // One frame in the tavern.
        world.Update(0.016f);
        Assert.Equal(RunPhase.Tavern, mgr.Phase);

        // Tavern closes.
        ts.Phase      = TavernPhase.Inactive;
        ts.SceneActive = false;
        world.Update(0.016f);

        Assert.Equal(2, mgr.RunNumber);
        world.Dispose();
    }

    [Fact]
    public void Phase_BackToInRun_AfterTavernClose()
    {
        var (world, mgr) = BuildFullWorld();
        world.Update(0.016f);

        var tavern = world.GetEntitiesWithTag("Tavern").First();
        ref var ts = ref world.GetComponent<TavernStateComponent>(tavern);
        ts.Phase      = TavernPhase.Open;
        ts.SceneActive = true;
        world.Update(0.016f);

        ts.Phase      = TavernPhase.Inactive;
        ts.SceneActive = false;
        world.Update(0.016f);

        Assert.Equal(RunPhase.InRun, mgr.Phase);
        world.Dispose();
    }

    // =========================================================================
    //  Event publishing
    // =========================================================================

    [Fact]
    public void RunStartedEvents_ClearedEachFrame()
    {
        var (world, mgr) = BuildMinimalWorld();
        world.Update(0.016f);
        Assert.Single(mgr.RunStartedEvents);

        world.Update(0.016f);   // second frame — no new run started
        Assert.Empty(mgr.RunStartedEvents);
        world.Dispose();
    }

    [Fact]
    public void RunCompletedEvent_Published_WhenRunSummaryIsComplete()
    {
        var (world, mgr) = BuildFullWorld();
        world.Update(0.016f);

        // Mark run as complete.
        var summaryEntity = world.GetEntitiesWithTag("RunSummary").First();
        ref var rs        = ref world.GetComponent<RunSummaryComponent>(summaryEntity);
        rs.IsComplete              = true;
        rs.PrincessDeliveredSafely = true;

        world.Update(0.016f);

        Assert.Single(mgr.RunCompletedEvents);
        Assert.True(mgr.RunCompletedEvents[0].PrincessDeliveredSafely);
        world.Dispose();
    }

    [Fact]
    public void Phase_ChangesToKingsCourt_WhenRunSummaryIsComplete()
    {
        var (world, mgr) = BuildFullWorld();
        world.Update(0.016f);

        var summaryEntity = world.GetEntitiesWithTag("RunSummary").First();
        ref var rs        = ref world.GetComponent<RunSummaryComponent>(summaryEntity);
        rs.IsComplete = true;

        world.Update(0.016f);

        Assert.Equal(RunPhase.KingsCourt, mgr.Phase);
        world.Dispose();
    }
}
