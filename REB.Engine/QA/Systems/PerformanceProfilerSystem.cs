using REB.Engine.ECS;
using REB.Engine.QA.Components;
using REB.Engine.Rendering.Systems;

namespace REB.Engine.QA.Systems;

/// <summary>
/// Extends the base performance overlay with frame-budget tracking and consecutive
/// over-budget warning events.
/// <para>
/// Reads frame timing from <see cref="PerformanceOverlaySystem"/> when registered,
/// falling back to <c>deltaTime × 1000</c> for headless test contexts.
/// </para>
/// <para>
/// Writes a <see cref="FrameBudgetComponent"/> to the singleton entity tagged
/// <c>"Profiler"</c>, creating it on first use.
/// </para>
/// </summary>
[RunAfter(typeof(PerformanceOverlaySystem))]
public sealed class PerformanceProfilerSystem : GameSystem
{
    // =========================================================================
    //  Configuration
    // =========================================================================

    /// <summary>Target frame budget in milliseconds (default 16.67 ms = 60 fps).</summary>
    public float TargetFrameMs { get; set; } = 16.67f;

    /// <summary>
    /// Number of consecutive over-budget frames before a warning string is added to
    /// <see cref="BudgetWarnings"/>. Default is 5.
    /// </summary>
    public int WarningThresholdFrames { get; set; } = 5;

    // =========================================================================
    //  Public telemetry
    // =========================================================================

    /// <summary>True when the most recent frame exceeded <see cref="TargetFrameMs"/>.</summary>
    public bool IsOverBudget { get; private set; }

    /// <summary>Number of consecutive frames that have exceeded the target.</summary>
    public int ConsecutiveOverBudgetFrames { get; private set; }

    /// <summary>Total frames that have exceeded the target since initialization.</summary>
    public int TotalOverBudgetFrames { get; private set; }

    /// <summary>Worst (longest) single frame duration observed since initialization (ms).</summary>
    public float WorstFrameMs { get; private set; }

    /// <summary>Budget warning messages generated this frame. Cleared each update.</summary>
    public IReadOnlyList<string> BudgetWarnings => _warnings;

    private readonly List<string> _warnings = new();
    private Entity _profilerEntity = Entity.Null;

    // =========================================================================
    //  Update
    // =========================================================================

    public override void Update(float deltaTime)
    {
        _warnings.Clear();

        float frameMs = SampleFrameMs(deltaTime);

        IsOverBudget = frameMs > TargetFrameMs;

        if (IsOverBudget)
        {
            ConsecutiveOverBudgetFrames++;
            TotalOverBudgetFrames++;
            if (frameMs > WorstFrameMs) WorstFrameMs = frameMs;

            if (ConsecutiveOverBudgetFrames == WarningThresholdFrames)
                _warnings.Add(
                    $"PERF: Frame budget exceeded for {ConsecutiveOverBudgetFrames} consecutive frames " +
                    $"(last={frameMs:F2}ms, budget={TargetFrameMs:F2}ms).");
        }
        else
        {
            ConsecutiveOverBudgetFrames = 0;
        }

        WriteToEntity();
    }

    // =========================================================================
    //  Helpers
    // =========================================================================

    private float SampleFrameMs(float deltaTime)
    {
        if (World.TryGetSystem<PerformanceOverlaySystem>(out var overlay))
            return overlay.LastFrameMs;

        // Headless fallback: convert deltaTime to ms.
        return deltaTime * 1000f;
    }

    private void WriteToEntity()
    {
        Entity e = FindOrCreateProfilerEntity();
        ref var budget = ref World.GetComponent<FrameBudgetComponent>(e);
        budget.TargetFrameMs               = TargetFrameMs;
        budget.IsOverBudget                = IsOverBudget;
        budget.ConsecutiveOverBudgetFrames = ConsecutiveOverBudgetFrames;
        budget.TotalOverBudgetFrames       = TotalOverBudgetFrames;
        budget.WorstFrameMs                = WorstFrameMs;
    }

    private Entity FindOrCreateProfilerEntity()
    {
        if (World.IsAlive(_profilerEntity)) return _profilerEntity;

        foreach (var e in World.GetEntitiesWithTag("Profiler"))
        {
            _profilerEntity = e;
            return e;
        }

        _profilerEntity = World.CreateEntity();
        World.AddTag(_profilerEntity, "Profiler");
        World.AddComponent(_profilerEntity, FrameBudgetComponent.Default);
        return _profilerEntity;
    }
}
