namespace REB.Engine.RunManagement.Events;

/// <summary>
/// Published by <see cref="Systems.RunManagerSystem"/> on the frame a run ends
/// (RunSummaryComponent.IsComplete becomes true).
/// </summary>
/// <param name="RunNumber">1-based index of the run that just completed.</param>
/// <param name="PrincessDeliveredSafely">
/// True if the princess reached the exit without being dropped at zero health.
/// </param>
public readonly record struct RunCompletedEvent(int RunNumber, bool PrincessDeliveredSafely);
