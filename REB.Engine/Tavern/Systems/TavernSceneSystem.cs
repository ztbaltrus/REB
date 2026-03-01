using REB.Engine.ECS;
using REB.Engine.KingsCourt;
using REB.Engine.KingsCourt.Components;
using REB.Engine.KingsCourt.Systems;
using REB.Engine.Tavern.Components;

namespace REB.Engine.Tavern.Systems;

/// <summary>
/// Manages the Tavern between-run scene.
/// <list type="number">
///   <item>Detects <see cref="KingsCourtPhase.Dismissed"/> on the King entity and opens the Tavern.</item>
///   <item>Resets the King's phase to <see cref="KingsCourtPhase.Inactive"/> to prevent re-triggering.</item>
///   <item>Auto-closes the Tavern after <see cref="TavernStateComponent.OpenDuration"/> elapses.</item>
/// </list>
/// </summary>
[RunAfter(typeof(KingsCourtSceneSystem))]
public sealed class TavernSceneSystem : GameSystem
{
    public override void Update(float deltaTime)
    {
        Entity tavern = FindTavern();
        if (!World.IsAlive(tavern)) return;

        ref var ts = ref World.GetComponent<TavernStateComponent>(tavern);

        if (!ts.SceneActive)
        {
            TryOpenTavern(ref ts);
            return;
        }

        // Advance open-phase timer.
        ts.PhaseTimer += deltaTime;
        if (ts.Phase == TavernPhase.Open && ts.PhaseTimer >= ts.OpenDuration)
            CloseTavern(ref ts);
    }

    // =========================================================================
    //  Open / close
    // =========================================================================

    private void TryOpenTavern(ref TavernStateComponent ts)
    {
        Entity king = FindKing();
        if (!World.IsAlive(king)) return;

        var ks = World.GetComponent<KingStateComponent>(king);
        if (ks.Phase != KingsCourtPhase.Dismissed) return;

        // Open the tavern.
        ts.Phase       = TavernPhase.Open;
        ts.SceneActive = true;
        ts.PhaseTimer  = 0f;

        // Reset King to Inactive so the Tavern doesn't re-open on subsequent frames.
        ref var kingState = ref World.GetComponent<KingStateComponent>(king);
        kingState.Phase = KingsCourtPhase.Inactive;
    }

    private static void CloseTavern(ref TavernStateComponent ts)
    {
        ts.Phase       = TavernPhase.Inactive;
        ts.SceneActive = false;
        ts.PhaseTimer  = 0f;
    }

    // =========================================================================
    //  Helpers
    // =========================================================================

    private Entity FindTavern()
    {
        foreach (var e in World.GetEntitiesWithTag("Tavern"))
            return e;
        return Entity.Null;
    }

    private Entity FindKing()
    {
        foreach (var e in World.GetEntitiesWithTag("King"))
            return e;
        return Entity.Null;
    }
}
