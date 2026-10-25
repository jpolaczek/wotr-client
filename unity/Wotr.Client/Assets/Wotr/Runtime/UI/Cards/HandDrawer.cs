using System;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class HandDrawer
    {
        public Button Tab { get; }
        public bool IsOpen { get; private set; }
        public bool Pinned { get; private set; }
        // UI-91: the right column hand is permanent; Escape and other panels never close it.
        public bool Docked { get; set; }
        public FloatingWindow Window { get; set; }
        public VisualElement InspectionBoundary { get; set; }
        public VisualElement Header { get; } = FloatingWindow.Header("Cards");
        public event Action Opening;
        private readonly VisualElement surface;
        private readonly VisualElement hand;
        private readonly CardInspector inspector;
        private int excess;
        private string match = "";
        private Side? side;
        private string openedDiscard;

        public HandDrawer(VisualElement surface, VisualElement hand, CardInspector inspector)
        {
            this.surface = surface;
            this.hand = hand;
            this.inspector = inspector;
            Tab = BoardScreen.Button("Cards", Open);
            Tab.name = "cards-tab";
            Tab.AddToClassList("cards-tab");
            Tab.AddToClassList(RuntimeTooltip.RichHoverClass);
            hand.AddToClassList("hand-drawer");
            hand.style.display = DisplayStyle.None;
            var close = IconButton.Create("close", "Close", Close);
            close.name = "close-hand";
            Header.Add(close);
            Bind();
        }

        private void Bind()
        {
            surface.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Escape)
                    Close();
            }, TrickleDown.TrickleDown);
            surface.RegisterCallback<DetachFromPanelEvent>(e =>
            {
                if (e.target == surface)
                    Close();
            });
        }

        public void Apply(PlayerView view)
        {
            if (match != view.MatchId || side != view.You)
            {
                Close();
                openedDiscard = null;
            }
            match = view.MatchId;
            side = view.You;
            var ownDiscard = view.Decision?.Kind == DecisionKind.DiscardCards && view.DecisionOwner == view.You;
            excess = ownDiscard ? Math.Max(0, view.Hand.Count - 6) : 0;
            ValuePulse.Set(Tab, excess > 0 ? "Discard " + excess : "Cards " + view.Hand.Count);
            if (excess > 0 && openedDiscard != view.Decision.Id)
            {
                openedDiscard = view.Decision.Id;
                Open();
            }
            Refresh();
        }

        public void Highlight(bool legal, bool selecting)
        {
            Tab.EnableInClassList("legal-target", legal || selecting);
            Tab.tooltip = excess > 0 ? "Click a card, then click it again to discard it face down" : selecting ? "Choose cards from your hand"
                : legal ? "Cards · Highlighted plays and decks" : "Cards · Inspect your hand";
        }

        public void Open()
        {
            Pinned = true;
            Refresh();
        }

        public void Close()
        {
            if (!Docked)
            {
                Pinned = false;
                Refresh();
            }
            inspector.CloseTransient();
        }

        public void CloseTransient() { if (!Pinned) Close(); }

        public void PreviewDrag(DraftToken source)
        {
            inspector.SuspendHover(source != null);
        }

        private void Refresh()
        {
            var open = Pinned;
            if (open == IsOpen)
                return;
            IsOpen = open;
            if (open)
            {
                Opening?.Invoke();
                hand.BringToFront();
                inspector.Root.BringToFront();
            }
            hand.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            Tab.EnableInClassList("selected", open);
            Window?.Refresh();
        }

    }
}
