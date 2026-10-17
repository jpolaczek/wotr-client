using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class BoardInformation
    {
        private enum InformationMode { None, Region, Rules }
        public VisualElement Root { get; } = new VisualElement { name = "board-information" };
        private readonly BoardPresentation data;
        private readonly Label title = BoardScreen.Title("");
        private readonly VisualElement content = new VisualElement { name = "information-content" };
        private PlayerView view;
        private InformationMode mode;
        private string subject = "", force = "";

        public BoardInformation(BoardPresentation data)
        {
            this.data = data;
            Root.AddToClassList("board-information");
            Root.style.display = DisplayStyle.None;
            var heading = BoardScreen.Row("information-heading");
            FloatingWindow.MarkHandle(heading);
            heading.Add(title);
            var close = IconButton.Create("close", "Close", Close);
            close.name = "close-information";
            heading.Add(close);
            Root.Add(heading);
            Root.Add(content);
        }

        public void Apply(PlayerView latest)
        {
            view = latest;
            if (mode != InformationMode.None)
                Render();
        }

        public void Region(string id) => Open(InformationMode.Region, id);
        public void Rules() => Open(InformationMode.Rules, "");

        private void Open(InformationMode next, string id)
        {
            if (view == null)
                return;
            if (mode != next || subject != id)
            {
                content.Clear();
                force = "";
            }
            mode = next;
            subject = id;
            Root.style.display = DisplayStyle.Flex;
            Render();
            Root.BringToFront();
        }

        public void Close()
        {
            mode = InformationMode.None;
            content.Clear();
            Root.style.display = DisplayStyle.None;
        }

        private void Render()
        {
            content.Clear();
            if (mode == InformationMode.Rules)
            {
                title.text = "Game rules";
                ConfigurationSummary.Render(content, view.Configuration);
            }
            else if (mode == InformationMode.Region && data.Regions.TryGetValue(subject, out var region))
            {
                title.text = region.Name;
                view.Control.TryGetValue(subject, out var control);
                content.Add(new Label((region.Settlement ?? "Wilderness") + " · " +
                    (region.Nation == null ? "No nation" : FigureGrid.Name(region.Nation)) + " · " +
                    (control.HasValue ? BoardScreen.SideName(control.Value) + " control" : "Neutral control"))
                { name = "inspected-region-state" });
                force = RegionInformation.Render(content, view, subject, force, selected => { force = selected; Render(); });
            }
        }
    }
}
