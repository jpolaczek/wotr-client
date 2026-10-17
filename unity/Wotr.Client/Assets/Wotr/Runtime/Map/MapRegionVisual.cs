using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    public enum MapRegionEmphasis { None, Protection, Hover, Source, Selected, Target, Attack, Pressed }

    internal sealed class MapRegionVisual
    {
        public MapVectorLayer Layer { get; }
        public MapRegionEmphasis Emphasis { get; private set; }

        public MapRegionVisual(RegionGeometry shape, Vector2 size)
        {
            Layer = new MapVectorLayer("map-fill-" + shape.Id, size.x, size.y, context =>
            {
                if (Emphasis != MapRegionEmphasis.None)
                    MapVectorDrawing.Fill(context.painter2D, shape.Rings, ColorFor(Emphasis));
            });
        }

        public void Update(MapRegionEmphasis emphasis)
        {
            if (Emphasis == emphasis)
                return;
            Emphasis = emphasis;
            Layer.MarkDirtyRepaint();
        }

        public static Color ColorFor(MapRegionEmphasis emphasis) => emphasis switch
        {
            // UI-114: the terrain under a held pointer darkens until the release chooses it.
            MapRegionEmphasis.Pressed => new Color(.26f, .17f, .05f, .22f),
            // UI-224: targets are filled more strongly; their edges also glow (MapTargetGlow).
            MapRegionEmphasis.Attack => new Color(.8f, .17f, .12f, .36f),
            MapRegionEmphasis.Target => new Color(.16f, .65f, .34f, .34f),
            MapRegionEmphasis.Selected => new Color(.9f, .56f, .12f, .26f),
            MapRegionEmphasis.Source => new Color(.96f, .7f, .24f, .20f),
            MapRegionEmphasis.Protection => new Color(.25f, .69f, .95f, .16f),
            MapRegionEmphasis.Hover => new Color(1f, .95f, .78f, .10f),
            _ => Color.clear
        };
    }
}
