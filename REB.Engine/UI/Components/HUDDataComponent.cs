using REB.Engine.ECS;
using REB.Engine.Player;

namespace REB.Engine.UI.Components;

/// <summary>
/// Frame-accurate HUD snapshot computed by <see cref="Systems.HUDSystem"/>.
/// Attach to an entity tagged "HUDData"; the system performs an upsert each frame.
/// UI rendering reads this component — no direct system dependencies required.
/// </summary>
public struct HUDDataComponent : IComponent
{
    /// <summary>Princess's current health.</summary>
    public float PrincessHealth;

    /// <summary>Princess's maximum health (for filling the health bar).</summary>
    public float PrincessMaxHealth;

    /// <summary>Princess goodwill score (0–100).</summary>
    public float PrincessGoodwill;

    /// <summary>Total effective gold value of loot currently held by the party.</summary>
    public int TreasureValue;

    /// <summary>Role of the first active player (used for the ability-icon display).</summary>
    public PlayerRole CarrierRole;

    /// <summary>Ability cooldown fraction in [0, 1]; 0 = ready, 1 = just used.</summary>
    public float AbilityCooldownPct;

    /// <summary>True when the first player's inventory weight exceeds the carry limit.</summary>
    public bool IsOverweight;

    /// <summary>Current gold balance shown in the HUD corner.</summary>
    public float GoldTotal;

    public static HUDDataComponent Default => default;
}
