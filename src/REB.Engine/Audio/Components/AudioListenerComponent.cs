using Microsoft.Xna.Framework;
using REB.Engine.ECS;

namespace REB.Engine.Audio.Components;

/// <summary>
/// Marks an entity as the 3D audio listener (typically the local player's camera/head).
/// Only one active listener should exist per world; the <see cref="Systems.AudioSystem"/>
/// uses the first it finds.
/// Requires a <see cref="REB.Engine.Rendering.Components.TransformComponent"/> on the same entity
/// so the system can read world-space position and orientation.
/// </summary>
public struct AudioListenerComponent : IComponent
{
    public bool IsActive;

    public static AudioListenerComponent Default => new() { IsActive = true };
}
