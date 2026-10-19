using System;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // A private host for the existing tray and sender, not another setup flow. UI-269: the homelands only; the private
    // versions moved to the Fellowship panel.
    public sealed class CompanionSetupPanel
    {
        public VisualElement Root { get; } = new VisualElement { name = "companion-setup-panel", focusable = true };
        public VisualElement DecisionHost { get; } = new VisualElement { name = "companion-setup-decision" };
        public Button Tab { get; }
        public bool IsOpen => Root.style.display.value != DisplayStyle.None;
        public bool Active { get; private set; }
        public event Action Opening;
        private VisualElement tray;
        private VisualElement dock;
        private string context = "";
        private string decision = "";
        private readonly Label title;

        public CompanionSetupPanel()
        {
            Root.AddToClassList("companion-setup-panel");
            Tab = BoardScreen.Button("Companions", Open);
            Tab.name = "companion-setup-tab";
            Tab.AddToClassList("companion-setup-tab");
            Tab.tooltip = "Choose companion versions and the starting Guide";
            var header = FloatingWindow.Header("Prepare the Fellowship");
            title = header.Q<Label>();
            var close = IconButton.Create("close", "Close", Close);
            close.name = "close-companion-setup";
            close.tooltip = "Close · Keep these choices";
            header.Add(close);
            Root.Add(header);
            Root.Add(DecisionHost);
            Root.style.display = DisplayStyle.None;
            Tab.style.display = DisplayStyle.None;
        }

        public void Attach(VisualElement decisionTray, VisualElement decisionDock)
        {
            tray = decisionTray;
            dock = decisionDock;
        }

        public void Context(PlayerView view)
        {
            var nextContext = view.MatchId + "/" + view.You;
            if (context != nextContext)
            {
                context = nextContext;
                decision = "";
                Close();
            }
            Active = CompanionSetupPresentation.Owns(view);
            var homelands = CouncilHomelandPresentation.Owns(view);
            Root.EnableInClassList("council-homeland-panel", homelands);
            title.text = homelands ? "Choose starting homelands" : "Prepare the Fellowship";
            Tab.text = homelands ? "Homelands" : "Companions";
            Tab.tooltip = homelands ? "Place companions or keep them with the Fellowship"
                : "Choose companion versions and the starting Guide";
            if (Active && tray != null && tray.parent != DecisionHost)
                DecisionHost.Add(tray);
            else if (!Active && tray?.parent == DecisionHost)
                dock.Add(tray);
            Tab.style.display = Active ? DisplayStyle.Flex : DisplayStyle.None;
            var nextDecision = Active ? view.Decision.Id : "";
            if (!Active)
                Close();
            else if (decision != nextDecision)
                Open();
            decision = nextDecision;
        }

        public void Open()
        {
            if (!Active)
                return;
            Opening?.Invoke();
            Root.style.display = DisplayStyle.Flex;
            Root.BringToFront();
            Tab.EnableInClassList("selected", true);
        }

        public void Close()
        {
            Root.style.display = DisplayStyle.None;
            Tab.EnableInClassList("selected", false);
        }

        public void HandleKey(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.Escape || !IsOpen || FloatingWindow.IsDragging(e.target as VisualElement))
                return;
            Close();
            Tab.Focus();
            e.StopImmediatePropagation();
        }

        // UI-269: the version cards, now on the Fellowship panel, keep the keyboard to themselves as well.
        public static bool ContainsInput(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
                if (current.name is "companion-setup-panel" or "companion-setup-choices")
                    return true;
            return false;
        }
    }
}
