using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-307/UI-311: the delta badge — per entry its figure art (a replacement's two with an arrow), the nation's crest and
    // a signed count — beside a force on the map (MapObservedTrace) or over a battle column's figures (BattlePresenter).
    public static class ObservedDelta
    {
        public static VisualElement Plate(IEnumerable<FigureDelta> entries)
        {
            var badge = new VisualElement { name = "observed-delta", pickingMode = PickingMode.Ignore };
            badge.AddToClassList("observed-delta");
            foreach (var entry in entries)
                badge.Add(Entry(entry));
            return badge;
        }

        private static VisualElement Entry(FigureDelta delta)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("observed-delta-entry");
            if (delta.From.HasValue)
            {
                row.Add(Figure(delta.From.Value));
                row.Add(new Label("→") { pickingMode = PickingMode.Ignore });
            }
            row.Add(Figure(delta.Art));
            if (delta.Nation.Length > 0)
            {
                var crest = new VisualElement { pickingMode = PickingMode.Ignore };
                crest.AddToClassList("nation-emblem");
                crest.AddToClassList("nation-emblem-" + delta.Nation);
                row.Add(crest);
            }
            var count = new Label(delta.Text) { name = "observed-delta-count", pickingMode = PickingMode.Ignore };
            count.AddToClassList(delta.Count < 0 ? "observed-delta-loss" : "observed-delta-gain");
            row.Add(count);
            return row;
        }

        private static VisualElement Figure(FigureArt art)
        {
            var image = FigureArtwork.Image(art);
            image.pickingMode = PickingMode.Ignore;
            image.AddToClassList("observed-delta-figure");
            return image;
        }
    }
}
