using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-546 (owner, 2026-11-18: "when, during a battle or the Hunt, a card's effect triggers and something happens — damage,
    // a draw, a move, anything — we must see an animation presenting that card, so it is clear where the effects come
    // from"). A shown battle or Hunt step whose recorded cause is a card (a reaction to the Hunt such as Axe and Bow or
    // Mithril Coat and Sting, a card that starts a Hunt, a card's hits in a battle) first presents that card. The card
    // grows out of where it comes from: the Effects tab for a card on the table, otherwise its side's hand. It stands to
    // be read in the middle of the safe map area, beside the open battle or Hunt window and never over it, with the note
    // "Takes effect". Then it shrinks into that window, and the effect plays.
    // - A card is presented once per battle or Hunt of an action. An Event the action played was revealed already
    //   (CardRevealPresenter) and is not presented again.
    // - A combat card's reveal stays the battle window's own. A card's dice keep their window (CardRollPresenter), which
    //   names the card.
    // - A click on the card pins it, and the presentation holds until it is closed. Skip closes an unpinned card.
    // - UI-558 (owner, 2026-11-20: "cards and effects that cause a roll before the combat phase fire twice: once when it is
    //   their time, then again just before the real roll, with its sound. It should happen once, from the first pre-roll,
    //   as with Charge, smoothly into the combat phase"): in a battle a card is presented once per round, across
    //   presentations. The losses its extra attack causes are the other side's decision, a later presentation, and they
    //   and the Combat roll after them flow on without it. A card whose extra attack the battle already shows is not
    //   presented again either (its pre-roll was the player's own, applied straight to the board).
    public sealed class CardTriggerPresenter : ICuePresenter
    {
        public const double HoldSeconds = 2.5;
        public const string TriggerNote = "Takes effect";
        private const float Inset = 8;
        private readonly BoardScreen board;
        private readonly StagedBoard staged;
        private readonly PresentationCamera camera;
        // The cards presented in this presentation (by action, battle or Hunt and card) and the action's revealed Events.
        private readonly HashSet<string> presented = new HashSet<string>();
        private readonly HashSet<string> events = new HashSet<string>();
        // UI-558: the cards presented in a battle round (battle, round and card), kept across presentations.
        private readonly HashSet<string> rounds = new HashSet<string>();
        private TriggerBeat beat;
        private string shown = "";

        public CardTriggerPresenter(BoardScreen board, StagedBoard staged, PresentationCamera camera)
        {
            this.board = board;
            this.staged = staged;
            this.camera = camera;
            Cards.Root.RegisterCallback<ClickEvent>(PinShown);
        }

        private CardInspector Cards => board.Cards;

        // The player reads the presented card: it is pinned and open.
        public bool Reading => Shown(shown) && Cards.Pinned;

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            if (cue.Kind == PresentationCueKind.Card)
            {
                if (cue.Card?.Use == ActionCardUse.Event && cue.Card.Id.Length > 0)
                    events.Add(cue.ActionId + "/" + cue.Card.Id);
                return null;
            }
            var card = Trigger(cue);
            if (card == null || events.Contains(cue.ActionId + "/" + card) || SeenInItsRound(cue, card) ||
                !presented.Add(Scope(cue) + "/" + card))
                return null;
            if (cue.BattleId.Length > 0)
                rounds.Add(RoundKey(cue, card));
            return Present(cue, card, now);
        }

        // The presentation ended: an unpinned card closes, and the next presentation presents its cards again.
        public void Released()
        {
            beat?.Finish();
            beat = null;
            shown = "";
            presented.Clear();
            events.Clear();
        }

        // The card whose effect a shown battle or Hunt cue is, or null. A card's own cues (its reveal, entry or removal),
        // a cost, a die and a card's dice present themselves.
        public static string Trigger(PresentationCue cue)
        {
            if (cue.Kind is PresentationCueKind.Card or PresentationCueKind.Cost or PresentationCueKind.Die
                or PresentationCueKind.SpentDie or PresentationCueKind.Roll or PresentationCueKind.RandomDraw)
                return null;
            if (cue.BattleId.Length == 0 && cue.HuntChainId.Length == 0 && cue.Hunt == null && cue.Battle == null)
                return null;
            var source = cue.Hunt?.SourceId is { Length: > 0 } entry ? entry : cue.Cause;
            return source.Length > 0 && CardLibrary.Cards.ContainsKey(source) ? source : null;
        }

        // UI-558: this battle round already presented the card, or already shows its extra attack.
        private bool SeenInItsRound(PresentationCue cue, string card)
        {
            if (cue.BattleId.Length == 0)
                return false;
            if (rounds.Contains(RoundKey(cue, card)))
                return true;
            var battle = staged.View.Battle;
            return battle != null && battle.Id == cue.BattleId && battle.Round == Round(cue) &&
                battle.SpecialAttacks.Any(attack => attack.SourceId == card);
        }

        // The cue's round: its own snapshot's, else the round the board presents for that battle.
        private int Round(PresentationCue cue) => cue.Battle?.Round ??
            (staged.View.Battle?.Id == cue.BattleId ? staged.View.Battle.Round : 0);

        private string RoundKey(PresentationCue cue, string card) => cue.BattleId + "/" + Round(cue) + "/" + card;

        private static string Scope(PresentationCue cue) =>
            cue.ActionId + "/" + (cue.HuntChainId.Length > 0 ? cue.HuntChainId : cue.BattleId);

        private IPresentationBeat Present(PresentationCue cue, string card, double now)
        {
            var flight = new CardFlight(Cards.Root, board.ReducedMotion());
            flight.Hide();
            Cards.Note = TriggerNote;
            Cards.Show(card, keep: false, allowPlay: false, avoid: OpenWindow);
            if (!Shown(card))
            {
                flight.Clear();
                return null;
            }
            Cards.Root.AddToClassList(CardRevealPresenter.PresentedClass);
            if (StageArea() is { } area)
                Cards.PlaceIn(area);
            shown = card;
            beat = new TriggerBeat(this, card, flight, now);
            return beat;
        }

        private bool Shown(string id) => id.Length > 0 && Cards.CardId == id && Cards.Root.style.display.value == DisplayStyle.Flex;

        // The window the effect plays in, when it is open: the battle's, else the Hunt's.
        private VisualElement OpenWindow()
        {
            if (board.Battles.IsOpen)
                return board.Battles.Root;
            return board.Hunt.IsOpen ? board.Hunt.Root : null;
        }

        // Where the card comes from: the Effects tab for a card on the table, else its side's hand count.
        private Rect? Origin(string card)
        {
            var view = staged.View;
            var table = view.Effects.Any(effect => effect.SourceId == card) || view.Players.Any(player => player.TableCards.Contains(card));
            var from = table ? board.TableEffects.Tab : CardLibrary.Owner(card) is { } side ? board.HandOf(side) : null;
            return from != null && PresentationTargets.Shown(from) ? from.worldBound : (Rect?)null;
        }

        // The safe map area in the card's stage, inset, as an Event's reveal stands in it.
        private Rect? StageArea()
        {
            var stage = Cards.Root.parent;
            if (stage == null)
                return null;
            var safe = camera.SafeArea();
            var world = new Rect(board.Map.LocalToWorld(safe.position), safe.size);
            var local = new Rect(stage.WorldToLocal(world.position), world.size);
            return new Rect(local.x + Inset, local.y + Inset, local.width - 2 * Inset, local.height - 2 * Inset);
        }

        // "A click on the shown card pins it"; the presentation then holds until it is closed.
        private void PinShown(ClickEvent e)
        {
            if (!Shown(shown) || Cards.Pinned)
                return;
            Cards.Pin();
            beat?.Settle();
        }

        private void Close(string card)
        {
            if (Shown(card) && !Cards.Pinned)
                Cards.Close();
            Cards.Root.RemoveFromClassList(CardRevealPresenter.PresentedClass);
            if (shown == card)
                shown = "";
        }

        // The card waits for its place, grows out of its origin, stands to be read, then shrinks into the window the effect
        // plays in (or fades where it stands) and closes. Closed by the player, it is done at once.
        private sealed class TriggerBeat : IPresentationBeat
        {
            private readonly CardTriggerPresenter owner;
            private readonly string card;
            private readonly CardFlight flight;
            private readonly double created;
            private double arriving = -1;
            private double standing = -1;
            private double leaving = -1;
            private bool settled;
            private bool done;
            private Rect place;

            public TriggerBeat(CardTriggerPresenter owner, string card, CardFlight flight, double now)
            {
                this.owner = owner;
                this.card = card;
                this.flight = flight;
                created = now;
            }

            public bool Advance(double now)
            {
                if (done)
                    return true;
                if (!owner.Shown(card))
                    return End();
                if (settled)
                    return Resume(now);
                if (arriving < 0)
                    return Arrive(now);
                if (standing < 0)
                    return Arriving(now);
                if (leaving < 0)
                    return Stand(now);
                return Leaving(now);
            }

            // Pinned by the player: the card stands at its place, whatever its flight.
            public void Settle()
            {
                flight.Clear();
                settled = true;
            }

            // Skip or a rebase: an unpinned card closes; a card the player pinned stays for them.
            public void Finish()
            {
                if (done)
                    return;
                done = true;
                flight.Clear();
                owner.Close(card);
            }

            private double Progress(double now, double from) => flight.Duration > 0 ? (now - from) / flight.Duration : 1;

            // Its place is laid out on a later tick than its opening: the flight starts once the place holds for a tick.
            private bool Arrive(double now)
            {
                var current = owner.Cards.Root.worldBound;
                var still = current == place;
                place = current;
                if (now <= created || !CardFlight.Laid(owner.Cards.Root) || !still)
                    return false;
                arriving = now;
                flight.From(owner.Origin(card));
                flight.Arriving(0);
                return false;
            }

            private bool Arriving(double now)
            {
                flight.Arriving(Progress(now, arriving));
                if (now - arriving < flight.Duration)
                    return false;
                flight.Clear();
                standing = now;
                return false;
            }

            private bool Stand(double now)
            {
                if (now - standing < HoldSeconds)
                    return false;
                flight.To(owner.OpenWindow());
                leaving = now;
                return false;
            }

            private bool Leaving(double now)
            {
                var t = Progress(now, leaving);
                flight.Leaving(t, (float)System.Math.Max(0, 1 - t));
                if (t < 1)
                    return false;
                return End();
            }

            // After a pin the reading time starts again.
            private bool Resume(double now)
            {
                settled = false;
                arriving = arriving < 0 ? now : arriving;
                standing = now;
                leaving = -1;
                return false;
            }

            private bool End()
            {
                Finish();
                return true;
            }
        }
    }
}
