using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-218: the political view. Every region with a nation is filled with that nation's colour (its current nation,
    // so Fate and the White Hand count); a settlement held by the other side is striped and outlined in its captor's
    // colour, the settlement gems' sapphire or ruby. The layer lies under the highlights, bands, names and markers, so
    // play goes on in this view. It repaints only when a nation or a controller changes, never while the camera moves.
    public sealed class MapPoliticalView
    {
        public const float FillAlpha = .5f;
        public const float StripeSpacing = 9f;
        public const float StripeWidth = 2.4f;
        public const float CaptureOutlineWidth = 3f;
        public const float CaptureInkAlpha = .9f;
        internal MapVectorLayer Layer { get; }
        public bool Active { get; private set; }
        private readonly IReadOnlyList<RegionGeometry> shapes;
        private readonly Dictionary<string, (string Nation, Side? Captor)> regions = new Dictionary<string, (string, Side?)>();
        private readonly Dictionary<string, List<(Vector2 From, Vector2 To)>> stripes =
            new Dictionary<string, List<(Vector2 From, Vector2 To)>>();
        private MapBorderPalette palette;

        internal MapPoliticalView(MapGeometry geometry, Vector2 size)
        {
            shapes = geometry.Regions;
            Layer = new MapVectorLayer("map-political", size.x, size.y, context => Draw(context.painter2D));
            Layer.style.display = DisplayStyle.None;
        }

        // The other side holds the region: its controller differs from its original owner.
        public static Side? Captor(RegionDefinition region, IReadOnlyDictionary<string, Side?> control) =>
            control.TryGetValue(region.Id, out var holder) && holder != null && holder != region.OriginalOwner ? holder : null;

        internal void SetActive(bool active)
        {
            Active = active;
            Layer.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        }

        internal void Update(BoardPresentation data)
        {
            palette = data.Borders.Palette;
            var changed = false;
            foreach (var shape in shapes)
            {
                if (!data.Regions.TryGetValue(shape.Id, out var region))
                    continue;
                var entry = (region.Nation, Captor(region, data.Control));
                if (regions.TryGetValue(shape.Id, out var previous) && previous == entry)
                    continue;
                regions[shape.Id] = entry;
                changed = true;
            }
            if (changed)
                Layer.MarkDirtyRepaint();
        }

        private void Draw(Painter2D painter)
        {
            if (palette == null)
                return;
            foreach (var shape in shapes)
                if (regions.TryGetValue(shape.Id, out var entry) && entry.Nation != null &&
                    palette.Nations.TryGetValue(entry.Nation, out var nation))
                    MapVectorDrawing.Fill(painter, shape.Rings, new Color(nation.r, nation.g, nation.b, FillAlpha));
            // Captures go over every fill, so a neighbouring nation never covers a captor's outline.
            foreach (var shape in shapes)
                if (regions.TryGetValue(shape.Id, out var entry) && entry.Captor != null)
                    DrawCapture(painter, shape, CaptorInk(entry.Captor.Value));
        }

        private void DrawCapture(Painter2D painter, RegionGeometry shape, Color ink)
        {
            if (!stripes.TryGetValue(shape.Id, out var segments))
                stripes[shape.Id] = segments = MapHatching.Segments(shape.Rings, StripeSpacing);
            painter.strokeColor = ink;
            painter.lineWidth = StripeWidth;
            painter.lineCap = LineCap.Butt;
            painter.BeginPath();
            foreach (var (from, to) in segments)
            {
                painter.MoveTo(from);
                painter.LineTo(to);
            }
            painter.Stroke();
            MapVectorDrawing.Outline(painter, shape.Rings, ink, CaptureOutlineWidth);
        }

        private static Color CaptorInk(Side captor)
        {
            var ink = SettlementGem.Palette(captor).Mid;
            ink.a = CaptureInkAlpha;
            return ink;
        }
    }
}
