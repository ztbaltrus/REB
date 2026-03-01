using REB.Engine.RunManagement.Components;

namespace REB.Engine.RunManagement.Events;

/// <summary>
/// Published by <see cref="Systems.RunManagerSystem"/> on the frame a new run begins.
/// Carries the full run configuration so other systems can react to the run seed.
/// </summary>
/// <param name="RunNumber">1-based index of the run that just started.</param>
/// <param name="Config">Full configuration for this run including all derived seeds.</param>
public readonly record struct RunStartedEvent(int RunNumber, RunConfigComponent Config);
