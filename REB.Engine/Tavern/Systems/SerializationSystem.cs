using System.Text.Json;
using REB.Engine.ECS;
using REB.Engine.KingsCourt.Components;
using REB.Engine.Tavern.Components;

namespace REB.Engine.Tavern.Systems;

/// <summary>
/// Saves and loads game state to/from JSON save files.
/// <para>Save data includes: gold balance, king relationship score, purchased upgrades,
/// run count, and Tavernkeeper service unlock state.</para>
/// </summary>
[RunAfter(typeof(GearUpgradeSystem))]
[RunAfter(typeof(TavernkeeperSystem))]
public sealed class SerializationSystem : GameSystem
{
    private readonly string _saveDirectory;

    /// <param name="saveDirectory">
    /// Directory where <c>Slot{N}.json</c> files are written.
    /// Defaults to the process's current working directory.
    /// </param>
    public SerializationSystem(string saveDirectory = "")
    {
        _saveDirectory = string.IsNullOrEmpty(saveDirectory)
            ? Directory.GetCurrentDirectory()
            : saveDirectory;
    }

    // =========================================================================
    //  Public API
    // =========================================================================

    /// <summary>Returns true if a save file exists for the given slot.</summary>
    public bool SaveExists(SaveSlotId slot) => File.Exists(SlotPath(slot));

    /// <summary>
    /// Gathers current world state into a <see cref="SaveDataComponent"/> and writes it to disk.
    /// </summary>
    public void Save(SaveSlotId slot)
    {
        var data = GatherSaveData(slot);
        WriteSaveData(data, slot);
    }

    /// <summary>
    /// Reads save data from disk and distributes it back into the relevant ECS components.
    /// Does nothing if the file does not exist.
    /// </summary>
    public void Load(SaveSlotId slot)
    {
        if (!SaveExists(slot)) return;

        try
        {
            string json = File.ReadAllText(SlotPath(slot));
            var dto = JsonSerializer.Deserialize<SaveDto>(json);
            if (dto is null) return;

            ApplySaveData(dto);
        }
        catch (Exception)
        {
            // Corrupt / incompatible save — silently ignore.
        }
    }

    // SerializationSystem has no per-frame logic; saves are triggered explicitly.
    public override void Update(float deltaTime) { }

    // =========================================================================
    //  Gather / apply
    // =========================================================================

    private SaveDataComponent GatherSaveData(SaveSlotId slot)
    {
        var data = new SaveDataComponent { SlotId = slot, SaveVersion = 1 };

        // Gold ledger.
        Entity ledger = FindTagged("GoldLedger");
        if (World.IsAlive(ledger) && World.HasComponent<GoldCurrencyComponent>(ledger))
        {
            var gc = World.GetComponent<GoldCurrencyComponent>(ledger);
            data.TotalGold = gc.TotalGold;
        }

        if (World.IsAlive(ledger) && World.HasComponent<UpgradeTreeComponent>(ledger))
        {
            data.PurchasedUpgradesFlags = World.GetComponent<UpgradeTreeComponent>(ledger).PurchasedFlags;
        }

        // King relationship.
        Entity king = FindTagged("King");
        if (World.IsAlive(king) && World.HasComponent<KingRelationshipComponent>(king))
        {
            var rel = World.GetComponent<KingRelationshipComponent>(king);
            data.KingRelationshipScore = rel.Score;
            data.TotalRunCount         = rel.TotalRunCount;
        }

        // Tavernkeeper.
        Entity tk = FindTagged("Tavernkeeper");
        if (World.IsAlive(tk) && World.HasComponent<TavernkeeperNPCComponent>(tk))
        {
            var npc = World.GetComponent<TavernkeeperNPCComponent>(tk);
            data.ConsecutivePleasedRuns = npc.ConsecutivePleasedRuns;
            data.MedicUnlocked          = npc.MedicUnlocked;
            data.FenceUnlocked          = npc.FenceUnlocked;
            data.ScoutUnlocked          = npc.ScoutUnlocked;
        }

        return data;
    }

    private void ApplySaveData(SaveDto dto)
    {
        // Gold ledger.
        Entity ledger = FindTagged("GoldLedger");
        if (World.IsAlive(ledger))
        {
            if (World.HasComponent<GoldCurrencyComponent>(ledger))
            {
                ref var gc = ref World.GetComponent<GoldCurrencyComponent>(ledger);
                gc.TotalGold = dto.TotalGold;
            }

            if (World.HasComponent<UpgradeTreeComponent>(ledger))
            {
                ref var tree = ref World.GetComponent<UpgradeTreeComponent>(ledger);
                tree.PurchasedFlags = dto.PurchasedUpgradesFlags;
            }
        }

        // King relationship.
        Entity king = FindTagged("King");
        if (World.IsAlive(king) && World.HasComponent<KingRelationshipComponent>(king))
        {
            ref var rel = ref World.GetComponent<KingRelationshipComponent>(king);
            rel.Score        = dto.KingRelationshipScore;
            rel.TotalRunCount = dto.TotalRunCount;
        }

        // Tavernkeeper.
        Entity tk = FindTagged("Tavernkeeper");
        if (World.IsAlive(tk) && World.HasComponent<TavernkeeperNPCComponent>(tk))
        {
            ref var npc = ref World.GetComponent<TavernkeeperNPCComponent>(tk);
            npc.ConsecutivePleasedRuns = dto.ConsecutivePleasedRuns;
            npc.MedicUnlocked          = dto.MedicUnlocked;
            npc.FenceUnlocked          = dto.FenceUnlocked;
            npc.ScoutUnlocked          = dto.ScoutUnlocked;
        }
    }

    // =========================================================================
    //  Disk I/O
    // =========================================================================

    private void WriteSaveData(SaveDataComponent data, SaveSlotId slot)
    {
        Directory.CreateDirectory(_saveDirectory);

        var dto = new SaveDto
        {
            TotalGold               = data.TotalGold,
            KingRelationshipScore   = data.KingRelationshipScore,
            PurchasedUpgradesFlags  = data.PurchasedUpgradesFlags,
            TotalRunCount           = data.TotalRunCount,
            ConsecutivePleasedRuns  = data.ConsecutivePleasedRuns,
            MedicUnlocked           = data.MedicUnlocked,
            FenceUnlocked           = data.FenceUnlocked,
            ScoutUnlocked           = data.ScoutUnlocked,
            SaveVersion             = data.SaveVersion,
        };

        string json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SlotPath(slot), json);
    }

    private string SlotPath(SaveSlotId slot) =>
        Path.Combine(_saveDirectory, $"Slot{(int)slot}.json");

    // =========================================================================
    //  Helper
    // =========================================================================

    private Entity FindTagged(string tag)
    {
        foreach (var e in World.GetEntitiesWithTag(tag))
            return e;
        return Entity.Null;
    }

    // =========================================================================
    //  DTO — plain serializable type for System.Text.Json
    // =========================================================================

    private sealed class SaveDto
    {
        public float  TotalGold              { get; set; }
        public float  KingRelationshipScore  { get; set; }
        public ulong  PurchasedUpgradesFlags { get; set; }
        public int    TotalRunCount          { get; set; }
        public int    ConsecutivePleasedRuns { get; set; }
        public bool   MedicUnlocked          { get; set; }
        public bool   FenceUnlocked          { get; set; }
        public bool   ScoutUnlocked          { get; set; }
        public int    SaveVersion            { get; set; }
    }
}
