namespace REB.Engine.Settings;

/// <summary>
/// Persistent player-facing settings. Serialized to/from <c>settings.json</c> by <see cref="SettingsSystem"/>.
/// Add fields here as new settings are needed; the JSON serializer handles missing fields gracefully.
/// </summary>
public sealed class SettingsData
{
    // -------------------------------------------------------------------------
    //  Display
    // -------------------------------------------------------------------------

    public int  ResolutionWidth  { get; set; } = 1920;
    public int  ResolutionHeight { get; set; } = 1080;
    public bool IsFullscreen     { get; set; } = false;
    public bool VSync            { get; set; } = true;

    // -------------------------------------------------------------------------
    //  Audio
    // -------------------------------------------------------------------------

    public float MasterVolume { get; set; } = 1.0f;
    public float MusicVolume  { get; set; } = 0.8f;
    public float SfxVolume    { get; set; } = 1.0f;

    // -------------------------------------------------------------------------
    //  Input
    // -------------------------------------------------------------------------

    /// <summary>Index of the preferred gamepad slot (0–3). -1 = keyboard/mouse only.</summary>
    public int PreferredGamepadSlot { get; set; } = 0;

    /// <summary>Mouse sensitivity multiplier for camera look.</summary>
    public float MouseSensitivity { get; set; } = 1.0f;

    public bool InvertMouseY { get; set; } = false;
}
