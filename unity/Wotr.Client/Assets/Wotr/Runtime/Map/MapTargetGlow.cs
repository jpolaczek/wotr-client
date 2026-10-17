using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-224: the regions the current action can enter glow green along their edges, the enemy regions it can attack
    // glow red. The glow is one retained layer under the bands and barriers; it repaints only when the targets change.
    // Its pulse only toggles a class whose opacity transition runs under .motion-enabled, so Reduced motion keeps it
    // still and panning or zooming never repaints it.
    public sealed class MapTargetGlow
    {
        public const string DimClass = "map-target-glow-dim";
        public const int PulseMilliseconds = 750;
        public static readonly Color Enter = new Color(.30f, .86f, .48f, 1f);
        public static readonly Color Attack = new Color(.95f, .26f, .20f, 1f);
        internal MapVectorLayer Layer { get; }
        private readonly Dictionary<string, RegionGeometry> shapes = new Dictionary<string, RegionGeometry>();
        private readonly HashSet<string> enter = new HashSet<string>();
        private readonly HashSet<string> attack = new HashSet<string>();
        private readonly IVisualElementScheduledItem pulse;

        internal MapTargetGlow(MapGeometry geometry, Vector2 size)
        {
            foreach (var shape in geometry.Regions)
                shapes[shape.Id] = shape;
            Layer = new MapVectorLayer("map-target-glow", size.x, size.y, context => Draw(context.painter2D));
            Layer.AddToClassList("map-target-glow");
            pulse = Layer.schedule.Execute(() => Layer.ToggleInClassList(DimClass)).Every(PulseMilliseconds);
            pulse.Pause();
        }

        public bool Glowing => enter.Count > 0 || attack.Count > 0;

        // The regions to mark this frame; the layer repaints only when either set changes.
        internal void Update(IReadOnlyList<string> enterable, IReadOnlyList<string> attackable)
        {
            if (!Changed(enter, enterable) & !Changed(attack, attackable))
                return;
            Layer.MarkDirtyRepaint();
            if (Glowing)
                pulse.Resume();
            else
            {
                pulse.Pause();
                Layer.RemoveFromClassList(DimClass);
            }
        }

        // Compares without allocating: it runs on every redraw, also while the camera moves.
        public static bool Changed(HashSet<string> current, IReadOnlyList<string> next)
        {
            var same = current.Count == next.Count;
            for (var index = 0; same && index < next.Count; index++)
                same = current.Contains(next[index]);
            if (same)
                return false;
            current.Clear();
            for (var index = 0; index < next.Count; index++)
                current.Add(next[index]);
            return true;
        }

        private void Draw(Painter2D painter)
        {
            foreach (var id in enter)
                Glow(painter, id, Enter);
            foreach (var id in attack)
                Glow(painter, id, Attack);
        }

        // A soft wide halo and a bright thin line on the region's edge.
        private void Glow(Painter2D painter, string id, Color color)
        {
            if (!shapes.TryGetValue(id, out var shape))
                return;
            MapVectorDrawing.Outline(painter, shape.Rings, new Color(color.r, color.g, color.b, .32f), 8f);
            MapVectorDrawing.Outline(painter, shape.Rings, new Color(color.r, color.g, color.b, .9f), 2.4f);
        }
    }
}
