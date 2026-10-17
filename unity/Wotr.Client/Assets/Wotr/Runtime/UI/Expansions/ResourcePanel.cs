using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Hosts the existing decision tray; visibility never edits or submits its draft.
    public sealed class ResourcePanel
    {
        public VisualElement Root { get; } = new VisualElement { name = "resource-panel" };
        public VisualElement DecisionHost { get; } = new VisualElement { name = "resource-decision-host" };
        public Button Tab { get; }
        public ComponentActionView CouncilChoices { get; } = new ComponentActionView();
        public bool IsOpen => Root.style.display.value != DisplayStyle.None;
        private readonly Label consequence = new Label { name = "resource-consequence" };
        private VisualElement tray;
        private VisualElement dock;
        private string match = "";
        private string observedDecision = "";
        private bool active;

        public ResourcePanel()
        {
            Root.AddToClassList("resource-panel");
            Tab = BoardScreen.Button("Dice & tokens", Open);
            Tab.name = "resource-decision-tab";
            Tab.AddToClassList("resource-decision-tab");
            var header = BoardScreen.Row("information-heading");
            FloatingWindow.MarkHandle(header);
            header.Add(BoardScreen.Title("Dice & tokens"));
            var close = IconButton.Create("close", "Close", Close);
            close.name = "resource-panel-close";
            close.tooltip = "Close this panel · Keep your selection";
            header.Add(close);
            Root.Add(header);
            consequence.AddToClassList("resource-consequence");
            Root.Add(consequence);
            Root.Add(CouncilChoices.Root);
            Root.Add(DecisionHost);
            Tab.style.display = DisplayStyle.None;
            Close();
        }

        public void Attach(VisualElement decisionTray, VisualElement decisionDock)
        {
            tray = decisionTray;
            dock = decisionDock;
        }

        public void Context(PlayerView view, TurnDraft draft)
        {
            if (match != view.MatchId)
            {
                match = view.MatchId;
                observedDecision = "";
                Close();
            }
            active = ResourcePresentation.OwnsDecision(view, draft);
            if (active && tray?.parent != DecisionHost)
                DecisionHost.Add(tray);
            else if (!active && tray?.parent == DecisionHost)
                dock.Add(tray);
            Tab.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            Tab.EnableInClassList("legal-target", active);
            consequence.text = active ? ResourcePresentation.Consequence(view, draft) : "";
            var decision = view.Decision?.Id ?? "";
            var changed = decision != observedDecision;
            observedDecision = decision;
            if (!active)
                Close();
            else if (changed)
                Open();
        }

        public void Open()
        {
            if (!active)
                return;
            Root.style.display = DisplayStyle.Flex;
            Root.BringToFront();
        }

        public void Close() => Root.style.display = DisplayStyle.None;
    }
}
