using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-308 (OPPONENT_ACTIONS_UX §6.2; storyboard B2–B3): the opponent's public Event card. It opens in the existing
    // read-only CardInspector (no Play bar; the Event half marked with the observed treatment) at its readable width, in the
    // half of the safe map area away from the action's first effect, for at least 3 s. A click on it pins it, and the
    // presentation then holds until it closes. Afterwards the card docks into the console trail as the compact source of the
    // rest of the presentation; its effects play through their own presenters. A table card's entry or removal is a brief
    // trace at the Effects tab or its tile, never a new reveal. A combat card's reveal belongs to the battle window.
    // UI-316 (owner review): the card grows out of the opponent's hand count into its reading place and stands there for
    // the 3 s; then it shrinks into where it goes — the console trail it docks in, or the Effects tab for a card that stays
    // on the table (the tab then lights up) — and the trail's new source glows. A card the player pinned stands for them.
    public sealed class CardRevealPresenter : ICuePresenter
    {
        public const double HoldSeconds = 3;
        public const double TableSeconds = .8;
        public const string PresentedClass = "presented-card";
        public const string PlayedNote = "Event played";
        private const float Inset = 8;
        private readonly BoardScreen board;
        private readonly PresentationPlayback playback;
        private readonly PresentationCamera camera;
        private readonly ObjectGlow dockGlow = new ObjectGlow();
        private RevealBeat reveal;
        private string revealed = "";
        // The card docked as the source of an action: later presentations of the same action recall it without a reveal.
        private string dockedAction = "";
        private string docked = "";

        public CardRevealPresenter(BoardScreen board, PresentationPlayback playback, PresentationCamera camera)
        {
            this.board = board;
            this.playback = playback;
            this.camera = camera;
            Cards.Root.RegisterCallback<ClickEvent>(PinRevealed);
        }

        private CardInspector Cards => board.Cards;
        private bool Shown(string id) => id.Length > 0 && Cards.CardId == id && Cards.Root.style.display.value == DisplayStyle.Flex;

        // The player reads the revealed card: it is pinned and open.
        public bool Reading => Shown(revealed) && Cards.Pinned;

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            Recall(cue.ActionId);
            var card = cue.Card;
            if (cue.Kind != PresentationCueKind.Card || card == null || card.Id.Length == 0 || !CardLibrary.Cards.ContainsKey(card.Id))
                return null;
            return card.Use switch
            {
                ActionCardUse.Event => Reveal(cue, now),
                ActionCardUse.TableEntered => Table(board.TableEffects.Tab, now),
                ActionCardUse.TableRemoved => Table(Tile(card.Id), now),
                _ => null
            };
        }

        // The presentation ended: the compact source leaves, and an unpinned card closes.
        public void Released()
        {
            dockGlow.End();
            reveal = null;
            board.Console.Status.EndSource();
            if (Shown(revealed) && !Cards.Pinned)
                Cards.Close();
            Cards.Root.RemoveFromClassList(PresentedClass);
            revealed = "";
        }

        private IPresentationBeat Reveal(PresentationCue cue, double now)
        {
            revealed = cue.Card.Id;
            var area = StageArea();
            var flight = new CardFlight(Cards.Root, board.ReducedMotion());
            flight.Hide();
            Cards.Note = PlayedNote;
            Cards.Show(revealed, keep: false, allowPlay: false);
            Cards.Root.AddToClassList(PresentedClass);
            if (area.HasValue)
                Cards.PlaceIn(area.Value, Left(cue));
            reveal = new RevealBeat(this, revealed, cue.ActionId, flight, StaysOnTable(cue), now);
            return reveal;
        }

        // A card that stays on the table goes to the Effects tab; any other Event docks in the console trail.
        private bool StaysOnTable(PresentationCue cue) => playback.Upcoming.Any(next => next.ActionId == cue.ActionId &&
            next.Kind == PresentationCueKind.Card && next.Card?.Use == ActionCardUse.TableEntered && next.Card.Id == cue.Card.Id);

        // Where the opponent's Event comes from: their hand count. The card's stage lies under the top bar, so it starts
        // just below the counter and is seen leaving it from the first frame.
        private Rect? Origin()
        {
            var hand = board.TopBar.Opponent.Root.Q("opponent-cards");
            if (!PresentationTargets.Shown(hand))
                return null;
            var bounds = hand.worldBound;
            var bar = board.TopBar.Root.worldBound;
            return new Rect(bounds.x, Mathf.Max(bounds.yMax, bar.yMax), bounds.width, bounds.height);
        }

        // Where it goes: the Effects tab for a card that stays on the table, else the console trail it docks in.
        private VisualElement Destination(bool table) => table
            ? board.TableEffects.Tab
            : PresentationTargets.First(board.Console.Status.Trail, board.Console.Status.Plaque);

        // The safe map area in the card's stage, inset; the card stands at its top.
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

        // Away from the action's first effect: when it lies in the right half of the safe area, the card stands left.
        private bool Left(PresentationCue cue)
        {
            var effect = playback.Upcoming.FirstOrDefault(next => next.ActionId == cue.ActionId && next.Regions.Count > 0);
            var region = effect?.Regions.FirstOrDefault(board.Map.Data.Shapes.ContainsKey);
            if (region == null)
                return false;
            var anchor = board.Map.Data.Shapes[region].Anchor;
            return board.Map.Camera.ToScreen(new Vector2(anchor[0], anchor[1])).x > camera.SafeArea().center.x;
        }

        private VisualElement Tile(string id)
        {
            var tile = board.TableEffects.Root.Q("effect-source-" + id);
            return board.TableEffects.IsOpen && tile != null && tile.worldBound.width > 0 ? tile : board.TableEffects.Tab;
        }

        private IPresentationBeat Table(VisualElement target, double now)
        {
            var glow = new ObjectGlow();
            glow.Follow(target);
            return new GlowBeat(glow, now, TableSeconds);
        }

        // "A click on the shown card pins it"; the presentation then holds until it is closed.
        // UI-316: a card pinned mid-flight stands at its place at once (the presentation clock stops while it is read).
        private void PinRevealed(ClickEvent e)
        {
            if (!Shown(revealed) || Cards.Pinned)
                return;
            Cards.Pin();
            reveal?.Settle();
        }

        // The dock: the console trail recalls the card until the presentation ends; a click opens and closes it. An Event
        // that went to the trail lights its new source there; a table card's Effects tab lights with its own cue.
        private void Dock(string id, string action, bool table)
        {
            if (Shown(id) && !Cards.Pinned)
                Cards.Close();
            Cards.Root.RemoveFromClassList(PresentedClass);
            docked = id;
            dockedAction = action;
            var chip = board.Console.Status.PresentSource(CardLibrary.Title(id), () => Toggle(id));
            if (!table)
                LightDock(chip);
        }

        private void LightDock(VisualElement chip)
        {
            dockGlow.Follow(chip);
            chip.schedule.Execute(() =>
            {
                if (dockGlow.Target == chip)
                    dockGlow.End();
            }).StartingIn((long)(PresentationPace.HoldSeconds * 1000));
        }

        // A later cue of the docked card's action (after a real choice) shows the same compact source again; another
        // action's cue forgets it.
        private void Recall(string action)
        {
            if (action != dockedAction)
            {
                docked = dockedAction = "";
                return;
            }
            if (!board.Console.Status.HasSource)
                board.Console.Status.PresentSource(CardLibrary.Title(docked), () => Toggle(docked));
        }

        private void Toggle(string id)
        {
            if (Shown(id))
                Cards.Close();
            else
            {
                Cards.Note = PlayedNote;
                Cards.Show(id, keep: true, allowPlay: false);
            }
        }

        // The card waits for its place, grows out of the hand count, stands to be read for 3 s, then leaves for where it
        // goes and docks. Closed by the player, it docks at once.
        private sealed class RevealBeat : IPresentationBeat
        {
            private readonly CardRevealPresenter owner;
            private readonly string id;
            private readonly string action;
            private readonly CardFlight flight;
            private readonly bool table;
            private readonly double created;
            private double arriving = -1;
            private double standing = -1;
            private double leaving = -1;
            private bool settled;

            public RevealBeat(CardRevealPresenter owner, string id, string action, CardFlight flight, bool table, double now)
            {
                this.owner = owner;
                this.id = id;
                this.action = action;
                this.flight = flight;
                this.table = table;
                created = now;
            }

            public bool Advance(double now)
            {
                if (!owner.Shown(id))
                    return Docked();
                if (settled)
                    return Resume(now);
                if (arriving < 0)
                    return Arrive(now);
                if (standing < 0)
                    return Arriving(now);
                if (leaving < 0)
                    return Stand(now);
                flight.Leaving((now - leaving) / CardFlight.Seconds);
                return now - leaving >= CardFlight.Seconds && Docked();
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
                flight.Clear();
                if (owner.Shown(id) && !owner.Cards.Pinned)
                    owner.Cards.Close();
                owner.Cards.Root.RemoveFromClassList(PresentedClass);
            }

            // Its place is laid out on a later tick than its opening.
            private bool Arrive(double now)
            {
                if (now <= created || !CardFlight.Laid(owner.Cards.Root))
                    return false;
                arriving = now;
                flight.From(owner.Origin());
                flight.Arriving(0);
                return false;
            }

            private bool Arriving(double now)
            {
                flight.Arriving((now - arriving) / CardFlight.Seconds);
                if (now - arriving < CardFlight.Seconds)
                    return false;
                flight.Clear();
                standing = now;
                return false;
            }

            private bool Stand(double now)
            {
                if (now - standing < HoldSeconds)
                    return false;
                leaving = now;
                flight.To(owner.Destination(table));
                flight.Leaving(0);
                return false;
            }

            // After a pin the reading time starts again; a card pinned on its way out docks.
            private bool Resume(double now)
            {
                settled = false;
                if (leaving >= 0)
                    return Docked();
                arriving = arriving < 0 ? now : arriving;
                standing = now;
                return false;
            }

            private bool Docked()
            {
                owner.Dock(id, action, table);
                flight.Clear();
                return true;
            }
        }

        private sealed class GlowBeat : IPresentationBeat
        {
            private readonly ObjectGlow glow;
            private readonly double end;

            public GlowBeat(ObjectGlow glow, double now, double seconds)
            {
                this.glow = glow;
                end = now + seconds;
            }

            public bool Advance(double now)
            {
                if (now < end)
                    return false;
                glow.End();
                return true;
            }

            public void Finish() => glow.End();
        }
    }
}
