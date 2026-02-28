namespace REB.Engine.Loot;

/// <summary>
/// Determines the value multiplier applied to an item's base value
/// during loot valuation.
/// <list type="table">
///   <item><term>Common</term>    <description>1× multiplier.</description></item>
///   <item><term>Rare</term>      <description>2× multiplier.</description></item>
///   <item><term>Legendary</term> <description>5× multiplier (7.5× with a Treasurer).</description></item>
///   <item><term>Cursed</term>    <description>0.5× multiplier; carries a negative effect.</description></item>
/// </list>
/// </summary>
public enum ItemRarity
{
    Common,
    Rare,
    Legendary,
    Cursed,
}
