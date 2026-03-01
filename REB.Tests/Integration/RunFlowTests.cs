using REB.Engine.ECS;
using REB.Engine.KingsCourt;
using REB.Engine.KingsCourt.Components;
using REB.Engine.KingsCourt.Systems;
using REB.Engine.Loot.Components;
using REB.Engine.Loot.Systems;
using REB.Engine.Player.Princess.Components;
using REB.Engine.RunManagement;
using REB.Engine.RunManagement.Components;
using REB.Engine.RunManagement.Systems;
using REB.Engine.Tavern;
using REB.Engine.Tavern.Components;
using REB.Engine.Tavern.Systems;
using REB.Engine.World;
using REB.Engine.World.Components;
using REB.Engine.World.Systems;
using Xunit;

namespace REB.Tests.Integration;

// ---------------------------------------------------------------------------
//  Full run-flow integration tests (Story 10.1 — QA Process & Automation)
//  These tests exercise the complete run loop end-to-end:
//    ProceduralFloorGeneratorSystem → RunManagerSystem → LootSpawnSystem
//    → KingsCourtSceneSystem → TavernSceneSystem → new run
//
//  No GraphicsDevice is required; all systems run headless.
// ---------------------------------------------------------------------------

public sealed class RunFlowTests
{
    // =========================================================================
    //  World builder
    // =========================================================================

    private static (World world, RunManagerSystem runMgr) BuildRunWorld(int masterSeed = 42)
    {
        var world      = new World();
        var floorGen   = new ProceduralFloorGeneratorSystem(seed: 1, gridWidth: 24, gridHeight: 24);
        var lootSpawn  = new LootSpawnSystem(seed: 1);
        var courtScene = new KingsCourtSceneSystem();
        var tavernScene = new TavernSceneSystem();
        var runMgr     = new RunManagerSystem(masterSeed);

        world.RegisterSystem(floorGen);
        world.RegisterSystem(lootSpawn);
        world.RegisterSystem(courtScene);
        world.RegisterSystem(tavernScene);
        world.RegisterSystem(runMgr);

        // Scene entities.
        var king = world.CreateEntity();
        world.AddTag(king, "King");
        world.AddComponent(king, KingStateComponent.Default);

        var summary = world.CreateEntity();
        world.AddTag(summary, "RunSummary");
        world.AddComponent(summary, new RunSummaryComponent());

        var princess = world.CreateEntity();
        world.AddTag(princess, "Princess");
        world.AddComponent(princess, PrincessStateComponent.Default);
        world.AddComponent(princess, PrincessGoodwillComponent.Default);
        world.AddComponent(princess, PrincessTraitComponent.Random(1));
        world.AddComponent(princess, NavAgentComponent.Default);

        var ledger = world.CreateEntity();
        world.AddTag(ledger, "TreasureLedger");
        world.AddComponent(ledger, TreasureLedgerComponent.Default);

        var tavern = world.CreateEntity();
        world.AddTag(tavern, "Tavern");
        world.AddComponent(tavern, TavernStateComponent.Default);

        return (world, runMgr);
    }

    // =========================================================================
    //  Floor generation
    // =========================================================================

    [Fact]
    public void FloorIsGenerated_OnFirstUpdate()
    {
        var (world, _) = BuildRunWorld();

        world.Update(0.016f);

        int roomCount = 0;
        foreach (var _ in world.Query<RoomComponent>()) roomCount++;
        Assert.True(roomCount >= 2, "Expected at least 2 rooms after first update.");
        world.Dispose();
    }

    [Fact]
    public void FloorRooms_AreTaggedCorrectly()
    {
        var (world, _) = BuildRunWorld();
        world.Update(0.016f);

        Assert.Single(world.GetEntitiesWithTag("Entrance"));
        Assert.Single(world.GetEntitiesWithTag("PrincessChamber"));
        world.Dispose();
    }

    [Fact]
    public void FloorSeed_ProducesDeterministicLayout_WithSameMasterSeed()
    {
        var (w1, _) = BuildRunWorld(masterSeed: 100);
        var (w2, _) = BuildRunWorld(masterSeed: 100);

        w1.Update(0.016f);
        w2.Update(0.016f);

        // Same master seed → same run-1 floor seed → same room count.
        int count1 = 0, count2 = 0;
        foreach (var _ in w1.Query<RoomComponent>()) count1++;
        foreach (var _ in w2.Query<RoomComponent>()) count2++;

        Assert.Equal(count1, count2);
        w1.Dispose();
        w2.Dispose();
    }

