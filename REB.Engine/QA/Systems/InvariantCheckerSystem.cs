using REB.Engine.Combat.Components;
using REB.Engine.ECS;
using REB.Engine.Rendering.Components;
using REB.Engine.World.Components;

namespace REB.Engine.QA.Systems;

/// <summary>
/// Validates ECS world invariants every frame and publishes a list of violations.
/// Violations are diagnostic — they do not halt execution. Use them in tests and
/// as in-game assertion aids.
/// <para>Checks performed each frame:</para>
/// <list type="bullet">
///   <item>Singleton tags (King, RunSummary, etc.) must have at most one entity.</item>
///   <item><see cref="HealthComponent.CurrentHealth"/> must not exceed <see cref="HealthComponent.MaxHealth"/>.</item>
///   <item><see cref="TransformComponent"/> positions must not contain NaN or Infinity.</item>
///   <item><see cref="RoomComponent"/> dimensions must be positive.</item>
/// </list>
/// </summary>
public sealed class InvariantCheckerSystem : GameSystem
{
    /// <summary>All violations detected in the most recent frame. Cleared at the start of each update.</summary>
    public IReadOnlyList<InvariantViolation> Violations => _violations;

    private readonly List<InvariantViolation> _violations = new();

    // Tags that should have at most one entity in a well-formed world.
    private static readonly string[] SingletonTags =
    [
        "King", "RunSummary", "GoldLedger", "Tavern",
        "Tavernkeeper", "HUDData", "TreasureLedger",
        "DynamicMusic", "AudioMixer", "ScreenShake", "MenuManager",
    ];

    // =========================================================================
    //  Update
    // =========================================================================

    public override void Update(float deltaTime)
    {
        _violations.Clear();
        CheckSingletonTags();
        CheckHealthComponents();
        CheckTransformComponents();
        CheckRoomComponents();
    }

    // =========================================================================
    //  Individual checks
    // =========================================================================

    private void CheckSingletonTags()
    {
        foreach (var tag in SingletonTags)
        {
            int count = 0;
            foreach (var _ in World.GetEntitiesWithTag(tag)) count++;
            if (count > 1)
                Report($"Singleton tag '{tag}' has {count} entities (expected ≤ 1).");
        }
    }

    private void CheckHealthComponents()
    {
        foreach (var e in World.Query<HealthComponent>())
        {
            var hp = World.GetComponent<HealthComponent>(e);
            if (hp.CurrentHealth > hp.MaxHealth + 0.01f)
                Report($"Entity {e}: CurrentHealth ({hp.CurrentHealth:F1}) exceeds MaxHealth ({hp.MaxHealth:F1}).");
        }
    }

    private void CheckTransformComponents()
    {
        foreach (var e in World.Query<TransformComponent>())
        {
            var t = World.GetComponent<TransformComponent>(e);
            if (!IsFinite(t.Position.X) || !IsFinite(t.Position.Y) || !IsFinite(t.Position.Z))
                Report($"Entity {e}: TransformComponent.Position contains NaN or Infinity ({t.Position}).");
        }
    }

    private void CheckRoomComponents()
    {
        foreach (var e in World.Query<RoomComponent>())
        {
            var r = World.GetComponent<RoomComponent>(e);
            if (r.Width <= 0 || r.Height <= 0)
                Report($"Entity {e}: RoomComponent has non-positive size ({r.Width}×{r.Height}).");
        }
    }

    // =========================================================================
    //  Helpers
    // =========================================================================

    private void Report(string description) =>
        _violations.Add(new InvariantViolation(nameof(InvariantCheckerSystem), description));

    private static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
}
