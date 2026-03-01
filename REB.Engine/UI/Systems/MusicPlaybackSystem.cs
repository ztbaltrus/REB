using Microsoft.Xna.Framework.Media;
using REB.Engine.ECS;
using REB.Engine.UI.Components;

namespace REB.Engine.UI.Systems;

/// <summary>
/// Bridges <see cref="DynamicMusicSystem"/> with FNA's <see cref="MediaPlayer"/>.
/// <para>
/// Call <see cref="LoadSongs"/> from <c>LoadContent()</c> with a dictionary mapping each
/// <see cref="MusicTrack"/> to a preloaded <see cref="Song"/>. Until then all audio is silenced.
/// Volume is applied every frame from the <c>AudioMixer</c> entity's <see cref="AudioMixerComponent"/>.
/// </para>
/// </summary>
[RunAfter(typeof(DynamicMusicSystem))]
public sealed class MusicPlaybackSystem : GameSystem
{
    private Dictionary<MusicTrack, Song>? _songs;
    private MusicTrack _lastTrack = MusicTrack.None;

    /// <summary>
    /// Provides the set of preloaded songs. Safe to call before or after registration.
    /// </summary>
    public void LoadSongs(Dictionary<MusicTrack, Song> songs) => _songs = songs;

    public override void Update(float deltaTime)
    {
        if (_songs == null) return;

        if (!World.TryGetSystem<DynamicMusicSystem>(out var dynMusic)) return;

        // React to track-change events.
        foreach (var ev in dynMusic.AudioEvents)
        {
            if (ev.Track == MusicTrack.None)
            {
                MediaPlayer.Stop();
                _lastTrack = MusicTrack.None;
                continue;
            }

            if (_songs.TryGetValue(ev.Track, out var song))
            {
                MediaPlayer.IsRepeating = true;
                MediaPlayer.Play(song);
                _lastTrack = ev.Track;
            }
        }

        ApplyVolume();
    }

    // =========================================================================
    //  Volume
    // =========================================================================

    private void ApplyVolume()
    {
        float musicVol  = 0.7f;
        float masterVol = 1.0f;

        foreach (var e in World.GetEntitiesWithTag("AudioMixer"))
        {
            if (World.HasComponent<AudioMixerComponent>(e))
            {
                var mix  = World.GetComponent<AudioMixerComponent>(e);
                musicVol  = mix.MusicVolume;
                masterVol = mix.MasterVolume;
            }
            break;
        }

        MediaPlayer.Volume = Math.Clamp(musicVol * masterVol, 0f, 1f);
    }
}