    [Fact]
    public void DifferentMasterSeeds_UsuallyProduceDifferentLayouts()
    {
        var (w1, _) = BuildRunWorld(masterSeed: 1);
        var (w2, _) = BuildRunWorld(masterSeed: 2);

        w1.Update(0.016f);
        w2.Update(0.016f);

        var gen1 = w1.GetSystem<ProceduralFloorGeneratorSystem>();
        var gen2 = w2.GetSystem<ProceduralFloorGeneratorSystem>();

        // Sample interior tiles; at least one should differ.
        bool anyDifference = false;
        for (int y = 2; y < 22 && !anyDifference; y++)
        for (int x = 2; x < 22 && !anyDifference; x++)
        {
            if (gen1.GetTile(x, y) != gen2.GetTile(x, y))
                anyDifference = true;
        }

        Assert.True(anyDifference, "Seeds 1 and 2 produced identical tile grids.");
        w1.Dispose();
        w2.Dispose();
    }

    // =========================================================================
    //  Run 1 → Run 2 transition
    // =========================================================================

    [Fact]
    public void AfterTavernCloses_NewFloorIsGenerated()
    {
        var (world, runMgr) = BuildRunWorld(masterSeed: 7);
        world.Update(0.016f);   // run 1

        // Capture run-1 floor layout before transition.
        var gen    = world.GetSystem<ProceduralFloorGeneratorSystem>();
        int seed1  = runMgr.CurrentFloorSeed;

        // Simulate tavern open → close to trigger run 2.
        var tavern = world.GetEntitiesWithTag("Tavern").First();
        ref var ts = ref world.GetComponent<TavernStateComponent>(tavern);
        ts.Phase      = TavernPhase.Open;
        ts.SceneActive = true;
        world.Update(0.016f);

        ts.Phase      = TavernPhase.Inactive;
        ts.SceneActive = false;
        world.Update(0.016f);   // run 2 starts

        Assert.Equal(2, runMgr.RunNumber);
        Assert.NotEqual(seed1, runMgr.CurrentFloorSeed);
        world.Dispose();
    }

    [Fact]
    public void SecondRun_HasDifferentTheme_ThanFirstRun()
    {
        var (world, runMgr) = BuildRunWorld(masterSeed: 5);
        world.Update(0.016f);
        var theme1 = runMgr.CurrentTheme;

        var tavern = world.GetEntitiesWithTag("Tavern").First();
        ref var ts = ref world.GetComponent<TavernStateComponent>(tavern);
        ts.Phase = TavernPhase.Open; ts.SceneActive = true;
        world.Update(0.016f);
        ts.Phase = TavernPhase.Inactive; ts.SceneActive = false;
        world.Update(0.016f);

        Assert.NotEqual(theme1, runMgr.CurrentTheme);
        world.Dispose();
    }

    [Fact]
    public void FloorRooms_AreCleanedUp_BeforeRegenerating()
    {
        var (world, runMgr) = BuildRunWorld(masterSeed: 3);
        world.Update(0.016f);

        int roomsBefore = world.GetEntitiesWithTag("Room").Count();

        // Trigger next run.
        var tavern = world.GetEntitiesWithTag("Tavern").First();
        ref var ts = ref world.GetComponent<TavernStateComponent>(tavern);
        ts.Phase = TavernPhase.Open; ts.SceneActive = true;
        world.Update(0.016f);
        ts.Phase = TavernPhase.Inactive; ts.SceneActive = false;
        world.Update(0.016f);

        int roomsAfter = world.GetEntitiesWithTag("Room").Count();

        // Rooms should have been replaced, not doubled.
        Assert.True(roomsAfter > 0, "New floor should have rooms.");
        Assert.True(roomsAfter <= roomsBefore * 2,
            "Room count should not have doubled (old rooms not cleaned up).");
        world.Dispose();
    }

    // =========================================================================
    //  King's Court phase in the loop
    // =========================================================================

