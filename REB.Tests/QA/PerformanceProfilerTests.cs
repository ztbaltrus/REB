using REB.Engine.ECS;
using REB.Engine.QA.Components;
using REB.Engine.QA.Systems;
using Xunit;

namespace REB.Tests.QA;

// ---------------------------------------------------------------------------
//  PerformanceProfilerSystem tests
//  Uses the deltaTime fallback path (no PerformanceOverlaySystem registered)
//  so that we can control the simulated frame time exactly.
// ---------------------------------------------------------------------------

public sealed class PerformanceProfilerTests
{
    // =========================================================================
    //  Helpers
    // =========================================================================

    private static (World world, PerformanceProfilerSystem profiler) BuildWorld(
        float targetFrameMs = 16.67f)
    {
        var world    = new World();
        var profiler = new PerformanceProfilerSystem { TargetFrameMs = targetFrameMs };
        world.RegisterSystem(profiler);
        return (world, profiler);
    }

    // =========================================================================
    //  Initial state
    // =========================================================================

    [Fact]
    public void NotOverBudget_Initially()
    {
        var (world, profiler) = BuildWorld();

        // deltaTime = 0.010 s → 10 ms < 16.67 ms target.
        world.Update(0.010f);

        Assert.False(profiler.IsOverBudget);
        world.Dispose();
    }

    [Fact]
    public void BudgetWarnings_Empty_WhenUnderBudget()
    {
        var (world, profiler) = BuildWorld();
        world.Update(0.010f);

        Assert.Empty(profiler.BudgetWarnings);
        world.Dispose();
    }

    // =========================================================================
    //  Over-budget detection
    // =========================================================================

    [Fact]
    public void IsOverBudget_True_WhenFrameExceedsTarget()
    {
        // Target = 10 ms; deltaTime = 0.020 s → 20 ms > 10 ms.
        var (world, profiler) = BuildWorld(targetFrameMs: 10f);
        world.Update(0.020f);

        Assert.True(profiler.IsOverBudget);
        world.Dispose();
    }

    [Fact]
    public void TotalOverBudgetFrames_IncrementsWhenOverBudget()
    {
        var (world, profiler) = BuildWorld(targetFrameMs: 10f);
        world.Update(0.020f);
        world.Update(0.020f);

        Assert.Equal(2, profiler.TotalOverBudgetFrames);
        world.Dispose();
    }

    [Fact]
    public void ConsecutiveFrames_ResetsAfterUnderBudgetFrame()
    {
        var (world, profiler) = BuildWorld(targetFrameMs: 10f);
        world.Update(0.020f);   // over
        world.Update(0.020f);   // over → consecutive = 2

        Assert.Equal(2, profiler.ConsecutiveOverBudgetFrames);

        world.Update(0.005f);   // under → consecutive resets
        Assert.Equal(0, profiler.ConsecutiveOverBudgetFrames);
        world.Dispose();
    }

    // =========================================================================
    //  Worst frame tracking
    // =========================================================================

    [Fact]
    public void WorstFrameMs_TracksHighestObservedFrameTime()
    {
        var (world, profiler) = BuildWorld(targetFrameMs: 10f);
        world.Update(0.020f);   // 20 ms
        world.Update(0.050f);   // 50 ms — worst
        world.Update(0.030f);   // 30 ms

        Assert.Equal(50f, profiler.WorstFrameMs, precision: 1);
        world.Dispose();
    }

    // =========================================================================
    //  Warning threshold
    // =========================================================================

    [Fact]
    public void BudgetWarning_GeneratedAfterThresholdConsecutiveFrames()
    {
        var (world, profiler) = BuildWorld(targetFrameMs: 10f);
        profiler.WarningThresholdFrames = 3;

        world.Update(0.020f);
        world.Update(0.020f);
        Assert.Empty(profiler.BudgetWarnings);  // not yet — only 2 consecutive

        world.Update(0.020f);
        Assert.Single(profiler.BudgetWarnings);
        world.Dispose();
    }

    [Fact]
    public void BudgetWarnings_ClearedEachFrame()
    {
        var (world, profiler) = BuildWorld(targetFrameMs: 10f);
        profiler.WarningThresholdFrames = 1;

        world.Update(0.020f);
        Assert.Single(profiler.BudgetWarnings);

        world.Update(0.020f);   // 2nd consecutive — no new warning at threshold 1
        // Warnings cleared; no new warning because we already crossed threshold
        Assert.Empty(profiler.BudgetWarnings);
        world.Dispose();
    }

    // =========================================================================
    //  Entity / component integration
    // =========================================================================

    [Fact]
    public void ProfilerEntity_CreatedWithTag()
    {
        var (world, profiler) = BuildWorld();
        world.Update(0.010f);

        Assert.Single(world.GetEntitiesWithTag("Profiler"));
        world.Dispose();
    }

    [Fact]
    public void FrameBudgetComponent_IsUpdatedEachFrame()
    {
        var (world, profiler) = BuildWorld(targetFrameMs: 10f);
        world.Update(0.020f);   // over budget

        var e      = world.GetEntitiesWithTag("Profiler").First();
        var budget = world.GetComponent<FrameBudgetComponent>(e);

        Assert.True(budget.IsOverBudget);
        Assert.Equal(1, budget.TotalOverBudgetFrames);
        world.Dispose();
    }
}
