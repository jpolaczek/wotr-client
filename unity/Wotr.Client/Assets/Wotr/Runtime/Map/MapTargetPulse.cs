using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-453 (owner: Monsters Roused's regions should pulse) and UI-468 (owner, 2026-11-03: "the regions that can be
    // clicked and are suggested by colour should pulse more clearly; now it is barely noticeable, especially against
    // similar colours all around"): every region the current step may take breathes over its still fill — a move's or a
    // placement's in a bright mint, an attack's in a bright red, both lighter than any land of the map so the breath
    // reads against green and red regions alike. One retained layer above the region fills; its pulse only toggles a
    // class whose opacity transition runs under .motion-enabled, in the targets' 750 ms rhythm (MapTargetGlow), so
    // Reduced motion keeps them lit and still, and panning or zooming never repaints it. It repaints only when a set
    // changes.
    public sealed class MapTargetPulse
    {
        public const string ClassName = "map-target-pulse";
        public const string DimClass = "map-target-pulse-dim";
        public static readonly Color EnterFill = new Color(.66f, 1f, .72f, .5f);
        public static readonly Color AttackFill = new Color(1f, .5f, .42f, .5f);
        internal MapVectorLayer Layer { get; }
        private readonly Dictionary<string, RegionGeometry> shapes = new Dictionary<string, RegionGeometry>();
        private readonly HashSet<string> enter = new HashSet<string>();
        private readonly HashSet<string> attack = new HashSet<string>();
        private readonly IVisualElementScheduledItem pulse;

        internal MapTargetPulse(MapGeometry geometry, Vector2 size)
        {
            foreach (var shape in geometry.Regions)
                shapes[shape.Id] = shape;
            Layer = new MapVectorLayer(ClassName, size.x, size.y, context => Draw(context.painter2D));
            Layer.AddToClassList(ClassName);
            pulse = Layer.schedule.Execute(() => Layer.ToggleInClassList(DimClass)).Every(MapTargetGlow.PulseMilliseconds);
            pulse.Pause();
        }

        public bool Pulsing => enter.Count > 0 || attack.Count > 0;

        public bool Pulses(string region) => enter.Contains(region) || attack.Contains(region);

        // The regions to breathe this frame; the layer repaints only when a set changes.
        internal void Update(IReadOnlyList<string> entering, IReadOnlyList<string> attacking)
        {
            if (!MapTargetGlow.Changed(enter, entering) & !MapTargetGlow.Changed(attack, attacking))
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
            Fill(painter, enter, EnterFill);
            Fill(painter, attack, AttackFill);
        }

        private void Fill(Painter2D painter, HashSet<string> regions, Color color)
        {
            foreach (var id in regions)
                if (shapes.TryGetValue(id, out var shape))
                    MapVectorDrawing.Fill(painter, shape.Rings, color);
        }
    }
}
