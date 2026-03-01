using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using REB.Engine.ECS;
using REB.Engine.KingsCourt;
using REB.Engine.KingsCourt.Systems;
using REB.Engine.Player.Princess;
using REB.Engine.Player.Princess.Systems;
using REB.Engine.Tavern;
using REB.Engine.Tavern.Systems;

namespace REB.Engine.UI.Systems;

/// <summary>
/// Displays one subtitle at a time, centred near the bottom of the screen.
/// Drains events from <see cref="KingsCourtSceneSystem"/>, <see cref="NegotiationMinigameSystem"/>,
/// <see cref="TavernkeeperSystem"/>, and <see cref="MoodReactionSystem"/> into an internal queue.
/// Each entry is shown for <see cref="DisplaySeconds"/> seconds and fades in the final 0.5 s.
/// </summary>
[RunAfter(typeof(KingsCourtSceneSystem))]
[RunAfter(typeof(NegotiationMinigameSystem))]
[RunAfter(typeof(TavernkeeperSystem))]
[RunAfter(typeof(MoodReactionSystem))]
public sealed class DialogueSubtitleSystem : GameSystem
{
    private const float DisplaySeconds = 4f;
    private const float FadeStartAt    = 0.5f;  // seconds before expiry to begin fade

    private SpriteFont?  _font;
    private SpriteBatch? _batch;

    private readonly Queue<SubtitleEntry> _queue = new();
    private SubtitleEntry? _active;

    // =========================================================================
    //  English string table (48 dialogue keys)
    // =========================================================================

    private static readonly Dictionary<string, string> Lines = new(StringComparer.Ordinal)
    {
        // ----- King arrival -----
        ["king.pleased.arrival.1"]         = "Ah, at last! I trust the errand went smoothly?",
        ["king.pleased.arrival.2"]         = "Welcome back, adventurers. My daughter looks well.",
        ["king.neutral.arrival.1"]         = "So, you have returned. Let us see what you have brought me.",
        ["king.neutral.arrival.2"]         = "Hmm. You stand before me. That is a start.",
        ["king.dissatisfied.arrival.1"]    = "I expected better. We shall discuss this.",
        ["king.dissatisfied.arrival.2"]    = "You've kept me waiting. I hope the princess is unharmed.",
        ["king.furious.arrival.1"]         = "This is an outrage! How dare you return in this state!",
        ["king.furious.arrival.2"]         = "My daughter — what have you done to her?!",

        // ----- King review -----
        ["king.pleased.review.1"]          = "Excellent work! The treasury is pleased.",
        ["king.pleased.review.2"]          = "My daughter speaks warmly of you. That means more than gold.",
        ["king.neutral.review.1"]          = "Acceptable. Not remarkable, but acceptable.",
        ["king.neutral.review.2"]          = "The loot tallies adequately. We move on.",
        ["king.dissatisfied.review.1"]     = "This haul is... underwhelming, to say the least.",
        ["king.dissatisfied.review.2"]     = "My daughter suffered more than I would have liked.",
        ["king.furious.review.1"]          = "This is a disgrace! You call this a successful errand?",
        ["king.furious.review.2"]          = "Had I known you'd return with so little, I'd have hired peasants!",

        // ----- King negotiation -----
        ["king.pleased.negotiation.1"]     = "Very well. Make your case — I am in a generous mood.",
        ["king.neutral.negotiation.1"]     = "Speak quickly. My patience is finite.",
        ["king.dissatisfied.negotiation.1"]= "You have something to say? It had better be good.",
        ["king.furious.negotiation.1"]     = "Enough! Say what you must and be done with it.",

        // ----- King negotiation responses -----
        ["king.negotiation.response.flattersking"]       = "Hah! You flatter me well. I shall consider it.",
        ["king.negotiation.response.citeprincessplight"] = "Yes... my daughter's ordeal was indeed harrowing.",
        ["king.negotiation.response.bribeadvisor"]       = "*cough* My advisor informs me of a... clerical adjustment.",
        ["king.negotiation.response.grovel"]             = "Get up, get up. Dignity, please. Fine — I'll add a little.",
        ["king.negotiation.response.challengeledger"]    = "You dare question my accounting?!",

        // ----- King payout -----
        ["king.pleased.payout.1"]          = "You have earned every coin. Spend it wisely.",
        ["king.neutral.payout.1"]          = "Here is your payment. Standard rates, no more.",
        ["king.dissatisfied.payout.1"]     = "Consider yourself lucky I pay at all. Here.",
        ["king.furious.payout.1"]          = "Take your pittance and leave my sight.",

        // ----- King dismissal -----
        ["king.pleased.dismissal.1"]       = "You are dismissed. Return anytime — I have more errands.",
        ["king.neutral.dismissal.1"]       = "You are dismissed. Do better next time.",
        ["king.dissatisfied.dismissal.1"]  = "You are dismissed. I suggest you use the time wisely.",
        ["king.furious.dismissal.1"]       = "Get out of my court. NOW.",

        // ----- Tavernkeeper -----
        ["tavernkeeper.welcome"]           = "Welcome back, heroes! What can old Marta do for you?",
        ["tavernkeeper.unlock.medic"]      = "I know a field medic — heals wounds, no questions asked. Interested?",
        ["tavernkeeper.unlock.fence"]      = "A fence passed through here. Could move your extra loot for premium coin.",
        ["tavernkeeper.unlock.scout"]      = "There's a scout looking for work. Knows every dungeon in the region.",
        ["tavernkeeper.tip.general"]       = "You lot keep at it. Every run, you get a little sharper.",
        ["tavernkeeper.tip.first_run"]     = "First run's always the roughest. It gets easier — mostly.",
        ["tavernkeeper.tip.princess_delivery"] = "Bringing the princess back alive is worth more than all the gold in the dungeon.",
        ["tavernkeeper.tip.princess_care"] = "She took a beating out there. A healer's worth the coin, trust me.",
        ["tavernkeeper.tip.gather_loot"]   = "The king counts every copper. Fill those packs next time.",
        ["tavernkeeper.tip.boss"]          = "That big monster is still alive? Put it down — the king respects strength.",
        ["tavernkeeper.tip.keep_it_up"]    = "Another solid run. The king's warming up to you lot.",
    };

