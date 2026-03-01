using Microsoft.Xna.Framework;

namespace REB.Engine.UI;

/// <summary>
/// Published by <see cref="Systems.HitFeedbackSystem"/> for each impactful in-game event.
/// Drives screen shake, particle bursts, and gamepad rumble.
/// </summary>
public readonly record struct HitFeedbackEvent(FeedbackType Type, Vector3 Position, float Intensity);
