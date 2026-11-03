using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-453 (owner, 2026-11-03: "Monsters Roused's regions are green but do not stand out enough; they should pulse"): every
    // region a placement may take (MapDraftOverlay.PlacementTargets — a recruitment, Monsters Roused's halves, a homeland)
    // breathes. One retained layer over the region fills paints them a bright green; its pulse only toggles a class whose
    // opacity transition runs under .motion-enabled, in the targets' 750 ms rhythm (MapTargetGlow), so Reduced motion keeps
    // them lit and still, and panning or zooming never repaints it. It repaints only when the set changes.
    public sealed class MapPlacementPulse
    {
        public const string ClassName = "map-placement-pulse";
        public const string DimClass = "map-placement-pulse-dim";
        public static readonly Color Fill = new Color(.42f, .95f, .55f, .42f);
        internal MapVectorLayer Layer { get; }
        private readonly Dictionary<string, RegionGeometry> shapes = new Dictionary<string, RegionGeometry>();
        private readonly HashSet<string> regions = new HashSet<string>();
        private readonly IVisualElementScheduledItem pulse;

        internal MapPlacementPulse(MapGeometry geometry, Vector2 size)
        {
            foreach (var shape in geometry.Regions)
                shapes[shape.Id] = shape;
            Layer = new MapVectorLayer(ClassName, size.x, size.y, context => Draw(context.painter2D));
            Layer.AddToClassList(ClassName);
            pulse = Layer.schedule.Execute(() => Layer.ToggleInClassList(DimClass)).Every(MapTargetGlow.PulseMilliseconds);
            pulse.Pause();
        }

        public bool Pulsing => regions.Count > 0;

        public bool Pulses(string region) => regions.Contains(region);

        // The regions to pulse this frame; the layer repaints only when the set changes.
        internal void Update(IReadOnlyList<string> placements)
        {
            if (!MapTargetGlow.Changed(regions, placements))
                return;
            Layer.MarkDirtyRepaint();
            if (Pulsing)
                pulse.Resume();
            else
            {
                pulse.Pause();
                Layer.RemoveFromClassList(DimClass);
            }
        }

        private void Draw(Painter2D painter)
        {
            foreach (var id in regions)
                if (shapes.TryGetValue(id, out var shape))
                    MapVectorDrawing.Fill(painter, shape.Rings, Fill);
        }
    }
}
