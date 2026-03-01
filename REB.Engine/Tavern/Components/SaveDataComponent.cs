using REB.Engine.ECS;

namespace REB.Engine.Tavern.Components;

/// <summary>
/// The serializable data bundle written to disk by <see cref="Systems.SerializationSystem"/>.
/// One entity tagged <c>"SaveData"</c> carries this component at runtime; the system
/// uses it as a staging area before writing JSON.
/// </summary>
public struct SaveDataComponent : IComponent
{
    /// <summary>Which save slot this data belongs to.</summary>
    public SaveSlotId SlotId;

    /// <summary>Gold balance at the time of the last save.</summary>
    public float TotalGold;

    /// <summary>King relationship score at the time of the last save.</summary>
    public float KingRelationshipScore;

    /// <summary>Bitmask snapshot of all purchased upgrades.</summary>
    public ulong PurchasedUpgradesFlags;

    /// <summary>Total completed runs at the time of the last save.</summary>
    public int TotalRunCount;

    /// <summary>Consecutive Pleased-reaction runs at the time of the last save.</summary>
    public int ConsecutivePleasedRuns;

    /// <summary>Whether the Medic service was unlocked at the time of save.</summary>
    public bool MedicUnlocked;

    /// <summary>Whether the Fence service was unlocked at the time of save.</summary>
    public bool FenceUnlocked;

    /// <summary>Whether the Scout service was unlocked at the time of save.</summary>
    public bool ScoutUnlocked;

    /// <summary>Incremented whenever the save-file format changes.</summary>
    public int SaveVersion;

    public static SaveDataComponent Default => new() { SlotId = SaveSlotId.Slot1, SaveVersion = 1 };
}
