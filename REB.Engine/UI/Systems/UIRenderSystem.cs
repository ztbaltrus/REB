using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using REB.Engine.ECS;
using REB.Engine.Input;
using REB.Engine.Player;
using REB.Engine.Rendering.Systems;
using REB.Engine.Settings;
using REB.Engine.UI;
using REB.Engine.UI.Components;

namespace REB.Engine.UI.Systems;

/// <summary>
/// SpriteBatch-based 2D overlay renderer.
/// <para>
/// Draws the HUD, all menu screens, the run-summary screen, and the performance
/// overlay text on top of the 3D scene.  All font references are null-safe —
/// nothing is drawn until <see cref="LoadFonts"/> is called (once assets are compiled).
/// </para>
/// <para>
/// Call <see cref="LoadFonts"/> from <c>RoyalErrandBoysGame.LoadContent()</c> after
/// building fonts with the MonoGame Content Pipeline (MGCB).
/// </para>
/// </summary>
[RunAfter(typeof(HUDSystem))]
[RunAfter(typeof(MenuSystem))]
[RunAfter(typeof(RunSummaryUISystem))]
[RunAfter(typeof(RenderSystem))]
public sealed class UIRenderSystem : GameSystem
{
    // =========================================================================
    //  State
    // =========================================================================

    private readonly GraphicsDevice _device;
    private SpriteBatch?            _batch;
    private Texture2D?              _whitePixel;

    private SpriteFont? _hudFont;
    private SpriteFont? _dialogueFont;
    private SpriteFont? _titleFont;

    // Settings-screen navigation state.
    private int _settingsIndex = 0;
    private const int SettingsOptionCount = 6;

    // =========================================================================
    //  Construction
    // =========================================================================

    public UIRenderSystem(GraphicsDevice device)
    {
        _device = device;
    }

    // =========================================================================
    //  Asset loading (called from LoadContent once MGCB output is present)
    // =========================================================================

    /// <summary>Supplies compiled SpriteFont assets. Safe to call with null arguments.</summary>
    public void LoadFonts(SpriteFont? hud, SpriteFont? dialogue, SpriteFont? title)
    {
        _hudFont      = hud;
        _dialogueFont = dialogue;
        _titleFont    = title;
    }

    // =========================================================================
    //  Lifecycle
    // =========================================================================

