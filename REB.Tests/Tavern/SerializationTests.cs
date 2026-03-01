using REB.Engine.ECS;
using REB.Engine.KingsCourt.Components;
using REB.Engine.Tavern;
using REB.Engine.Tavern.Components;
using REB.Engine.Tavern.Systems;
using Xunit;

namespace REB.Tests.Tavern;

// ---------------------------------------------------------------------------
//  SerializationSystem tests
//
//  Each test writes to a unique temp directory to avoid cross-test pollution.
//  Directories are cleaned up in Dispose.
// ---------------------------------------------------------------------------

public sealed class SerializationTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(
        Path.GetTempPath(), "REB_SerializationTests_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private (World world, SerializationSystem serialSystem) BuildWorld()
    {
        var world        = new World();
        var serialSystem = new SerializationSystem(_tempDir);
        world.RegisterSystem(serialSystem);
        return (world, serialSystem);
    }

    private static Entity AddGoldLedger(World world, float gold = 500f, ulong flags = 0UL)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "GoldLedger");
        var gc = GoldCurrencyComponent.Default;
        gc.TotalGold = gold;
        world.AddComponent(e, gc);
        var tree = UpgradeTreeComponent.Default;
        tree.PurchasedFlags = flags;
        world.AddComponent(e, tree);
        return e;
    }

    private static Entity AddKing(World world, float score = 50f, int runCount = 3)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "King");
        world.AddComponent(e, KingStateComponent.Default);
        var rel = KingRelationshipComponent.Default;
        rel.Score        = score;
        rel.TotalRunCount = runCount;
        world.AddComponent(e, rel);
        return e;
    }

    private static Entity AddTavernkeeper(World world,
        int consecutivePleased = 0,
        bool medic = false, bool fence = false, bool scout = false)
    {
        var e = world.CreateEntity();
        world.AddTag(e, "Tavernkeeper");
        world.AddComponent(e, new TavernkeeperNPCComponent
        {
            ConsecutivePleasedRuns = consecutivePleased,
            MedicUnlocked          = medic,
            FenceUnlocked          = fence,
            ScoutUnlocked          = scout,
        });
        return e;
    }

    // -------------------------------------------------------------------------
    //  SaveExists
    // -------------------------------------------------------------------------

    [Fact]
    public void SaveExists_ReturnsFalse_BeforeSave()
    {
        var (world, serialSystem) = BuildWorld();

        Assert.False(serialSystem.SaveExists(SaveSlotId.Slot1));
        world.Dispose();
    }

    [Fact]
    public void SaveExists_ReturnsTrue_AfterSave()
    {
        var (world, serialSystem) = BuildWorld();
        AddGoldLedger(world);
        AddKing(world);
        AddTavernkeeper(world);

        serialSystem.Save(SaveSlotId.Slot1);

        Assert.True(serialSystem.SaveExists(SaveSlotId.Slot1));
        world.Dispose();
    }

    [Fact]
    public void SaveExists_DistinctPerSlot()
    {
        var (world, serialSystem) = BuildWorld();
        AddGoldLedger(world);
        AddKing(world);
        AddTavernkeeper(world);

        serialSystem.Save(SaveSlotId.Slot1);

        Assert.True(serialSystem.SaveExists(SaveSlotId.Slot1));
        Assert.False(serialSystem.SaveExists(SaveSlotId.Slot2));
        Assert.False(serialSystem.SaveExists(SaveSlotId.Slot3));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Save / Load roundtrip — gold
    // -------------------------------------------------------------------------

    [Fact]
    public void Load_RestoresGold_FromFile()
    {
        var (world, serialSystem) = BuildWorld();
        var ledger = AddGoldLedger(world, gold: 750f);
        AddKing(world);
        AddTavernkeeper(world);

        serialSystem.Save(SaveSlotId.Slot1);

        // Corrupt the in-memory value.
        ref var gc = ref world.GetComponent<GoldCurrencyComponent>(ledger);
        gc.TotalGold = 0f;

        serialSystem.Load(SaveSlotId.Slot1);

        Assert.Equal(750f,
            world.GetComponent<GoldCurrencyComponent>(ledger).TotalGold, precision: 3);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Save / Load roundtrip — upgrade flags
    // -------------------------------------------------------------------------

    [Fact]
    public void Load_RestoresPurchasedUpgrades_FromFile()
    {
        var (world, serialSystem) = BuildWorld();

        // Pre-purchase a couple of upgrades manually via AddUpgrade.
        var ledger = AddGoldLedger(world, gold: 500f);
        ref var tree = ref world.GetComponent<UpgradeTreeComponent>(ledger);
        tree.AddUpgrade(UpgradeId.HarnessSpeed1);
        tree.AddUpgrade(UpgradeId.Armor1);
        ulong savedFlags = tree.PurchasedFlags;

        AddKing(world);
        AddTavernkeeper(world);

        serialSystem.Save(SaveSlotId.Slot2);

        // Wipe the in-memory flags.
        ref var tree2 = ref world.GetComponent<UpgradeTreeComponent>(ledger);
        tree2.PurchasedFlags = 0UL;

        serialSystem.Load(SaveSlotId.Slot2);

        Assert.Equal(savedFlags,
            world.GetComponent<UpgradeTreeComponent>(ledger).PurchasedFlags);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Save / Load roundtrip — king relationship
    // -------------------------------------------------------------------------

    [Fact]
    public void Load_RestoresKingRelationshipScore_FromFile()
    {
        var (world, serialSystem) = BuildWorld();
        AddGoldLedger(world);
        var king = AddKing(world, score: 72f, runCount: 8);
        AddTavernkeeper(world);

        serialSystem.Save(SaveSlotId.Slot3);

        ref var rel = ref world.GetComponent<KingRelationshipComponent>(king);
        rel.Score        = 0f;
        rel.TotalRunCount = 0;

        serialSystem.Load(SaveSlotId.Slot3);

        var loaded = world.GetComponent<KingRelationshipComponent>(king);
        Assert.Equal(72f, loaded.Score,        precision: 3);
        Assert.Equal(8,   loaded.TotalRunCount);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Save / Load roundtrip — tavernkeeper state
    // -------------------------------------------------------------------------

    [Fact]
    public void Load_RestoresTavernkeeperServiceUnlocks()
    {
        var (world, serialSystem) = BuildWorld();
        AddGoldLedger(world);
        AddKing(world);
        var tk = AddTavernkeeper(world,
            consecutivePleased: 4, medic: true, fence: true, scout: false);

        serialSystem.Save(SaveSlotId.Slot1);

        // Reset in-memory state.
        ref var npc = ref world.GetComponent<TavernkeeperNPCComponent>(tk);
        npc.MedicUnlocked          = false;
        npc.FenceUnlocked          = false;
        npc.ConsecutivePleasedRuns = 0;

        serialSystem.Load(SaveSlotId.Slot1);

        var loaded = world.GetComponent<TavernkeeperNPCComponent>(tk);
        Assert.True(loaded.MedicUnlocked);
        Assert.True(loaded.FenceUnlocked);
        Assert.False(loaded.ScoutUnlocked);
        Assert.Equal(4, loaded.ConsecutivePleasedRuns);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Load from missing file
    // -------------------------------------------------------------------------

    [Fact]
    public void Load_FromNonexistentFile_DoesNotThrow()
    {
        var (world, serialSystem) = BuildWorld();
        AddGoldLedger(world, gold: 100f);
        AddKing(world);
        AddTavernkeeper(world);

        // Should complete without exception.
        var ex = Record.Exception(() => serialSystem.Load(SaveSlotId.Slot2));
        Assert.Null(ex);
        world.Dispose();
    }

    [Fact]
    public void Load_FromNonexistentFile_LeavesGoldUnchanged()
    {
        var (world, serialSystem) = BuildWorld();
        var ledger = AddGoldLedger(world, gold: 100f);
        AddKing(world);
        AddTavernkeeper(world);

        serialSystem.Load(SaveSlotId.Slot2);   // no file exists

        Assert.Equal(100f,
            world.GetComponent<GoldCurrencyComponent>(ledger).TotalGold, precision: 3);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    //  Multiple slots are independent
    // -------------------------------------------------------------------------

    [Fact]
    public void Save_TwoSlots_AreIndependent()
    {
        var (world, serialSystem) = BuildWorld();
        var ledger = AddGoldLedger(world, gold: 300f);
        AddKing(world);
        AddTavernkeeper(world);

        serialSystem.Save(SaveSlotId.Slot1);

        ref var gc = ref world.GetComponent<GoldCurrencyComponent>(ledger);
        gc.TotalGold = 999f;
        serialSystem.Save(SaveSlotId.Slot2);

        // Load Slot1 back — should restore 300 not 999.
        serialSystem.Load(SaveSlotId.Slot1);
        Assert.Equal(300f,
            world.GetComponent<GoldCurrencyComponent>(ledger).TotalGold, precision: 3);

        // Load Slot2 back — should restore 999.
        serialSystem.Load(SaveSlotId.Slot2);
        Assert.Equal(999f,
            world.GetComponent<GoldCurrencyComponent>(ledger).TotalGold, precision: 3);

        world.Dispose();
    }
}
