using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-308 (OPPONENT_ACTIONS_UX §6.2; storyboard B2–B3): the opponent's public Event card. It opens in the existing
    // read-only CardInspector (no Play bar; the Event half marked with the observed treatment) at its readable width, in the
    // middle of the safe map area (UI-324, the owner's decision), for at least 3 s. A click on it pins it, and the
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
        public const string StandInName = "presentation-card-stand-in";
        // The part of the way out over which the card and its stand-in cross-fade.
        private const double Handover = .35;
        public const string PlayedNote = "Event played";
        private const float Inset = 8;
        private readonly BoardScreen board;
        private readonly PresentationPlayback playback;
        private readonly PresentationCamera camera;
        private readonly PresentationLayer layer;
        private readonly ObjectGlow dockGlow = new ObjectGlow();
        private RevealBeat reveal;
        private string revealed = "";
        // The card docked as the source of an action: later presentations of the same action recall it without a reveal.
        private string dockedAction = "";
        private string docked = "";

        public CardRevealPresenter(BoardScreen board, PresentationPlayback playback, PresentationCamera camera, PresentationLayer layer)
        {
            this.board = board;
            this.playback = playback;
            this.camera = camera;
            this.layer = layer;
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
                Cards.PlaceIn(area.Value);
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
        private VisualElement Dock(string id, string action)
        {
            CloseRevealed(id);
            return PresentSource(id, action);
        }

        private void CloseRevealed(string id)
        {
            if (Shown(id) && !Cards.Pinned)
                Cards.Close();
            Cards.Root.RemoveFromClassList(PresentedClass);
        }

        private VisualElement PresentSource(string id, string action)
        {
            docked = id;
            dockedAction = action;
            return board.Console.Status.PresentSource(CardLibrary.Title(id), () => Toggle(id));
        }

        // UI-324 (owner: "cards going down to the bottom panel should not slide behind it"): the card's stand-in on its way
        // out — a small parchment card with its title in the presentation layer above the HUD — so it is seen landing on its
        // place in the console or at the Effects tab.
        private VisualElement StandIn(string id)
        {
            var card = new VisualElement { name = StandInName, pickingMode = PickingMode.Ignore };
            card.AddToClassList(StandInName);
            card.Add(new Label(CardLibrary.Title(id)) { pickingMode = PickingMode.Ignore });
            var bounds = layer.Local(Cards.Root.worldBound);
            card.style.left = bounds.x;
            card.style.top = bounds.y;
            card.style.width = bounds.width;
            card.style.height = bounds.height;
            layer.Root.Add(card);
            return card;
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
            private Rect place;
            // The way out: the stand-in above the HUD, its flight, and the console's new source waiting for it.
            private VisualElement standIn;
            private CardFlight departure;
            private VisualElement chip;
            private bool aimed;
            private bool handedOver;

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
                if (standIn != null)
                    return Depart(now);
                if (!owner.Shown(id))
                    return Docked();
                if (settled)
                    return Resume(now);
                if (arriving < 0)
                    return Arrive(now);
                if (standing < 0)
                    return Arriving(now);
                return Stand(now);
            }

            // How far a flight that started at `from` has gone: all the way when it takes no time (Skip animations).
            private double Progress(double now, double from) => flight.Duration > 0 ? (now - from) / flight.Duration : 1;

            // Pinned by the player: the card stands at its place, whatever its flight.
            public void Settle()
            {
                flight.Clear();
                settled = true;
            }

            // Skip or a rebase: an unpinned card closes; a card the player pinned stays for them; a stand-in on its way
            // lands at once.
            public void Finish()
            {
                flight.Clear();
                if (standIn != null)
                {
                    Land();
                    return;
                }
                if (owner.Shown(id) && !owner.Cards.Pinned)
                    owner.Cards.Close();
                owner.Cards.Root.RemoveFromClassList(PresentedClass);
            }

            // Its place is laid out on a later tick than its opening, and centred on the next (UI-324): the flight starts
            // once the place stays the same for a tick.
            private bool Arrive(double now)
            {
                var current = owner.Cards.Root.worldBound;
                var still = current == place;
                place = current;
                if (now <= created || !CardFlight.Laid(owner.Cards.Root) || !still)
                    return false;
                arriving = now;
                flight.From(owner.Origin());
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

            // After its reading the card's stand-in joins it, unseen, and its new source waits, unseen, in the console.
            private bool Stand(double now)
            {
                if (now - standing < HoldSeconds)
                    return false;
                standIn = owner.StandIn(id);
                standIn.style.opacity = 0;
                departure = new CardFlight(standIn, owner.board.ReducedMotion());
                chip = owner.PresentSource(id, action);
                if (!table)
                    chip.style.opacity = 0;
                leaving = now;
                return false;
            }

            // Once the destination is laid out the card and its stand-in leave together: the card fades out as the stand-in
            // fades in (the card's stage ends above the console, the stand-in flies over it), then the stand-in lands.
            private bool Depart(double now)
            {
                var destination = table ? owner.board.TableEffects.Tab : chip;
                if (!aimed)
                {
                    if (!CardFlight.Laid(destination) && now - leaving < CardFlight.Seconds)
                        return false;
                    aimed = true;
                    flight.To(destination);
                    departure.To(destination);
                    leaving = now;
                }
                var t = departure.Duration > 0 ? (now - leaving) / departure.Duration : 1;
                if (!handedOver)
                    HandOver(t);
                departure.Leaving(t, (float)System.Math.Min(1, t / Handover));
                if (now - leaving < departure.Duration)
                    return false;
                Land();
                return true;
            }

            private void HandOver(double t)
            {
                flight.Leaving(t, (float)System.Math.Max(0, 1 - t / Handover));
                if (t < Handover)
                    return;
                handedOver = true;
                flight.Clear();
                owner.CloseRevealed(id);
            }

            private void Land()
            {
                if (!handedOver)
                {
                    handedOver = true;
                    flight.Clear();
                    owner.CloseRevealed(id);
                }
                standIn.RemoveFromHierarchy();
                standIn = null;
                chip.style.opacity = StyleKeyword.Null;
                if (!table)
                    owner.LightDock(chip);
            }

            // After a pin the reading time starts again.
            private bool Resume(double now)
            {
                settled = false;
                arriving = arriving < 0 ? now : arriving;
                standing = now;
                return false;
            }

            // Closed by the player: the card docks at once.
            private bool Docked()
            {
                chip = owner.Dock(id, action);
                if (!table)
                    owner.LightDock(chip);
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
