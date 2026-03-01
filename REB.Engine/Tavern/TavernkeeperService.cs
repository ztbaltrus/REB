namespace REB.Engine.Tavern;

/// <summary>
/// Optional services the Tavernkeeper unlocks based on the crew's run history.
/// Services are unlocked progressively:
/// <list type="bullet">
///   <item><b>Medic</b> — unlocked after 3 consecutive Pleased King reactions.</item>
///   <item><b>Fence</b> — unlocked after 5 total runs.</item>
///   <item><b>Scout</b> — unlocked when King relationship score reaches Respected (≥ 60).</item>
/// </list>
/// </summary>
public enum TavernkeeperService
{
    /// <summary>No service.</summary>
    None,

    /// <summary>Heals the crew between runs (free top-up of health).</summary>
    Medic,

    /// <summary>Sells contraband loot at a premium rate.</summary>
    Fence,

    /// <summary>Provides advance intel on the next floor's layout.</summary>
    Scout,
}
