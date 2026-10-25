using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-91: hand cards sit in fixed slots; hover or focus unrolls one card upward as a read-only preview
    // over the cards above. UI-194: one click plays a card that has one way to pay; two ways to pay are the
    // player's choice, so the click opens the preview with a Play button for each.
    public sealed class HandView
    {
        public const float CardHeight = 46;
        public const float CompactCardHeight = 40;
        private const float Gap = 6;
        private const float TopClearance = 64;
        private readonly VisualElement root;
        private readonly VisualElement grid = new VisualElement { name = "hand-grid" };
        private readonly DeckView decks;
        private readonly CardInspector inspector;
        private readonly HandDrawer drawer;
        private readonly Action<string> choose;
        private readonly Action<DraftToken> finalize;
        private readonly Dictionary<string, HandCard> cards = new Dictionary<string, HandCard>();
        private readonly List<string> order = new List<string>();
        private PlayerView view;
        private TurnDraft draft;
        private ActionDieContext context;
        private bool suspended;

        // A pointer crossing cards while the map pans, or with a button held, is not reading them.
        public Func<bool> Blocked { get; set; } = () => false;

        public string ExpandedCard => cards.Values.FirstOrDefault(card => card.Expanded)?.Id ?? "";

        public HandView(VisualElement root, CardInspector inspector, DeckView decks, HandDrawer drawer,
            Action<string> choose, Action<DraftToken> finalize)
        {
            this.root = root;
            this.inspector = inspector;
            this.choose = choose;
            this.finalize = finalize;
            this.decks = decks;
            this.drawer = drawer;
            grid.AddToClassList("hand-grid");
            root.Add(drawer.Header);
            root.Add(decks.Root);
            root.Add(grid);
            grid.RegisterCallback<GeometryChangedEvent>(e =>
            {
                if (Mathf.Abs(e.newRect.width - e.oldRect.width) > .1f)
                    Layout();
            });
        }

        public void Apply(PlayerView latest)
        {
            view = latest;
            var battle = view.Battle != null;
            var shown = view.Hand.Concat(view.Battle?.OwnTableCombatCards ?? Enumerable.Empty<string>()).Concat(view.Options.Where(o => o.Kind == CommandKind.CommitCombatCard)
                .SelectMany(o => o.EligibleIds)).Distinct().ToList();
            if (grid.parent != root)
            {
                root.Clear();
                root.Add(drawer.Header);
                root.Add(decks.Root);
                root.Add(grid);
            }
            foreach (var id in cards.Keys.Where(id => !shown.Contains(id)).ToArray())
            {
                cards[id].Slot.RemoveFromHierarchy();
                cards.Remove(id);
            }
            order.Clear();
            order.AddRange(shown);
            foreach (var id in shown)
            {
                if (!cards.TryGetValue(id, out var card))
                {
                    card = Make(id);
                    cards.Add(id, card);
                    grid.Add(card.Slot);
                }
                card.Slot.userData = view.Decision?.Kind == DecisionKind.CombatCard ? null : new DraftToken(DraftObject.Card, id);
                card.Apply(view.Configuration, battle, !view.Hand.Contains(id));
            }
            Layout();
            root.EnableInClassList("excess-hand", shown.Count > 6);
            inspector.Update(view);
        }

        public void Refresh(TurnDraft selection, ActionDieContext die)
        {
            draft = selection;
            context = die;
            if (view == null)
                return;
            var anyPlayable = false;
            var combat = CombatCardCondition.Eligible(view);
            var dieChosen = DieChosen();
            foreach (var card in cards.Values)
            {
                // UI-111: a card that cannot be committed in this battle is dimmed and says why.
                card.Slot.EnableInClassList("combat-unavailable", combat != null && !combat.Contains(card.Id));
                card.Slot.EnableInClassList("selected", draft.Payload.CardId == card.Id || draft.Payload.Ids.Contains(card.Id));
                var eligible = Eligible(card.Id);
                card.State(eligible, Discarding(), Hint(card.Id, eligible));
                card.Slot.EnableInClassList(HandCard.UnplayableClass, dieChosen && !eligible);
                anyPlayable |= eligible;
            }
            drawer.Highlight(anyPlayable || CardActions.CanDraw(view, draft, context, "character") ||
                CardActions.CanDraw(view, draft, context, "strategy"), Discarding());
        }

        public void Suspend(bool dragging)
        {
            suspended = dragging;
            if (dragging)
                CollapseAll();
        }

        private HandCard Make(string id)
        {
            HandCard card = null;
            card = new HandCard(id, () => Click(card.Id));
            card.Slot.RegisterCallback<PointerEnterEvent>(e =>
            {
                if (!Blocked() && e.pressedButtons == 0)
                    Expand(card);
            });
            card.Slot.RegisterCallback<PointerLeaveEvent>(_ => card.Collapse());
            card.Slot.RegisterCallback<FocusInEvent>(_ => Expand(card));
            card.Slot.RegisterCallback<FocusOutEvent>(_ => card.Collapse());
            return card;
        }

        private void Click(string id)
        {
            if (view == null)
                return;
            // OBS-03: a board without a draft (an observer's) only shows the card.
            if (draft == null)
            {
                inspector.Show(id, true, false, () => drawer.InspectionBoundary ?? root, cards[id].Slot);
                return;
            }
            // UI-142: discarding from an excess hand is a complete choice, so the click discards the card.
            // UI-194: a card with one way to pay is a complete choice too, so the click plays it.
            if (draft.Kind == CommandKind.DiscardCards && Eligible(id) || Playable(id) && !Alternatives(id))
            {
                finalize(new DraftToken(DraftObject.Card, id));
                return;
            }
            if (Playable(id))
                inspector.Show(id, true, avoid: () => drawer.InspectionBoundary ?? root, origin: cards[id].Slot);
            choose(id);
        }

        // Two ways to pay are a real choice: the preview's Play buttons offer both.
        private bool Alternatives(string id) =>
            view.Decision?.Kind == DecisionKind.Action && CardActions.Costs(view, draft, context, id).Select(c => c.Face).Distinct().Count() > 1;

        private void Expand(HandCard card)
        {
            if (suspended || grid.contentRect.width <= 0)
                return;
            foreach (var other in cards.Values.Where(other => other != card))
                other.Collapse();
            var top = grid.panel?.visualTree.worldBound.yMin ?? 0;
            card.Expand(grid.contentRect.width, Mathf.Max(card.Slot.layout.height, card.Slot.worldBound.yMax - top - TopClearance));
        }

        private void CollapseAll()
        {
            foreach (var card in cards.Values)
                card.Collapse();
        }

        // Slots are positioned absolutely, so raising an expanded card never reflows its neighbours.
        private void Layout()
        {
            var width = grid.contentRect.width;
            if (width <= 0 || float.IsNaN(width))
                return;
            var columns = order.Count > 6 ? 3 : 2;
            var height = IsCompact() ? CompactCardHeight : CardHeight;
            var cardWidth = Mathf.Floor((width - Gap * (columns - 1)) / columns);
            for (var i = 0; i < order.Count; i++)
                cards[order[i]].Place(i % columns * (cardWidth + Gap), i / columns * (height + Gap), cardWidth, height, width);
            var rows = (order.Count + columns - 1) / columns;
            grid.style.height = Mathf.Max(0, rows * (height + Gap) - Gap);
        }

        private bool IsCompact()
        {
            for (var element = root; element != null; element = element.parent)
                if (element.ClassListContains("compact-edge-board"))
                    return true;
            return false;
        }

        // UI-222: the hand is where the player discards: an excess hand, a removal cost (A Power too Great, Tom
        // Bombadil) or an effect's choice of a card from this hand (We Won't Go Back).
        private static readonly string[] HandDiscardChoices = { "SmeagolCorruption" };

        private bool Discarding() => draft.Kind is CommandKind.DiscardCards or CommandKind.DiscardProtection ||
            draft.Kind == CommandKind.ChooseEffect && DiscardChoice(view);

        // An effect's choice of a hand card that discards it (UI-326 dissolves it).
        public static bool DiscardChoice(PlayerView view) => HandDiscardChoices.Contains(view?.Decision?.Presentation);

        private bool HandChoice(string id) => draft.Kind == CommandKind.ChooseEffect && Discarding() && view.Hand.Contains(id) &&
            DraftOptions.For(view, draft)?.EligibleIds.Contains(id) == true;

        private bool Eligible(string id)
        {
            if (HandChoice(id))
                return true;
            var removal = view.Options.FirstOrDefault(o => o.Kind == CommandKind.DiscardProtection && draft.Kind == o.Kind);
            if (removal != null)
                return removal.EligibleIds.Contains(id);
            if (ReactionCard(id))
                return true;
            if (view.Decision?.Kind == DecisionKind.CombatCard)
                return false;
            return draft.Kind == CommandKind.DiscardCards ? DraftOptions.For(view, draft).EligibleIds.Contains(id)
                : CardActions.CanPlay(view, draft, context, id);
        }

        // UI-214: a card whose reaction the player's decision offers is lit; its preview's Use answers (UI-164).
        private bool ReactionCard(string id) => view.DecisionOwner == view.You &&
            view.Options.Any(o => o.Enabled && o.Kind == CommandKind.RespondEffect && o.EffectChoices.Any(c => c.SourceId == id));

        // UI-124: with an action die chosen, the cards it cannot play step back so the playable ones stand out.
        private bool DieChosen() => view.Decision?.Kind == DecisionKind.Action && context != null &&
            context.DieId.Length > 0 && draft.Kind != CommandKind.DiscardCards;

        // Choosing a card as a cost is not a play.
        private bool Playable(string id) => !Discarding() && Eligible(id);

        private string Hint(string id, bool eligible)
        {
            var discard = draft.Kind == CommandKind.DiscardCards;
            if (draft.Kind == CommandKind.DiscardProtection)
                return eligible ? "Click to discard as the cost · both cards remove the protection" : "Cannot pay this cost";
            if (HandChoice(id))
                return "Click to discard this card";
            if (view.Decision?.Kind == DecisionKind.CombatCard)
                return CombatCardCondition.Eligible(view)?.Contains(id) == false ? CombatCardCondition.Text(id)
                    : "Choose Combat cards in the battle window";
            if (eligible && ReactionCard(id))
                return "Click, then Use its reaction";
            if (eligible)
                return discard ? "Click to discard face down" : PlayHint(id);
            return "Choose a matching die to play";
        }

        private string PlayHint(string id) => Alternatives(id) ? "Click to choose how to pay" : "Click to play" + PaidWith(id);

        private string PaidWith(string id)
        {
            var face = CardActions.Cost(view, draft, context, id)?.Face;
            return face.HasValue && view.Decision?.Kind == DecisionKind.Action ? " · " + face + " die" : "";
        }
    }
}
