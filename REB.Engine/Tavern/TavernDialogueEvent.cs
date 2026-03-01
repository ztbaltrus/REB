using REB.Engine.ECS;

namespace REB.Engine.Tavern;

/// <summary>Fired by <see cref="Systems.TavernkeeperSystem"/> when the Tavernkeeper speaks.</summary>
public readonly record struct TavernDialogueEvent(Entity TavernkeeperEntity, string LineKey);
