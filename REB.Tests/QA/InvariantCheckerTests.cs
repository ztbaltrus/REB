using Microsoft.Xna.Framework;
using REB.Engine.Combat.Components;
using REB.Engine.ECS;
using REB.Engine.QA;
using REB.Engine.QA.Systems;
using REB.Engine.Rendering.Components;
using REB.Engine.World;
using REB.Engine.World.Components;
using Xunit;

namespace REB.Tests.QA;

// ---------------------------------------------------------------------------
//  InvariantCheckerSystem tests
//  Verifies that the system correctly detects and reports world invariant
//  violations and passes clean worlds without false positives.
// ---------------------------------------------------------------------------

public sealed class InvariantCheckerTests
{
    // =========================================================================
    //  Helpers
    // =========================================================================

    private static (World world, InvariantCheckerSystem checker) BuildWorld()
    {
        var world   = new World();
        var checker = new InvariantCheckerSystem();
        world.RegisterSystem(checker);
        return (world, checker);
    }

    // =========================================================================
    //  Clean-world sanity
    // =========================================================================

    [Fact]
    public void NoViolations_InEmptyWorld()
    {
        var (world, checker) = BuildWorld();

        world.Update(0.016f);

        Assert.Empty(checker.Violations);
        world.Dispose();
    }

    [Fact]
    public void NoViolations_WithFullHealthEntities()
    {
        var (world, checker) = BuildWorld();
        var e = world.CreateEntity();
        world.AddComponent(e, HealthComponent.For(100f));

        world.Update(0.016f);

        Assert.Empty(checker.Violations);
        world.Dispose();
    }

    [Fact]
    public void NoViolations_WithValidTransform()
    {
        var (world, checker) = BuildWorld();
        var e = world.CreateEntity();
        world.AddComponent(e, new TransformComponent { Position = new Vector3(1f, 2f, 3f) });

        world.Update(0.016f);

        Assert.Empty(checker.Violations);
        world.Dispose();
    }

    // =========================================================================
    //  Singleton tag check
    // =========================================================================

    [Fact]
    public void Violation_WhenSingletonTagHasMultipleEntities()
    {
        var (world, checker) = BuildWorld();

        // Two "King" entities — invariant violated.
        var k1 = world.CreateEntity();
        world.AddTag(k1, "King");
        var k2 = world.CreateEntity();
        world.AddTag(k2, "King");

        world.Update(0.016f);

        Assert.Contains(checker.Violations, v =>
            v.SystemName == nameof(InvariantCheckerSystem) &&
            v.Description.Contains("King"));
        world.Dispose();
    }

    [Fact]
    public void NoViolation_WhenSingletonTagHasOneEntity()
    {
        var (world, checker) = BuildWorld();
        var king = world.CreateEntity();
        world.AddTag(king, "King");

        world.Update(0.016f);

        Assert.DoesNotContain(checker.Violations, v => v.Description.Contains("King"));
        world.Dispose();
    }

    // =========================================================================
    //  Health check
    // =========================================================================

    [Fact]
    public void Violation_WhenCurrentHealthExceedsMax()
    {
        var (world, checker) = BuildWorld();
        var e = world.CreateEntity();
        world.AddComponent(e, new HealthComponent
        {
            MaxHealth     = 100f,
            CurrentHealth = 150f,   // over max
        });

        world.Update(0.016f);

        Assert.Contains(checker.Violations, v =>
            v.Description.Contains("CurrentHealth"));
        world.Dispose();
    }

    [Fact]
    public void NoViolation_WhenCurrentHealthEqualsMax()
    {
        var (world, checker) = BuildWorld();
        var e = world.CreateEntity();
        world.AddComponent(e, HealthComponent.For(100f));

        world.Update(0.016f);

        Assert.DoesNotContain(checker.Violations, v =>
            v.Description.Contains("CurrentHealth"));
        world.Dispose();
    }

    // =========================================================================
    //  Transform NaN check
    // =========================================================================

    [Fact]
    public void Violation_WhenTransformHasNaN()
    {
        var (world, checker) = BuildWorld();
        var e = world.CreateEntity();
        world.AddComponent(e, new TransformComponent
        {
            Position = new Vector3(float.NaN, 0f, 0f)
        });

        world.Update(0.016f);

        Assert.Contains(checker.Violations, v =>
            v.Description.Contains("NaN"));
        world.Dispose();
    }

    [Fact]
    public void Violation_WhenTransformHasInfinity()
    {
        var (world, checker) = BuildWorld();
        var e = world.CreateEntity();
        world.AddComponent(e, new TransformComponent
        {
            Position = new Vector3(float.PositiveInfinity, 0f, 0f)
        });

        world.Update(0.016f);

        Assert.Contains(checker.Violations, v =>
            v.Description.Contains("Infinity"));
        world.Dispose();
    }

    // =========================================================================
    //  Room dimensions check
    // =========================================================================

    [Fact]
    public void Violation_WhenRoomHasZeroWidth()
    {
        var (world, checker) = BuildWorld();
        var e = world.CreateEntity();
        world.AddComponent(e, new RoomComponent { Width = 0, Height = 4, Theme = FloorTheme.Dungeon });

        world.Update(0.016f);

        Assert.Contains(checker.Violations, v =>
            v.Description.Contains("0×4"));
        world.Dispose();
    }

    [Fact]
    public void NoViolation_WithValidRoom()
    {
        var (world, checker) = BuildWorld();
        var e = world.CreateEntity();
        world.AddComponent(e, new RoomComponent { Width = 6, Height = 4, Theme = FloorTheme.Dungeon });

        world.Update(0.016f);

        Assert.DoesNotContain(checker.Violations, v =>
            v.Description.Contains("RoomComponent"));
        world.Dispose();
    }

    // =========================================================================
    //  Violations cleared each frame
    // =========================================================================

    [Fact]
    public void Violations_ClearedEachFrame()
    {
        var (world, checker) = BuildWorld();

        // Create violation.
        var e = world.CreateEntity();
        world.AddComponent(e, new TransformComponent
        {
            Position = new Vector3(float.NaN, 0f, 0f)
        });

        world.Update(0.016f);
        Assert.NotEmpty(checker.Violations);

        // Fix the violation.
        ref var t = ref world.GetComponent<TransformComponent>(e);
        t.Position = new Vector3(1f, 0f, 0f);

        world.Update(0.016f);
        Assert.Empty(checker.Violations);
        world.Dispose();
    }

    // =========================================================================
    //  InvariantViolation record
    // =========================================================================

    [Fact]
    public void InvariantViolation_HasCorrectSystemName()
    {
        var (world, checker) = BuildWorld();
        var k1 = world.CreateEntity(); world.AddTag(k1, "King");
        var k2 = world.CreateEntity(); world.AddTag(k2, "King");

        world.Update(0.016f);

        Assert.All(checker.Violations, v =>
            Assert.Equal(nameof(InvariantCheckerSystem), v.SystemName));
        world.Dispose();
    }
}
