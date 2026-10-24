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
            Cards.Note = PlayedNote;
            Cards.Show(revealed, keep: false, allowPlay: false);
            Cards.Root.AddToClassList(PresentedClass);
            if (area.HasValue)
                Cards.PlaceIn(area.Value, Left(cue));
            return new RevealBeat(this, revealed, cue.ActionId, now);
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
        private void PinRevealed(ClickEvent e)
        {
            if (Shown(revealed) && !Cards.Pinned)
                Cards.Pin();
        }

        // The dock: the console trail recalls the card until the presentation ends; a click opens and closes it.
        private void Dock(string id, string action)
        {
            if (Shown(id) && !Cards.Pinned)
                Cards.Close();
            Cards.Root.RemoveFromClassList(PresentedClass);
            docked = id;
            dockedAction = action;
            board.Console.Status.PresentSource(CardLibrary.Title(id), () => Toggle(id));
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

        private sealed class RevealBeat : IPresentationBeat
        {
            private readonly CardRevealPresenter owner;
            private readonly string id;
            private readonly string action;
            private readonly double end;

            public RevealBeat(CardRevealPresenter owner, string id, string action, double now)
            {
                this.owner = owner;
                this.id = id;
                this.action = action;
                end = now + HoldSeconds;
            }

            public bool Advance(double now)
            {
                if (now < end)
                    return false;
                owner.Dock(id, action);
                return true;
            }

            // Skip or a rebase: an unpinned card closes; a card the player pinned stays for them.
            public void Finish()
            {
                if (owner.Shown(id) && !owner.Cards.Pinned)
                    owner.Cards.Close();
                owner.Cards.Root.RemoveFromClassList(PresentedClass);
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
