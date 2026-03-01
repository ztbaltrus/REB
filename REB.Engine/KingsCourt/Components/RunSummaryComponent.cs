using REB.Engine.ECS;

namespace REB.Engine.KingsCourt.Components;

/// <summary>
/// A snapshot of run performance compiled at the end of each dungeon run.
/// Attach to a dedicated entity tagged <c>"RunSummary"</c>.
/// Populated by the run-management system (Epic 8) or manually set by tests.
/// Consumed by <see cref="Systems.KingsCourtSceneSystem"/> and
/// <see cref="Systems.PayoutCalculationSystem"/>.
/// </summary>
public struct RunSummaryComponent : IComponent
{
    /// <summary>Total effective gold value of all loot collected by the party.</summary>
    public float LootGoldValue;

    /// <summary>Number of individual loot items collected.</summary>
    public int LootItemCount;

    /// <summary>Princess hit-point total at the moment the run ended (0–100).</summary>
    public float PrincessHealth;

    /// <summary>Princess goodwill score at run end (0–100).</summary>
    public float PrincessGoodwill;

    /// <summary>True if the princess reached the exit without being dropped at zero health.</summary>
    public bool PrincessDeliveredSafely;

    /// <summary>Number of times the princess was dropped during the run.</summary>
    public int PrincessDropCount;

    /// <summary>True if at least one boss was defeated this run.</summary>
    public bool BossDefeated;

    /// <summary>Total run duration in seconds (wall-clock from first floor entry to exit).</summary>
    public float RunDurationSeconds;

    /// <summary>
    /// Set to true when all run data is populated and the court scene may begin.
    /// Watched by <see cref="Systems.KingsCourtSceneSystem"/>.
    /// </summary>
    public bool IsComplete;

    public static RunSummaryComponent Empty => new()
    {
        LootGoldValue          = 0f,
        LootItemCount          = 0,
        PrincessHealth         = 100f,
        PrincessGoodwill       = 50f,
        PrincessDeliveredSafely = false,
        PrincessDropCount      = 0,
        BossDefeated           = false,
        RunDurationSeconds     = 0f,
        IsComplete             = false,
    };
}
