using REB.Engine.ECS;

namespace REB.Engine.Tavern.Components;

/// <summary>
/// Persistent NPC state for the Tavernkeeper on the entity tagged <c>"Tavernkeeper"</c>.
/// Tracks unlockable services and remembers consecutive run performance to generate tips.
/// </summary>
public struct TavernkeeperNPCComponent : IComponent
{
    // ── Service unlock state ─────────────────────────────────────────────────

    /// <summary>Unlocked after 3 consecutive Pleased King reactions.</summary>
    public bool MedicUnlocked;

    /// <summary>Unlocked after 5 total runs.</summary>
    public bool FenceUnlocked;

    /// <summary>Unlocked when King relationship score reaches Respected (≥ 60).</summary>
    public bool ScoutUnlocked;

    // ── Run memory ───────────────────────────────────────────────────────────

    /// <summary>Number of back-to-back runs where the King reaction was Pleased.  Resets on any non-Pleased result.</summary>
    public int ConsecutivePleasedRuns;

    /// <summary>Line key for the tip the Tavernkeeper will offer this visit. Set by <see cref="Systems.TavernkeeperSystem"/>.</summary>
    public string LastTipLineKey;

    public static TavernkeeperNPCComponent Default => new()
    {
        MedicUnlocked          = false,
        FenceUnlocked          = false,
        ScoutUnlocked          = false,
        ConsecutivePleasedRuns = 0,
        LastTipLineKey         = string.Empty,
    };
}
