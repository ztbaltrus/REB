using REB.Engine.ECS;
using REB.Engine.KingsCourt;
using REB.Engine.KingsCourt.Components;
using REB.Engine.Tavern.Components;

namespace REB.Engine.Tavern.Systems;

/// <summary>
/// Manages the Tavernkeeper NPC on the entity tagged <c>"Tavernkeeper"</c>.
/// <para>On the frame the Tavern opens:</para>
/// <list type="number">
///   <item>Updates <see cref="TavernkeeperNPCComponent.ConsecutivePleasedRuns"/> based on the King's last reaction.</item>
///   <item>Checks service unlock thresholds and sets the corresponding flags.</item>
///   <item>Generates a run-history tip and fires welcome + tip <see cref="TavernDialogueEvent"/>s.</item>
/// </list>
/// </summary>
[RunAfter(typeof(TavernSceneSystem))]
public sealed class TavernkeeperSystem : GameSystem
{
    // ── Public events ─────────────────────────────────────────────────────────

    /// <summary>Dialogue events fired this frame. Cleared at the start of each update.</summary>
    public IReadOnlyList<TavernDialogueEvent> DialogueEvents => _dialogue;

    private readonly List<TavernDialogueEvent> _dialogue = new();

    // ── Frame transition tracking ─────────────────────────────────────────────

    private bool _wasOpen;

    // =========================================================================
    //  Update
    // =========================================================================

    public override void Update(float deltaTime)
    {
        _dialogue.Clear();

        Entity tavern = FindTavern();
        bool isOpen = World.IsAlive(tavern) &&
                      World.GetComponent<TavernStateComponent>(tavern).SceneActive;

        bool justOpened = isOpen && !_wasOpen;
        _wasOpen = isOpen;

        if (!justOpened) return;

        Entity tk = FindTavernkeeper();
        if (!World.IsAlive(tk)) return;

        ref var npc = ref World.GetComponent<TavernkeeperNPCComponent>(tk);

        // Update consecutive pleased counter from King's last reaction.
        UpdatePleasedCounter(ref npc);

        // Check service unlocks.
        UpdateServiceUnlocks(ref npc);

        // Generate tip and fire dialogue.
        npc.LastTipLineKey = GenerateTip();
        _dialogue.Add(new TavernDialogueEvent(tk, "tavernkeeper.welcome"));
        _dialogue.Add(new TavernDialogueEvent(tk, npc.LastTipLineKey));
    }

    // =========================================================================
    //  Pleased counter
    // =========================================================================

    private void UpdatePleasedCounter(ref TavernkeeperNPCComponent npc)
    {
        Entity king = FindKing();
        if (!World.IsAlive(king)) return;

        var ks = World.GetComponent<KingStateComponent>(king);

        if (ks.ReactionState == KingReactionState.Pleased)
            npc.ConsecutivePleasedRuns++;
        else
            npc.ConsecutivePleasedRuns = 0;
    }

    // =========================================================================
    //  Service unlock checks
    // =========================================================================

    private void UpdateServiceUnlocks(ref TavernkeeperNPCComponent npc)
    {
        // Medic: 3 consecutive Pleased reactions.
        if (!npc.MedicUnlocked && npc.ConsecutivePleasedRuns >= 3)
        {
            npc.MedicUnlocked = true;
            _dialogue.Add(new TavernDialogueEvent(FindTavernkeeper(), "tavernkeeper.unlock.medic"));
        }

        // Fence: 5 total runs (read from KingRelationshipComponent).
        Entity king = FindKing();
        if (World.IsAlive(king) && World.HasComponent<KingRelationshipComponent>(king))
        {
            var rel = World.GetComponent<KingRelationshipComponent>(king);

            if (!npc.FenceUnlocked && rel.TotalRunCount >= 5)
            {
                npc.FenceUnlocked = true;
                _dialogue.Add(new TavernDialogueEvent(FindTavernkeeper(), "tavernkeeper.unlock.fence"));
            }

            // Scout: relationship score ≥ 60 (Respected tier).
            if (!npc.ScoutUnlocked && rel.Score >= 60f)
            {
                npc.ScoutUnlocked = true;
                _dialogue.Add(new TavernDialogueEvent(FindTavernkeeper(), "tavernkeeper.unlock.scout"));
            }
        }
    }

    // =========================================================================
    //  Tip generation
    // =========================================================================

    private string GenerateTip()
    {
        // Read the most recent run history entry for context-aware tips.
        Entity king = FindKing();
        if (!World.IsAlive(king) || !World.HasComponent<KingRelationshipComponent>(king))
            return "tavernkeeper.tip.general";

        var rel = World.GetComponent<KingRelationshipComponent>(king);
        if (rel.TotalRunCount == 0) return "tavernkeeper.tip.first_run";

        var last = rel.GetHistory((rel.TotalRunCount - 1) % 5);

        if (!last.PrincessDelivered)        return "tavernkeeper.tip.princess_delivery";
        if (last.PrincessHealth < 50f)      return "tavernkeeper.tip.princess_care";
        if (last.LootGoldValue < 100f)      return "tavernkeeper.tip.gather_loot";
        if (!last.BossDefeated)             return "tavernkeeper.tip.boss";

        return "tavernkeeper.tip.keep_it_up";
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

    private Entity FindTavernkeeper()
    {
        foreach (var e in World.GetEntitiesWithTag("Tavernkeeper"))
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
