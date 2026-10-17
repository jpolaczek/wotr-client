using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    internal sealed class VictoryScoreColumn
    {
        private int pageSize = 3;
        public VisualElement Root { get; } = new VisualElement();
        private readonly Side side;
        private readonly Label heading = new Label();
        private readonly Label empty = new Label();
        private readonly VisualElement rows = new VisualElement();
        private readonly VisualElement pages = BoardScreen.Row("victory-pages");
        private readonly Label pageLabel = new Label();
        private readonly Button previous, next;
        private readonly Action<string> locate;
        private readonly Dictionary<string, ScoreRow> entries = new Dictionary<string, ScoreRow>();
        private List<VictoryRegionView> regions = new List<VictoryRegionView>();
        private int page;

        public VictoryScoreColumn(Side side, Action<string> locate)
        {
            this.side = side;
            this.locate = locate;
            Root.name = "victory-score-" + side;
            Root.AddToClassList("victory-score");
            if (side == Side.Shadow)
                Root.AddToClassList("victory-score-shadow");
            heading.name = "victory-points-" + side;
            heading.AddToClassList("victory-side");
            Root.Add(heading);
            Root.Add(rows);
            empty.name = "victory-empty-" + side;
            empty.AddToClassList("muted");
            rows.Add(empty);
            previous = IconButton.Create("back", "Previous", () => ChangePage(-1));
            previous.name = "victory-previous-" + side;
            previous.tooltip = "Previous captured settlements";
            next = IconButton.Create("next", "Next", () => ChangePage(1));
            next.name = "victory-next-" + side;
            next.tooltip = "Next captured settlements";
            pages.Add(previous);
            pages.Add(pageLabel);
            pages.Add(next);
            Root.Add(pages);
        }

        public void Apply(PlayerView view)
        {
            var score = VictoryPresentation.Score(view, side);
            heading.text = VictoryPresentation.SideCode(side) + " " + VictoryPresentation.ScoreText(view, side);
            heading.tooltip = BoardScreen.SideName(side) + " control these enemy settlements. " + score?.Source;
            heading.EnableInClassList("threshold-reached", score?.ThresholdReached == true && !view.Winner.HasValue);
            regions = score?.Regions.OrderBy(region => region.Name, StringComparer.Ordinal).ToList() ?? new List<VictoryRegionView>();
            var ids = new HashSet<string>(regions.Select(region => region.RegionId));
            foreach (var id in entries.Keys.Where(id => !ids.Contains(id)).ToArray())
            {
                entries[id].Root.RemoveFromHierarchy();
                entries.Remove(id);
            }
            for (var index = 0; index < regions.Count; index++)
            {
                var region = regions[index];
                if (!entries.TryGetValue(region.RegionId, out var row))
                {
                    row = new ScoreRow(region.RegionId, locate);
                    entries.Add(region.RegionId, row);
                    rows.Add(row.Root);
                }
                row.Apply(region, side);
                if (rows.IndexOf(row.Root) != index + 1)
                    rows.Insert(index + 1, row.Root);
            }
            empty.text = score == null ? "Breakdown unavailable" : "No captured cities or strongholds";
            empty.style.display = regions.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            page = Math.Min(page, Math.Max(0, (regions.Count - 1) / pageSize));
            RenderPage();
        }

        public void ResetPage()
        {
            page = 0;
            RenderPage();
        }

        public void SetPageSize(int size)
        {
            if (pageSize == size)
                return;
            var first = page * pageSize;
            var focused = Root.panel?.focusController.focusedElement as VisualElement;
            var focusedIndex = regions.FindIndex(region => entries.TryGetValue(region.RegionId, out var row) &&
                focused != null && (row.Root == focused || row.Root.Contains(focused)));
            pageSize = size;
            page = (focusedIndex >= 0 ? focusedIndex : first) / pageSize;
            RenderPage();
        }

        private void ChangePage(int direction)
        {
            page += direction;
            RenderPage();
        }

        private void RenderPage()
        {
            for (var index = 0; index < regions.Count; index++)
                entries[regions[index].RegionId].Root.style.display = index / pageSize == page ? DisplayStyle.Flex : DisplayStyle.None;
            var count = Math.Max(1, (regions.Count + pageSize - 1) / pageSize);
            pages.style.display = count > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            pageLabel.text = (page + 1) + " / " + count;
            previous.SetEnabled(page > 0);
            next.SetEnabled(page + 1 < count);
        }

        private sealed class ScoreRow
        {
            public VisualElement Root { get; } = new VisualElement();
            private readonly Label title = new Label();
            private readonly Label detail = new Label();
            private readonly Button locate;

            public ScoreRow(string id, Action<string> focus)
            {
                Root.name = "victory-region-" + id;
                Root.AddToClassList("victory-region");
                var text = new VisualElement();
                text.AddToClassList("victory-region-text");
                title.AddToClassList("victory-region-name");
                detail.AddToClassList("victory-region-detail");
                text.Add(title);
                text.Add(detail);
                Root.Add(text);
                locate = IconButton.Create("locate", "Show on the map", () => focus(id));
                locate.name = "victory-locate-" + id;
                Root.Add(locate);
            }

            public void Apply(VictoryRegionView region, Side owner)
            {
                title.text = VictoryPresentation.SettlementSymbol(region.Settlement) + " " + region.Name;
                detail.text = VictoryPresentation.SideCode(owner) + " control · " + region.Settlement + " · " + region.Points;
                locate.tooltip = "Locate " + region.Name + " · " + region.Points + (region.Points == 1 ? " point" : " points");
            }
        }
    }
}
