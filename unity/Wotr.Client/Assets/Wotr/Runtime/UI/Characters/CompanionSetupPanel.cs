using System;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // A private host for the existing tray and sender, not another setup flow.
    public sealed class CompanionSetupPanel
    {
        public VisualElement Root { get; } = new VisualElement { name = "companion-setup-panel", focusable = true };
        public VisualElement DecisionHost { get; } = new VisualElement { name = "companion-setup-decision" };
        public Button Tab { get; }
        public string ComparedFigure { get; set; } = "";
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
                ComparedFigure = "";
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
            {
                ComparedFigure = "";
                Close();
            }
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

        public static bool ContainsInput(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
                if (current.name == "companion-setup-panel")
                    return true;
            return false;
        }
    }
}
