namespace REB.Engine.UI;

/// <summary>
/// Published by <see cref="Systems.DynamicMusicSystem"/> when the active music track changes.
/// Downstream audio systems use this to fade out the old track and start the new one.
/// </summary>
public readonly record struct AudioTriggerEvent(MusicTrack Track);
