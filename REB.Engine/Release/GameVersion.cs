namespace REB.Engine.Release;

/// <summary>
/// Static version constants for Royal Errand Boys.
/// Bump Major on breaking saves/netcode, Minor on new features, Patch on bug fixes.
/// </summary>
public static class GameVersion
{
    public const int    Major    = 1;
    public const int    Minor    = 0;
    public const int    Patch    = 0;
    public const string BuildTag = "ship";

    /// <summary>Full semver string — e.g. <c>1.0.0-ship</c>.</summary>
    public static string Version => $"{Major}.{Minor}.{Patch}-{BuildTag}";

    /// <summary>Display title shown in UI, credits, and platform store.</summary>
    public const string GameTitle = "Royal Errand Boys";

    /// <summary>Studio name for credits and store pages.</summary>
    public const string StudioName = "REB Studio";
}
