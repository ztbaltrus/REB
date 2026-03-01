namespace REB.Engine.QA;

/// <summary>
/// Describes a single ECS world invariant violation detected by
/// <see cref="Systems.InvariantCheckerSystem"/>.
/// </summary>
/// <param name="SystemName">Name of the checker subsystem that raised the violation.</param>
/// <param name="Description">Human-readable description of what was wrong.</param>
public readonly record struct InvariantViolation(string SystemName, string Description);
