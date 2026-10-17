using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    internal sealed class MapBorderVisual
    {
        public MapVectorLayer Layer { get; }
        public int Builds { get; private set; }
        private readonly MapBorderData data;
        private readonly MapBorderPath path;
        private readonly MapBandPath bands;
        private readonly float keyline;
        private MapNationBands nations;

        public MapBorderVisual(MapBorderData data, MapBorderPath path, MapBandPath bands,
            float keyline, Vector2 size)
        {
            this.data = data;
            this.path = path;
            this.bands = bands;
            this.keyline = keyline;
            Layer = new MapVectorLayer(path.Id, size.x, size.y, context => Draw(context.painter2D));
        }

        public void Update(IReadOnlyDictionary<string, RegionDefinition> regions)
        {
            var next = MapBorderData.Bands(path, MapBorderKind.Passage, regions);
            if (Builds > 0 && next.Positive == nations.Positive && next.Negative == nations.Negative)
                return;
            nations = next;
            Builds++;
            Layer.MarkDirtyRepaint();
        }

        private void Draw(Painter2D painter)
        {
            var points = data.Geometry[path.Id].Points;
            for (var index = 0; index < path.Parts.Count; index++)
            {
                var part = path.Parts[index];
                if (!MapBorderData.Passable(part.Kind))
                    continue;
                var band = bands.Parts[index];
                DrawNation(painter, band.Positive, nations.Positive);
                DrawNation(painter, band.Negative, nations.Negative);
                MapVectorDrawing.Stroke(painter, points, part, data.Palette.Keyline,
                    data.Palette.Style.PassageWidth + 2 * keyline);
            }
        }

        // UI-143: the band is a translucent wash of the nation's hue over the map, edged by a keyline-wide stroke of a
        // darker shade of the same hue; the passage line drawn afterwards covers the inner edge.
        private void DrawNation(Painter2D painter, List<MapContourRing> band, string nation)
        {
            if (nation == null)
                return;
            MapVectorDrawing.Fill(painter, band, data.Palette.NationWashes[nation]);
            MapVectorDrawing.Outline(painter, band, data.Palette.NationEdges[nation], keyline * MapBorderPalette.EdgeKeylines);
        }
    }
}
