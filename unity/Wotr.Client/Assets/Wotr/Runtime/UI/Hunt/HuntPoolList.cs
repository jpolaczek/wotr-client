using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-141: the public composition of the Hunt pool in the Hunt panel: one entry per printed face with its count,
    // each explained in its tooltip, and the special tiles waiting for Mordor. The server groups the tiles and never
    // sends their order; the list rebuilds only when that composition changes. UI-184: the heading stands on its own
    // line and the entries form a grid of equal cells (Styles/HuntPool.uss).
    public sealed class HuntPoolList
    {
        public VisualElement Root { get; } = new VisualElement { name = "hunt-pool" };
        private readonly Label heading = new Label { name = "hunt-pool-heading", enableRichText = false };
        private readonly VisualElement body = new VisualElement { name = "hunt-pool-body" };
        private readonly VisualElement tiles = BoardScreen.Row("hunt-pool-tiles");
        // UI-184: the special tiles waiting for Mordor stand on a line of their own, after their short mark.
        private readonly VisualElement preparedLine = BoardScreen.Row("hunt-pool-prepared-line");
        private readonly Label preparedHeading = new Label("Mordor") { name = "hunt-pool-prepared-heading" };
        private readonly VisualElement prepared = BoardScreen.Row("hunt-pool-tiles");
        private string shown = "";

        public HuntPoolList()
        {
            Root.AddToClassList("hunt-pool");
            heading.AddToClassList("hunt-pool-heading");
            preparedHeading.AddToClassList("hunt-pool-prepared-heading");
            preparedHeading.tooltip = "Special tiles in play · They join the pool when the Fellowship enters Mordor";
            prepared.name = "hunt-pool-prepared";
            tiles.name = "hunt-pool-entries";
            Root.Add(heading);
            body.AddToClassList("hunt-pool-body");
            body.Add(tiles);
            preparedLine.Add(preparedHeading);
            preparedLine.Add(prepared);
            body.Add(preparedLine);
            Root.Add(body);
        }

        public void Apply(HuntPoolView pool)
        {
            pool ??= new HuntPoolView();
            var key = Key(pool);
            if (key == shown)
                return;
            shown = key;
            heading.text = "In the pool · " + pool.Count + (pool.Unseen > 0 ? " · " + pool.Unseen + " face down elsewhere" : "");
            heading.tooltip = pool.Unseen > 0
                ? "Tiles in the Hunt pool. " + pool.Unseen + " of the listed tiles lie face down elsewhere (Corruption tiles), so their faces stay unknown."
                : "Tiles in the Hunt pool · Drawn tiles leave it; the drawing order is never shown";
            Fill(tiles, pool.Tiles, "hunt-pool-tile-");
            Fill(prepared, pool.Prepared, "hunt-pool-prepared-");
            var waiting = pool.Prepared.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            preparedHeading.style.display = waiting;
            prepared.style.display = waiting;
        }

        private static void Fill(VisualElement row, IEnumerable<HuntTileFaceView> faces, string prefix)
        {
            row.Clear();
            var index = 0;
            foreach (var face in faces)
                row.Add(Entry(face, prefix + index++));
        }

        private static VisualElement Entry(HuntTileFaceView face, string name)
        {
            var entry = new VisualElement { name = name, focusable = true };
            entry.AddToClassList("hunt-pool-tile");
            entry.tooltip = HuntTileText.Tooltip(face) + "\nIn this list · " + face.Count;
            var art = new HuntTileFace("small");
            art.Apply(face);
            entry.Add(art.Root);
            var count = new Label("×" + face.Count) { pickingMode = PickingMode.Ignore, enableRichText = false };
            count.AddToClassList("hunt-pool-count");
            entry.Add(count);
            return entry;
        }

        private static string Key(HuntPoolView pool) => pool.Count + "/" + pool.Unseen + "|" +
            string.Join(";", pool.Tiles.Select(Face)) + "|" + string.Join(";", pool.Prepared.Select(Face));

        private static string Face(HuntTileFaceView face) =>
            face.Source + ":" + face.Pool + ":" + face.Value + ":" + face.Reveal + ":" + face.Stop + "×" + face.Count;
    }
}
