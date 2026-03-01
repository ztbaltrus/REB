using REB.Engine.ECS;
using REB.Engine.KingsCourt;
using REB.Engine.KingsCourt.Components;
using REB.Engine.KingsCourt.Systems;
using REB.Engine.UI.Components;

namespace REB.Engine.UI.Systems;

/// <summary>
/// Populates the <see cref="RunSummaryUIComponent"/> on the frame a payout is calculated.
/// <para>
/// Reads: <c>PayoutCalculationSystem.PayoutEvents</c>, <c>RunSummaryComponent</c>.
/// Writes: <see cref="RunSummaryUIComponent"/> on the entity tagged "RunSummaryUI" (upsert).
/// The <see cref="LastSummary"/> property exposes the most-recently computed values.
/// </para>
/// </summary>
[RunAfter(typeof(PayoutCalculationSystem))]
public sealed class RunSummaryUISystem : GameSystem
{
    /// <summary>The last summary populated by a payout event (empty until first payout).</summary>
    public RunSummaryUIComponent LastSummary { get; private set; } = RunSummaryUIComponent.Default;

    public override void Update(float deltaTime)
    {
        if (!World.TryGetSystem<PayoutCalculationSystem>(out var payoutSystem)) return;
        if (payoutSystem.PayoutEvents.Count == 0) return;

        var ev = payoutSystem.PayoutEvents[0];

        // Pull run-duration from the RunSummary singleton.
        float runDuration = 0f;
        int   lootValue   = 0;

        foreach (var e in World.GetEntitiesWithTag("RunSummary"))
        {
            if (World.HasComponent<RunSummaryComponent>(e))
            {
                var rs    = World.GetComponent<RunSummaryComponent>(e);
                runDuration = rs.RunDurationSeconds;
                lootValue   = (int)rs.LootGoldValue;
            }
            break;
        }

        string reactionLabel = ev.KingReaction switch
        {
            KingReactionState.Pleased      => "Pleased",
            KingReactionState.Neutral      => "Neutral",
            KingReactionState.Dissatisfied => "Dissatisfied",
            KingReactionState.Furious      => "Furious",
            _                              => "Unknown",
        };

        var summary = new RunSummaryUIComponent
        {
            FinalPayout        = ev.FinalPayout,
            TreasureValue      = lootValue,
            RunDurationSeconds = runDuration,
            KingReactionLabel  = reactionLabel,
        };

        LastSummary = summary;

        foreach (var e in World.GetEntitiesWithTag("RunSummaryUI"))
        {
            World.SetComponent(e, summary);
            break;
        }
    }
}