    protected override void OnInitialize()
    {
        _batch = new SpriteBatch(_device);
        _whitePixel = new Texture2D(_device, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    public override void OnShutdown()
    {
        _whitePixel?.Dispose();
        _batch?.Dispose();
    }

    // =========================================================================
    //  Update — menu / settings input
    // =========================================================================

    public override void Update(float deltaTime)
    {
        if (!World.TryGetSystem<InputSystem>(out var input)) return;
        if (!World.TryGetSystem<MenuSystem>(out var menus)) return;

        var menu = menus!.CurrentState;

        // Enter on MainMenu → start game.
        if (menu == MenuState.MainMenu && input!.IsKeyPressed(Keys.Enter))
            menus.RequestNavigation(MenuState.HUD);

        // Escape toggles Pause.
        if (menu == MenuState.HUD    && input!.IsKeyPressed(Keys.Escape))
            menus.RequestNavigation(MenuState.Paused);
        if (menu == MenuState.Paused && input!.IsKeyPressed(Keys.Escape))
            menus.RequestNavigation(MenuState.HUD);

        // S on Pause → Settings.
        if (menu == MenuState.Paused   && input!.IsKeyPressed(Keys.S))
            menus.RequestNavigation(MenuState.Settings);
        if (menu == MenuState.Settings && input!.IsKeyPressed(Keys.Escape))
            menus.RequestNavigation(MenuState.Paused);

        // RunSummary → Escape back to MainMenu.
        if (menu == MenuState.RunSummary && input!.IsKeyPressed(Keys.Escape))
            menus.RequestNavigation(MenuState.MainMenu);

        // Settings navigation.
        if (menu == MenuState.Settings)
            HandleSettingsInput(input!);
    }

    // =========================================================================
    //  Draw — 2D overlay
    // =========================================================================

    public override void Draw(GameTime gameTime)
    {
        if (_batch == null || _whitePixel == null) return;

        MenuState menuState = GetMenuState();

        _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                     SamplerState.LinearClamp, null, null);
        try
        {
            switch (menuState)
            {
                case MenuState.MainMenu:     DrawMainMenu();     break;
                case MenuState.RoleSelection: DrawRoleSelection(); break;
                case MenuState.Paused:       DrawHUD(); DrawPauseOverlay(); break;
                case MenuState.RunSummary:   DrawRunSummary();   break;
                case MenuState.Settings:     DrawHUD(); DrawSettings(); break;
                case MenuState.Credits:      DrawCredits();      break;
                case MenuState.HUD:          DrawHUD();          break;
            }

            DrawPerformanceText();
        }
        finally
        {
            _batch.End();
        }
    }

    // =========================================================================
    //  HUD
    // =========================================================================

    private void DrawHUD()
    {
        if (_hudFont == null && _whitePixel == null) return;

        HUDDataComponent hud = default;
        foreach (var e in World.GetEntitiesWithTag("HUDData"))
        {
            if (World.HasComponent<HUDDataComponent>(e))
                hud = World.GetComponent<HUDDataComponent>(e);
            break;
        }

        int sw = _device.Viewport.Width;
        int sh = _device.Viewport.Height;
        const int Margin  = 20;
        const int BarW    = 200;
        const int BarH    = 14;
        const int BarGap  = 6;

        int x = Margin;
        int y = sh - Margin - BarH * 2 - BarGap - 20;

        // Princess health bar.
        float hpPct = hud.PrincessMaxHealth > 0 ? hud.PrincessHealth / hud.PrincessMaxHealth : 0f;
        DrawBar(x, y, BarW, BarH, hpPct, Color.DarkRed, Color.Red);
        DrawText(_hudFont, $"HP {(int)hud.PrincessHealth}/{(int)hud.PrincessMaxHealth}",
                 x + BarW + 6, y, Color.White);

        y += BarH + BarGap;

        // Goodwill bar.
        float gwPct = hud.PrincessGoodwill / 100f;
        var   gwCol = gwPct >= 0.7f ? Color.ForestGreen
                    : gwPct <= 0.3f ? Color.OrangeRed
                    : Color.Gold;
        DrawBar(x, y, BarW, BarH, gwPct, Color.DarkGreen, gwCol);
        DrawText(_hudFont, $"Goodwill {(int)hud.PrincessGoodwill}", x + BarW + 6, y, Color.White);

        y += BarH + BarGap;

        // Ability cooldown bar.
        float cdPct = 1f - hud.AbilityCooldownPct;
        DrawBar(x, y, BarW / 2, BarH, cdPct,
                Color.Gray, hud.AbilityCooldownPct < 0.01f ? Color.Cyan : Color.SteelBlue);
        string roleName = hud.CarrierRole == PlayerRole.None ? "No Role" : hud.CarrierRole.ToString();
        DrawText(_hudFont, roleName, x + BarW / 2 + 6, y, Color.White);

        // Top-right: treasure + gold.
        DrawText(_hudFont, $"Treasure: {hud.TreasureValue} g",
                 sw - Margin - 180, Margin,      Color.Goldenrod);
        DrawText(_hudFont, $"Gold: {(int)hud.GoldTotal} g",
                 sw - Margin - 180, Margin + 20, Color.Yellow);

        // Overweight warning.
        if (hud.IsOverweight)
            DrawTextCentered(_hudFont, "OVERWEIGHT", sw / 2, sh / 2 - 60, Color.Red);
    }

    // =========================================================================
    //  Menu screens
    // =========================================================================

    private void DrawMainMenu()
    {
        int sw = _device.Viewport.Width;
        int sh = _device.Viewport.Height;

        // Dark overlay.
        DrawRect(0, 0, sw, sh, Color.Black * 0.7f);

        DrawTextCentered(_titleFont ?? _hudFont, "Royal Errand Boys",
                         sw / 2, sh / 2 - 80, Color.Gold);
        DrawTextCentered(_hudFont, "Press ENTER to Start",
                         sw / 2, sh / 2 + 20, Color.White);
        DrawTextCentered(_hudFont, "WASD / Arrow Keys = Move  |  Mouse = Look  |  E = Interact  |  G = Drop",
                         sw / 2, sh - 40, Color.Gray);
    }

    private void DrawRoleSelection()
    {
        int sw = _device.Viewport.Width;
        int sh = _device.Viewport.Height;

        DrawRect(0, 0, sw, sh, Color.Black * 0.8f);
        DrawTextCentered(_titleFont ?? _hudFont, "Select Your Role", sw / 2, 80, Color.Gold);

        var roles = new[] {
            ("Carrier",    "Carry the princess. Sprint burst suppresses mood penalty. (Cooldown: 10s)"),
            ("Scout",      "Ping nearby rooms for 5 s. (Cooldown: 10s)"),
            ("Treasurer",  "Highlight all loot through walls for 5 s. (Cooldown: 10s)"),
            ("Negotiator", "Halve princess goodwill decay for 15 s. (Cooldown: 10s)"),
        };

        int y = sh / 2 - 60;
        foreach (var (name, desc) in roles)
        {
            DrawTextCentered(_hudFont, $"{name}  —  {desc}", sw / 2, y, Color.LightGray);
            y += 36;
        }

        DrawTextCentered(_hudFont, "Use RoleSelectionSystem to assign roles before starting.",
                         sw / 2, y + 20, Color.DimGray);
    }

    private void DrawPauseOverlay()
    {
        int sw = _device.Viewport.Width;
        int sh = _device.Viewport.Height;

        DrawRect(0, 0, sw, sh, Color.Black * 0.5f);
        DrawTextCentered(_titleFont ?? _hudFont, "PAUSED",       sw / 2, sh / 2 - 40, Color.White);
        DrawTextCentered(_hudFont,               "ESC = Resume", sw / 2, sh / 2 + 10, Color.LightGray);
        DrawTextCentered(_hudFont,               "S = Settings", sw / 2, sh / 2 + 36, Color.LightGray);
    }

    private void DrawRunSummary()
    {
        int sw = _device.Viewport.Width;
        int sh = _device.Viewport.Height;

        DrawRect(0, 0, sw, sh, Color.Black * 0.85f);
        DrawTextCentered(_titleFont ?? _hudFont, "Run Complete!", sw / 2, 100, Color.Gold);

        RunSummaryUIComponent summary = default;
        foreach (var e in World.GetEntitiesWithTag("RunSummaryUI"))
        {
            if (World.HasComponent<RunSummaryUIComponent>(e))
                summary = World.GetComponent<RunSummaryUIComponent>(e);
            break;
        }

        int y = sh / 2 - 60;
        DrawTextCentered(_hudFont, $"King's Reaction: {summary.KingReactionLabel}",
                         sw / 2, y,      Color.LightGoldenrodYellow);
        DrawTextCentered(_hudFont, $"Treasure Delivered: {summary.TreasureValue} g",
                         sw / 2, y + 30, Color.White);
        DrawTextCentered(_hudFont, $"Final Payout: {(int)summary.FinalPayout} g",
                         sw / 2, y + 60, Color.Goldenrod);

        int mins = (int)(summary.RunDurationSeconds / 60);
        int secs = (int)(summary.RunDurationSeconds % 60);
        DrawTextCentered(_hudFont, $"Run Time: {mins}:{secs:D2}",
                         sw / 2, y + 90, Color.LightGray);

        DrawTextCentered(_hudFont, "ESC = Return to Main Menu", sw / 2, sh - 60, Color.DimGray);
    }

    private void DrawSettings()
    {
        int sw = _device.Viewport.Width;
        int sh = _device.Viewport.Height;

        DrawRect(sw / 4, sh / 6, sw / 2, sh * 2 / 3, Color.Black * 0.9f);

        DrawTextCentered(_titleFont ?? _hudFont, "Settings", sw / 2, sh / 6 + 20, Color.Gold);

        SettingsData? cfg = null;
        if (World.TryGetSystem<SettingsSystem>(out var settingsSys))
            cfg = settingsSys!.Settings;

        if (cfg == null || _hudFont == null) return;

        var options = new (string Label, string Value)[]
        {
            ("Master Volume",      $"{cfg.MasterVolume:P0}"),
            ("Music Volume",       $"{cfg.MusicVolume:P0}"),
            ("SFX Volume",         $"{cfg.SfxVolume:P0}"),
            ("Fullscreen",         cfg.IsFullscreen ? "On" : "Off"),
            ("VSync",              cfg.VSync        ? "On" : "Off"),
            ("Mouse Sensitivity",  $"{cfg.MouseSensitivity:F1}"),
        };

        int y = sh / 6 + 70;
        for (int i = 0; i < options.Length; i++)
        {
            bool   selected = i == _settingsIndex;
            var    col      = selected ? Color.Yellow : Color.LightGray;
            string prefix   = selected ? "> " : "  ";
            DrawTextCentered(_hudFont,
                $"{prefix}{options[i].Label}: {options[i].Value}",
                sw / 2, y, col);
            y += 30;
        }

        DrawTextCentered(_hudFont, "Up/Down = Select  |  Left/Right = Adjust  |  ESC = Back",
                         sw / 2, sh - sh / 6 - 20, Color.DimGray);
    }

    private void DrawCredits()
    {
        int sw = _device.Viewport.Width;
        int sh = _device.Viewport.Height;

        DrawRect(0, 0, sw, sh, Color.Black * 0.9f);
        DrawTextCentered(_titleFont ?? _hudFont, "Royal Errand Boys", sw / 2, sh / 2 - 40, Color.Gold);
        DrawTextCentered(_hudFont, "Thank you for playing!", sw / 2, sh / 2 + 10, Color.White);
    }

    // =========================================================================
    //  Performance overlay (reads from PerformanceOverlaySystem if registered)
    // =========================================================================

    private void DrawPerformanceText()
    {
        if (_hudFont == null) return;
        if (!World.TryGetSystem<PerformanceOverlaySystem>(out var perf)) return;

        float fps  = perf!.AverageFps;
        float ms   = perf.LastFrameMs;
        var   col  = perf.AverageFrameMs <= perf.TargetFrameMs ? Color.LimeGreen : Color.Red;
        DrawText(_hudFont, $"FPS: {fps:F0}  Frame: {ms:F1} ms", 8, 8, col);
    }

    // =========================================================================
    //  Settings input
    // =========================================================================

    private void HandleSettingsInput(InputSystem input)
    {
        if (input.IsKeyPressed(Keys.Up))
            _settingsIndex = (_settingsIndex - 1 + SettingsOptionCount) % SettingsOptionCount;
        if (input.IsKeyPressed(Keys.Down))
            _settingsIndex = (_settingsIndex + 1) % SettingsOptionCount;

        bool left  = input.IsKeyPressed(Keys.Left);
        bool right = input.IsKeyPressed(Keys.Right);
        if (!left && !right) return;

        float dir = right ? 1f : -1f;

        if (!World.TryGetSystem<SettingsSystem>(out var settingsSys)) return;
        var cfg = settingsSys!.Settings;

        switch (_settingsIndex)
        {
            case 0: cfg.MasterVolume     = Math.Clamp(cfg.MasterVolume     + dir * 0.1f, 0f, 1f); break;
            case 1: cfg.MusicVolume      = Math.Clamp(cfg.MusicVolume      + dir * 0.1f, 0f, 1f); break;
            case 2: cfg.SfxVolume        = Math.Clamp(cfg.SfxVolume        + dir * 0.1f, 0f, 1f); break;
            case 3: cfg.IsFullscreen     = !cfg.IsFullscreen;                                       break;
            case 4: cfg.VSync            = !cfg.VSync;                                              break;
            case 5: cfg.MouseSensitivity = Math.Clamp(cfg.MouseSensitivity + dir * 0.1f, 0.5f, 3f); break;
        }

        settingsSys.Save();
        settingsSys.Apply();
    }

    // =========================================================================
    //  Primitive draw helpers
    // =========================================================================

    private void DrawBar(int x, int y, int w, int h, float fill, Color background, Color foreground)
    {
        if (_batch == null || _whitePixel == null) return;
        _batch.Draw(_whitePixel, new Rectangle(x, y, w, h), background);
        int fillW = (int)(w * Math.Clamp(fill, 0f, 1f));
        if (fillW > 0)
            _batch.Draw(_whitePixel, new Rectangle(x, y, fillW, h), foreground);
    }

    private void DrawRect(int x, int y, int w, int h, Color color)
    {
        if (_batch == null || _whitePixel == null) return;
        _batch.Draw(_whitePixel, new Rectangle(x, y, w, h), color);
    }

    private void DrawText(SpriteFont? font, string text, int x, int y, Color color)
    {
        if (_batch == null || font == null) return;
        _batch.DrawString(font, text, new Vector2(x, y), color);
    }

    private void DrawTextCentered(SpriteFont? font, string? text, int cx, int y, Color color)
    {
        if (_batch == null || font == null || string.IsNullOrEmpty(text)) return;
        var size = font.MeasureString(text);
        _batch.DrawString(font, text, new Vector2(cx - size.X / 2f, y), color);
    }

    // =========================================================================
    //  State helpers
    // =========================================================================

    private MenuState GetMenuState()
    {
        foreach (var e in World.GetEntitiesWithTag("MenuManager"))
        {
            if (World.HasComponent<MenuManagerComponent>(e))
                return World.GetComponent<MenuManagerComponent>(e).CurrentState;
            break;
        }
        return MenuState.HUD;
    }
}
