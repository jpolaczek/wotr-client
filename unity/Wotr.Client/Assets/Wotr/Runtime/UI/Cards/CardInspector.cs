using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class CardInspector
    {
        public VisualElement Root { get; } = new VisualElement { name = "card-inspector" };
        public string CardId { get; private set; } = "";
        public bool Pinned { get; private set; }
        public FloatingWindow Window { get; set; }
        public event Action Opening;
        // UI-89: the control that opened this preview, or null when it came from outside any window.
        public event Action<VisualElement> OpenedFrom;
        public event Action<string, DieFace?> PlayRequested;
        // UI-164: the reaction the current decision offers for a card in the hand ("" without one), used at the card.
        public Func<string, string> Reaction { get; set; }
        public event Action<string> ReactionRequested;
        // UI-142: a click on the card's text discards it while an excess hand asks for a discard.
        public event Action<string> DiscardRequested;
        private bool combat;
        private PlayerView view;
        private readonly Label title = BoardScreen.Title("");
        private readonly Label text = new Label { name = "card-text" };
        private readonly Label window = new Label { name = "card-window" };
        private readonly Button pin;
        private readonly VisualElement eventSection = new VisualElement { name = "card-event-section" };
        private readonly VisualElement combatSection = new VisualElement { name = "card-combat-section" };
        private readonly Label eventHeading = new Label { name = "card-event-heading" };
        private readonly Label combatHeading = new Label { name = "card-combat-heading" };
        private readonly Label combatText = new Label { name = "card-combat-text" };
        private readonly VisualElement toolbar = BoardScreen.Row("card-toolbar");
        private readonly CardPlayButtons plays;
        private readonly CardViewportLayout viewportLayout;
        private TurnDraft draft = new TurnDraft();
        private ActionDieContext context = new ActionDieContext();
        private bool interactive;
        private bool hoverSuspended;
        private bool allowPlay = true;
        private bool tableSource;
        private bool anchorPinnedSource;
        private Func<VisualElement> currentAvoid;
        private Vector2 defaultAnchor = new Vector2(342, 8);

        public CardInspector()
        {
            Root.AddToClassList("card-inspector");
            Root.style.display = DisplayStyle.None;
            var header = BoardScreen.Row("card-header");
            FloatingWindow.MarkHandle(header);
            header.Add(title);
            pin = IconButton.Create("pin", "Pin", () => { Pinned = !Pinned; Refresh(); });
            pin.name = "pin-card";
            pin.tooltip = "Pin this preview";
            header.Add(pin);
            var close = IconButton.Create("close", "Close", Close);
            close.name = "close-card";
            close.tooltip = "Close card";
            header.Add(close);
            Root.Add(header);
            plays = new CardPlayButtons(face => PlayRequested?.Invoke(CardId, face), () => ReactionRequested?.Invoke(CardId));
            toolbar.Add(plays.Root);
            Root.Add(toolbar);
            Root.Add(window);
            eventSection.AddToClassList("card-effect");
            combatSection.AddToClassList("card-effect");
            eventHeading.AddToClassList("card-effect-heading");
            combatHeading.AddToClassList("card-effect-heading");
            eventSection.Add(eventHeading);
            eventSection.Add(text);
            combatSection.Add(combatHeading);
            combatSection.Add(combatText);
            Root.Add(eventSection);
            Root.Add(combatSection);
            eventSection.RegisterCallback<ClickEvent>(_ => RequestDiscard(eventSection));
            combatSection.RegisterCallback<ClickEvent>(_ => RequestDiscard(combatSection));
            text.AddToClassList("card-rules");
            combatText.AddToClassList("card-rules");
            window.AddToClassList("muted");
            viewportLayout = new CardViewportLayout(Root, text, combatText, toolbar);
            Root.RegisterCallback<GeometryChangedEvent>(e =>
            {
                viewportLayout.LayoutChanged();
                if (PopoverPlacement.SizeChanged(e.oldRect, e.newRect))
                    PositionHover();
            });
            Root.RegisterCallback<AttachToPanelEvent>(_ => Application.focusChanged += FocusChanged);
            Root.RegisterCallback<DetachFromPanelEvent>(_ => { Application.focusChanged -= FocusChanged; CloseTransient(); });
        }

        public void Update(PlayerView latest)
        {
            if (view != null && (view.MatchId != latest.MatchId || view.You != latest.You))
                Close();
            view = latest;
            Refresh();
        }

        public void Show(string id, bool keep = false, bool allowPlay = true, Func<VisualElement> avoid = null, VisualElement origin = null)
        {
            if (!CardLibrary.Cards.ContainsKey(id) || Pinned && !keep)
                return;
            if (CardId != id)
                combat = view?.Decision?.Kind == DecisionKind.CombatCard;
            currentAvoid = avoid;
            Opening?.Invoke();
            CardId = id;
            this.allowPlay = allowPlay;
            tableSource = !allowPlay && IsActiveSource(id);
            anchorPinnedSource = keep && !Pinned && avoid != null;
            Pinned |= keep;
            Root.style.display = DisplayStyle.Flex;
            Root.BringToFront();
            Refresh();
            OpenedFrom?.Invoke(origin);
        }

        public void Close()
        {
            Pinned = false;
            CardId = "";
            currentAvoid = null;
            tableSource = false;
            anchorPinnedSource = false;
            eventSection.userData = null;
            combatSection.userData = null;
            Root.style.display = DisplayStyle.None;
        }

        public void ShowCombat(string id, bool keep = true, bool allowPlay = true, Func<VisualElement> avoid = null, VisualElement origin = null)
        {
            Show(id, keep, allowPlay, avoid, origin);
            if (CardId != id)
                return;
            combat = true;
            Refresh();
        }

        public void Observe(VisualElement element, string id, Func<bool> inCombat = null, Func<bool> allowPlay = null,
            Func<VisualElement> avoid = null)
        {
            element.AddToClassList(RuntimeTooltip.RichHoverClass);
            if (element is Button button)
                button.clicked += () => InspectActivated(element, id, inCombat, allowPlay, avoid);
            else
            {
                element.RegisterCallback<ClickEvent>(_ => InspectActivated(element, id, inCombat, allowPlay, avoid));
                element.RegisterCallback<KeyDownEvent>(e =>
                {
                    if (e.keyCode is KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Space)
                        InspectActivated(element, id, inCombat, allowPlay, avoid);
                });
            }
        }

        private void InspectActivated(VisualElement element, string id, Func<bool> inCombat,
            Func<bool> allowPlay, Func<VisualElement> avoid)
        {
            if (element.panel == null || hoverSuspended)
                return;
            if (inCombat?.Invoke() == true)
                ShowCombat(id, true, allowPlay?.Invoke() != false, avoid);
            else
                Show(id, true, allowPlay?.Invoke() != false, avoid);
        }

        public void Pin()
        {
            Pinned = true;
            Refresh();
        }

        public void CloseTransient()
        {
            if (!Pinned)
                Close();
        }

        public void SuspendHover(bool suspended)
        {
            hoverSuspended = suspended;
            if (suspended)
                CloseTransient();
        }

        private void FocusChanged(bool focused)
        {
            if (!focused)
            {
                CloseTransient();
            }
        }

        public void Actions(TurnDraft selection, ActionDieContext die, bool enabled)
        {
            draft = selection;
            context = die;
            interactive = enabled;
            if (CardLibrary.Cards.TryGetValue(CardId, out var card))
                RefreshPlay(card);
        }

        private void Refresh()
        {
            if (!CardLibrary.Cards.TryGetValue(CardId, out var card))
                return;
            title.text = card.Title;
            eventHeading.text = card.Deck == "special" ? "SPECIAL EVENT" : card.EventType.ToUpperInvariant() + " · EVENT";
            combatHeading.text = "COMBAT · " + string.Join(" / ", card.Initiative) + " · " + card.CombatTitle;
            text.text = CardLibrary.EventText(card, view?.Configuration);
            combatText.text = card.Combat;
            combatSection.style.display = card.HasCombat ? DisplayStyle.Flex : DisplayStyle.None;
            RefreshPlay(card);
            FitViewport();
            IconButton.SetIcon(pin, Pinned ? "pinned" : "pin");
            pin.EnableInClassList("selected", Pinned);
            Window?.Refresh();
            PositionHover();
        }

        public void SetDefaultAnchor(Vector2 anchor)
        {
            if (defaultAnchor == anchor)
                return;
            defaultAnchor = anchor;
            PositionHover();
        }

        public bool Inspecting(VisualElement source) => currentAvoid?.Invoke() == source;

        private void PositionHover()
        {
            if (Window == null || Root.parent == null)
                return;
            if (Pinned && (!anchorPinnedSource || Window.Restore()))
            {
                anchorPinnedSource = false;
                return;
            }
            var obstacle = currentAvoid?.Invoke();
            if (obstacle?.panel == null)
            {
                Window.SetDefaultAnchor(defaultAnchor);
                return;
            }
            if (Root.layout.width <= 0 || Root.layout.height <= 0 ||
                float.IsNaN(Root.layout.width) || float.IsNaN(Root.layout.height))
                return;
            var viewport = Root.parent;
            var minimum = viewport.WorldToLocal(obstacle.worldBound.min);
            var bounds = new Rect(minimum, viewport.WorldToLocal(obstacle.worldBound.max) - minimum);
            var placement = PopoverPlacement.Near(bounds.center, Root.layout.size, viewport.contentRect, new[] { bounds });
            Window.SetDefaultAnchor(new Vector2(viewport.contentRect.xMax - placement.xMax, placement.y));
            anchorPinnedSource = false;
        }

        public void FitViewport()
        {
            if (CardId.Length > 0)
                viewportLayout.Fit();
        }

        private void RequestDiscard(VisualElement section)
        {
            if (section.userData is DraftToken token)
                DiscardRequested?.Invoke(token.Id);
        }

        private void RefreshPlay(CardText card)
        {
            var combatWindow = view?.Decision?.Kind == DecisionKind.CombatCard;
            var selecting = draft.Kind is CommandKind.DiscardCards or CommandKind.DiscardProtection;
            var playable = allowPlay && view != null && !selecting && CardActions.CanPlay(view, draft, context, CardId);
            var discardable = view?.Decision?.Kind == DecisionKind.DiscardCards && view.Hand.Contains(CardId) &&
                view.Options.Any(option => option.Enabled && option.Kind == CommandKind.DiscardCards && option.EligibleIds.Contains(CardId));
            // Only the card's content carries this gesture. Header, Play, Pin and
            // Close retain their own clicks and can never become discard targets.
            eventSection.userData = combatSection.userData = discardable ? new DraftToken(DraftObject.Card, CardId) : null;
            eventSection.tooltip = combatSection.tooltip = discardable ? "Click to discard face down" : "";
            eventSection.EnableInClassList(PressFeedback.PressableClass, discardable);
            combatSection.EnableInClassList(PressFeedback.PressableClass, discardable);
            var costs = view == null ? Enumerable.Empty<EventCostOption>() : CardActions.Costs(view, draft, context, CardId);
            var freeEvent = view?.Decision?.Kind != DecisionKind.Action;
            plays.Refresh(combatWindow, freeEvent, interactive && playable, costs);
            toolbar.style.display = card.Deck == "special" || selecting || !allowPlay ? DisplayStyle.None : DisplayStyle.Flex;
            eventSection.EnableInClassList("playable-effect", playable && !combatWindow);
            combatSection.EnableInClassList("playable-effect", playable && combatWindow);
            eventSection.EnableInClassList("focused-effect", !combat && !combatWindow);
            combatSection.EnableInClassList("focused-effect", combat || combatWindow);
            Root.EnableInClassList("playable-preview", playable);
            window.text = selecting ? "Choose cards to discard" : playable
                ? combatWindow ? "Play face down" : freeEvent ? "Play this Event" : "Play with the selected die"
                : combatWindow ? "Cannot play in this battle" : "Select a matching die to play";
            if (view?.Decision?.SourceId == CardId || view?.EventLoss?.SourceId == CardId ||
                view?.EffectTrail.Any(e => e.SourceId == CardId) == true)
                window.text = "Resolving Event";
            if (card.Deck == "special")
                window.text = view?.Players.Any(p => p.TableCards.Contains(CardId)) == true
                    ? "On table · Hunt reaction with Gollum" : "Enters automatically when Sméagol leaves";
            if (!allowPlay && combatWindow) window.text = "Choose Combat cards in the battle window";
            if (tableSource && !allowPlay)
                window.text = IsActiveSource(CardId) ? "In play" : "Effect ended";
            ShowReaction();
        }

        private void ShowReaction()
        {
            if (!allowPlay || (Reaction?.Invoke(CardId) ?? "").Length == 0)
                return;
            plays.Reaction(interactive);
            toolbar.style.display = DisplayStyle.Flex;
            Root.EnableInClassList("playable-preview", true);
            window.text = "Use this reaction";
        }

        private bool IsActiveSource(string id) => view?.Effects.Any(e => e.SourceId == id) == true ||
            view?.Players.Any(p => p.TableCards.Contains(id)) == true;
    }
}