    [Fact]
    public void Phase_TransitionsToKingsCourt_WhenRunSummaryComplete()
    {
        var (world, runMgr) = BuildRunWorld();
        world.Update(0.016f);

        Assert.Equal(RunPhase.InRun, runMgr.Phase);

        var summaryEntity = world.GetEntitiesWithTag("RunSummary").First();
        ref var rs = ref world.GetComponent<RunSummaryComponent>(summaryEntity);
        rs.IsComplete = true;

        world.Update(0.016f);

        Assert.Equal(RunPhase.KingsCourt, runMgr.Phase);
        world.Dispose();
    }

    // =========================================================================
    //  Reproducibility across multi-run sequences
    // =========================================================================

    [Fact]
    public void FiveRuns_WithSameMasterSeed_ProduceSameThemeSequence()
    {
        var themes1 = CollectThemes(masterSeed: 77, runCount: 5);
        var themes2 = CollectThemes(masterSeed: 77, runCount: 5);
        Assert.Equal(themes1, themes2);
    }

    [Fact]
    public void FiveRuns_WithDifferentMasterSeeds_ProduceDifferentFloorSeeds()
    {
        // Themes cycle by run number only (independent of master seed, by design).
        // Floor seeds, however, ARE derived from the master seed and must differ.
        var seeds1 = CollectFloorSeeds(masterSeed: 1, runCount: 5);
        var seeds2 = CollectFloorSeeds(masterSeed: 2, runCount: 5);

        bool allSame = seeds1.SequenceEqual(seeds2);
        Assert.False(allSame, "Different master seeds should produce different floor seed sequences.");
    }

    private static List<FloorTheme> CollectThemes(int masterSeed, int runCount)
    {
        var (world, runMgr) = BuildRunWorld(masterSeed);
        world.Update(0.016f);
        var themes = new List<FloorTheme> { runMgr.CurrentTheme };
        for (int i = 1; i < runCount; i++)
        {
            runMgr.StartNextRun();
            themes.Add(runMgr.CurrentTheme);
        }
        world.Dispose();
        return themes;
    }

    private static List<int> CollectFloorSeeds(int masterSeed, int runCount)
    {
        var (world, runMgr) = BuildRunWorld(masterSeed);
        world.Update(0.016f);
        var seeds = new List<int> { runMgr.CurrentFloorSeed };
        for (int i = 1; i < runCount; i++)
        {
            runMgr.StartNextRun();
            seeds.Add(runMgr.CurrentFloorSeed);
        }
        world.Dispose();
        return seeds;
    }

    // =========================================================================
    //  Loot reset between runs
    // =========================================================================

    [Fact]
    public void LootItems_AreRespawned_AfterRunTransition()
    {
        var (world, runMgr) = BuildRunWorld(masterSeed: 8);

        // Frame 1: RunManagerSystem starts run 1 and reseeds LootSpawnSystem.
        //          LootSpawnSystem.Update() runs before RunManagerSystem in topo order,
        //          so its initial spawn (with stale seed=1) is destroyed by Reseed().
        //          _initialSpawnDone is reset to false by Reseed().
        world.Update(0.016f);

        // Frame 2: LootSpawnSystem now spawns with the correct run-1 seed.
        world.Update(0.016f);
        int itemsRun1 = world.GetEntitiesWithTag("Item").Count();

        var tavern = world.GetEntitiesWithTag("Tavern").First();
        ref var ts = ref world.GetComponent<TavernStateComponent>(tavern);
        ts.Phase = TavernPhase.Open; ts.SceneActive = true;
        world.Update(0.016f);
        ts.Phase = TavernPhase.Inactive; ts.SceneActive = false;

        // Frame 4: new run 2 starts (RunManager calls Reseed again). Items reset.
        world.Update(0.016f);
        // Frame 5: LootSpawnSystem respawns with run-2 seed.
        world.Update(0.016f);
        int itemsRun2 = world.GetEntitiesWithTag("Item").Count();

        // Both runs should have spawned items (exact count may differ due to room layouts).
        Assert.True(itemsRun1 > 0, "Run 1 should have items.");
        Assert.True(itemsRun2 > 0, "Run 2 should have items.");
        world.Dispose();
    }
}
