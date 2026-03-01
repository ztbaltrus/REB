using REB.Engine.ECS;

namespace REB.Engine.QA.Components;

/// <summary>
/// Frame-budget tracking data written each frame by
/// <see cref="Systems.PerformanceProfilerSystem"/>.
/// Attach this to the singleton entity tagged <c>"Profiler"</c>.
/// </summary>
public struct FrameBudgetComponent : IComponent
{
    /// <summary>Target frame time in milliseconds (e.g. 16.67 for 60 fps).</summary>
    public float TargetFrameMs;

    /// <summary>True when the most recent frame exceeded <see cref="TargetFrameMs"/>.</summary>
    public bool IsOverBudget;

    /// <summary>Number of consecutive frames that have exceeded the target.</summary>
    public int ConsecutiveOverBudgetFrames;

    /// <summary>Total frames that have exceeded the target since system initialization.</summary>
    public int TotalOverBudgetFrames;

    /// <summary>Longest individual frame time observed since initialization (milliseconds).</summary>
    public float WorstFrameMs;

    public static FrameBudgetComponent Default => new()
    {
        TargetFrameMs              = 16.67f,
        IsOverBudget               = false,
        ConsecutiveOverBudgetFrames = 0,
        TotalOverBudgetFrames      = 0,
        WorstFrameMs               = 0f,
    };
}
