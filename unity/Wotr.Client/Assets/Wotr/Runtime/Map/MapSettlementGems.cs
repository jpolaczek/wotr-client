using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-406 (TABLETOP_FIGURES_UX, "Settlements"): every settlement's gem is its own element on a layer of its own, under
    // the Mordor track and the stands. Each is painted once, at its design size (UI-195's constant size), round the middle
    // of its box, and stands on its region's anchor by a transform only (MapStand: translate and scale, written when they
    // change), at the scale clamp(zoom / reference, .85, 1.4) (MapStandScale.Gem). So a camera move — a pan, a zoom, a
    // glide — repaints no gem: the mesh is kept and only moved. A gem repaints when its settlement or its controller
    // changes, on a new board, never per frame.
    public sealed class MapSettlementGems
    {
        // Room for the largest stone (a Stronghold in its setting), its side band and its shadow.
        public static readonly Vector2 Box = new Vector2(32, 32);
        private static readonly Vector2 Middle = Box / 2;
        public VisualElement Layer { get; }
        private readonly MapCanvas map;
        private readonly Dictionary<string, Gem> gems = new Dictionary<string, Gem>();
        private readonly List<string> stale = new List<string>();

        public MapSettlementGems(MapCanvas map)
        {
            this.map = map;
            Layer = new VisualElement { name = "map-settlements", pickingMode = PickingMode.Ignore };
            Layer.AddToClassList("map-settlements");
            Layer.StretchToParentSize();
        }

        // How often the gems were painted, in all.
        public int Repaints
        {
            get
            {
                var total = 0;
                foreach (var gem in gems.Values)
                    total += gem.Repaints;
                return total;
            }
        }

        // The element of a region's gem, or null where the region has no settlement.
        public VisualElement Find(string region) => gems.TryGetValue(region, out var gem) ? gem.Element : null;

        // Where a region's stone (its crown and side band, not its shadow) lies on the map as last placed, or null.
        public Rect? Bounds(string region) => gems.TryGetValue(region, out var gem) ? gem.Stand.Bounds : (Rect?)null;

        // On a new board: a gem for every settlement, repainted only where its kind or controller changed.
        public void Apply(BoardPresentation data)
        {
            stale.Clear();
            foreach (var region in gems.Keys)
                if (!data.Regions.TryGetValue(region, out var definition) || definition.Settlement == null)
                    stale.Add(region);
            foreach (var region in stale)
            {
                gems[region].Stand.Remove();
                gems.Remove(region);
            }
            foreach (var region in data.Regions.Values)
                if (region.Settlement != null)
                    Show(region.Id, region.Settlement, data.Control.TryGetValue(region.Id, out var side) ? side : null, data);
        }

        private void Show(string region, string settlement, Side? controller, BoardPresentation data)
        {
            if (!gems.TryGetValue(region, out var gem))
            {
                gem = new Gem(region);
                gems.Add(region, gem);
                Layer.Add(gem.Stand.Root);
            }
            var anchor = data.Shapes[region].Anchor;
            gem.Anchor = new Vector2(anchor[0], anchor[1]);
            gem.Show(settlement, controller);
        }

        // Per camera change: each gem's middle on its anchor at the gem scale; nothing is painted.
        public void Layout(float scale)
        {
            foreach (var gem in gems.Values)
                gem.Stand.Place(map.Camera.ToScreen(gem.Anchor), scale);
        }

        private sealed class Gem
        {
            public VisualElement Element { get; }
            public MapStand Stand { get; }
            public Vector2 Anchor;
            public int Repaints { get; private set; }
            private string settlement;
            private Side? controller;
            private bool assigned;
            private SettlementGem stone;

            public Gem(string region)
            {
                Element = new VisualElement { name = "settlement-gem-" + region, pickingMode = PickingMode.Ignore };
                Element.AddToClassList("settlement-gem");
                Element.style.position = Position.Absolute;
                Element.style.width = Box.x;
                Element.style.height = Box.y;
                Element.generateVisualContent += Paint;
                Stand = new MapStand(Element, Box);
            }

            public void Show(string kind, Side? side)
            {
                if (assigned && kind == settlement && side == controller)
                    return;
                settlement = kind;
                controller = side;
                assigned = true;
                stone = SettlementGem.For(kind, side);
                Stand.Design(Middle, Extent(stone.Silhouette(Middle)));
                Element.MarkDirtyRepaint();
            }

            private static Rect Extent(Vector2[] points)
            {
                var min = points[0];
                var max = points[0];
                foreach (var point in points)
                {
                    min = Vector2.Min(min, point);
                    max = Vector2.Max(max, point);
                }
                return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            }

            private void Paint(MeshGenerationContext context)
            {
                if (stone == null)
                    return;
                MapSettlementVisual.Draw(context.painter2D, Middle, stone);
                Repaints++;
            }
        }
    }
}