    // =========================================================================
    //  Asset loading
    // =========================================================================

    /// <summary>Called by <c>LoadContent()</c>. Null-safe — subtitle system skips Draw without font.</summary>
    public void LoadFont(SpriteFont font, GraphicsDevice device)
    {
        _font  = font;
        _batch = new SpriteBatch(device);
    }

    // =========================================================================
    //  Update — drain dialogue events into queue
    // =========================================================================

    public override void Update(float deltaTime)
    {
        // Enqueue King dialogue.
        if (World.TryGetSystem<KingsCourtSceneSystem>(out var kingSys))
        {
            foreach (var ev in kingSys.DialogueEvents)
                Enqueue(Resolve(ev.LineKey));
        }

        // Enqueue negotiation responses.
        if (World.TryGetSystem<NegotiationMinigameSystem>(out var negSys))
        {
            foreach (var ev in negSys.DialogueEvents)
                Enqueue(Resolve(ev.LineKey));
        }

        // Enqueue Tavernkeeper dialogue.
        if (World.TryGetSystem<TavernkeeperSystem>(out var tavernSys))
        {
            foreach (var ev in tavernSys.DialogueEvents)
                Enqueue(Resolve(ev.LineKey));
        }

        // Enqueue princess barks (already plain English strings, no key lookup).
        if (World.TryGetSystem<MoodReactionSystem>(out var moodSys))
        {
            foreach (var ev in moodSys.Barks)
                Enqueue(ev.Line);
        }

        // Tick the active entry.
        if (_active.HasValue)
        {
            var e = _active.Value;
            e.TimeRemaining -= deltaTime;
            if (e.TimeRemaining <= 0f)
            {
                _active = _queue.Count > 0 ? _queue.Dequeue() : null;
            }
            else
            {
                _active = e;
            }
        }
        else if (_queue.Count > 0)
        {
            _active = _queue.Dequeue();
        }
    }

    // =========================================================================
    //  Draw — centred subtitle with fade
    // =========================================================================

    public override void Draw(GameTime gameTime)
    {
        if (_font == null || _batch == null) return;
        if (!_active.HasValue) return;

        var entry    = _active.Value;
        float alpha  = entry.TimeRemaining < FadeStartAt
            ? Math.Clamp(entry.TimeRemaining / FadeStartAt, 0f, 1f)
            : 1f;

        var viewport = _batch.GraphicsDevice.Viewport;
        var measured = _font.MeasureString(entry.Text);
        float x = (viewport.Width  - measured.X) * 0.5f;
        float y =  viewport.Height * 0.78f;

        _batch.Begin();

        // Shadow for readability.
        _batch.DrawString(_font, entry.Text,
            new Vector2(x + 2f, y + 2f),
            Color.Black * (alpha * 0.8f));

        // Main text.
        _batch.DrawString(_font, entry.Text,
            new Vector2(x, y),
            Color.White * alpha);

        _batch.End();
    }

    // =========================================================================
    //  Helpers
    // =========================================================================

    private void Enqueue(string text) =>
        _queue.Enqueue(new SubtitleEntry(text, DisplaySeconds));

    private static string Resolve(string key) =>
        Lines.TryGetValue(key, out var text) ? text : key;

    // =========================================================================
    //  Inner type
    // =========================================================================

    private record struct SubtitleEntry(string Text, float TimeRemaining);
}
