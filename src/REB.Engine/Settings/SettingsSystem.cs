using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using REB.Engine.Audio.Systems;
using REB.Engine.ECS;

namespace REB.Engine.Settings;

/// <summary>
/// Loads, saves, and applies player-configurable settings.
/// <para>
/// Settings are persisted to <c>settings.json</c> alongside the executable.
/// Call <see cref="Apply"/> after any change to push values to the graphics device and audio system.
/// </para>
/// </summary>
public sealed class SettingsSystem : GameSystem
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented        = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _filePath;
    private readonly GraphicsDeviceManager _graphics;

    public SettingsData Settings { get; private set; } = new();

    public SettingsSystem(GraphicsDeviceManager graphics, string? filePath = null)
    {
        _graphics = graphics;
        _filePath = filePath ?? Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "settings.json");
    }

    // -------------------------------------------------------------------------
    //  Lifecycle
    // -------------------------------------------------------------------------

    protected override void OnInitialize()
    {
        Settings = Load();
        Apply();
    }

    // -------------------------------------------------------------------------
    //  Persistence
    // -------------------------------------------------------------------------

    public SettingsData Load()
    {
        if (!File.Exists(_filePath))
        {
            Settings = new SettingsData();
            Save();
            return Settings;
        }

        try
        {
            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<SettingsData>(json, JsonOptions)
                   ?? new SettingsData();
        }
        catch
        {
            // Corrupt / unreadable file: fall back to defaults.
            return new SettingsData();
        }
    }

    public void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(Settings, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Non-fatal: settings just won't persist this session.
        }
    }

    // -------------------------------------------------------------------------
    //  Apply
    // -------------------------------------------------------------------------

    /// <summary>Pushes current settings to the graphics device and audio subsystem.</summary>
    public void Apply()
    {
        ApplyDisplay();
        ApplyAudio();
    }

    private void ApplyDisplay()
    {
        _graphics.PreferredBackBufferWidth  = Settings.ResolutionWidth;
        _graphics.PreferredBackBufferHeight = Settings.ResolutionHeight;
        _graphics.IsFullScreen              = Settings.IsFullscreen;
        _graphics.SynchronizeWithVerticalRetrace = Settings.VSync;
        _graphics.ApplyChanges();
    }

    private void ApplyAudio()
    {
        if (World.TryGetSystem<AudioSystem>(out var audio))
            audio!.MasterVolume = Settings.MasterVolume;
    }
}
