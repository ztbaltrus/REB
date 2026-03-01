using REB.Engine.ECS;

namespace REB.Engine.KingsCourt.Components;

/// <summary>
/// Persistent relationship state between the crew and the King.
/// Attach to the King entity. Updated by <see cref="Systems.KingRelationshipSystem"/>
/// after each payout and persisted by the save system (Epic 8).
/// </summary>
public struct KingRelationshipComponent : IComponent
{
    /// <summary>Relationship score in [0, 100]. Starts at 50 (Known tier).</summary>
    public float Score;

    /// <summary>Relationship tier derived from <see cref="Score"/>.</summary>
    public KingRelationshipTier Tier;

    /// <summary>Total number of runs completed with this crew.</summary>
    public int TotalRunCount;

    // ── Run history ring buffer (last 5 runs) ──────────────────────────────────
    // Fixed-size to avoid heap allocation inside a struct.

    public RunHistoryEntry History0;
    public RunHistoryEntry History1;
    public RunHistoryEntry History2;
    public RunHistoryEntry History3;
    public RunHistoryEntry History4;

    public static KingRelationshipComponent Default => new()
    {
        Score         = 50f,
        Tier          = KingRelationshipTier.Known,
        TotalRunCount = 0,
    };

    /// <summary>Records a run in the ring buffer and increments <see cref="TotalRunCount"/>.</summary>
    public void AddRun(RunHistoryEntry entry)
    {
        switch (TotalRunCount % 5)
        {
            case 0: History0 = entry; break;
            case 1: History1 = entry; break;
            case 2: History2 = entry; break;
            case 3: History3 = entry; break;
            case 4: History4 = entry; break;
        }
        TotalRunCount++;
    }

    /// <summary>Returns the history entry at the given slot index (0–4).</summary>
    public readonly RunHistoryEntry GetHistory(int slot) => slot switch
    {
        0 => History0,
        1 => History1,
        2 => History2,
        3 => History3,
        4 => History4,
        _ => default,
    };

    /// <summary>
    /// Payout multiplier bonus contributed by the current relationship tier.
    /// Applied by <see cref="Systems.PayoutCalculationSystem"/>.
    /// </summary>
    public readonly float TierBonusPercent => Tier switch
    {
        KingRelationshipTier.Beloved   =>  20f,
        KingRelationshipTier.Respected =>  10f,
        KingRelationshipTier.Known     =>   0f,
        KingRelationshipTier.Suspected => -10f,
        KingRelationshipTier.Despised  => -25f,
        _                              =>   0f,
    };
}
