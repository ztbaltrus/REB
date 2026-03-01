namespace REB.Engine.UI;

/// <summary>
/// Named music tracks managed by <see cref="Systems.DynamicMusicSystem"/>.
/// Tracks are ordered by ascending priority; higher values win during conflicts.
/// </summary>
public enum MusicTrack
{
    /// <summary>Silence / no music.</summary>
    None        = 0,

    /// <summary>Ambient exploration theme for traversing dungeon corridors.</summary>
    Exploration = 1,

    /// <summary>Energetic loop that activates when enemies are in combat range.</summary>
    Combat      = 2,

    /// <summary>Upbeat theme played during the Tavern scene.</summary>
    Tavern      = 3,

    /// <summary>Ceremonial theme played during the King's Court scene.</summary>
    KingsCourt  = 4,

    /// <summary>Intense stinger that overrides all others during a boss fight.</summary>
    BossEncounter = 5,

    /// <summary>Quiet title-screen loop.</summary>
    MainMenu    = 6,
}
