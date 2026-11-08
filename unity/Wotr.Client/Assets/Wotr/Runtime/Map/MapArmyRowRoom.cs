using System.Collections.Generic;
using UnityEngine;

namespace Wotr.Client
{
    // UI-522: how far a force's row stand may reach on either side of its feet where it stands now, in board units: its
    // place's reach (the export's, clear of the region's border, the gems, drawings and names, the other force, the king's
    // marker and the labels around it), the side a character, the Fellowship or the planned recruits stand on now
    // narrowed so their bases keep clear of the row's (UI-422's gap) and the row's figures cover none of theirs.
    public static class MapArmyRowRoom
    {
        // scripts/export_map_stands.py: GAP between bases; UI-523: a row's figures as tall as an army's 40 units.
        private const float Gap = 1.5f;
        private const float RowHeight = 40;
        private const float PlaqueTop = 2.2f;
        private const float PlaqueBottom = 14.8f;

        // (left, right): how far the row reaches before and after its feet.
        public static Vector2 Narrow(Vector2 feet, Vector2 reach, IEnumerable<(Vector2 Feet, StandFootprint Footprint)> standing)
        {
            var left = reach.x;
            var right = reach.y;
            foreach (var (other, footprint) in standing)
            {
                var side = Reach(feet, other, footprint);
                if (other.x >= feet.x)
                    right = Mathf.Min(right, side);
                if (other.x <= feet.x)
                    left = Mathf.Min(left, side);
            }
            var round = StandFootprint.Army.BaseWidth / 2;
            return new Vector2(Mathf.Clamp(left, round, Mathf.Max(round, reach.x)), Mathf.Clamp(right, round, Mathf.Max(round, reach.y)));
        }

        // UI-526: a mark drawn round a board point at a constant size in points, as a board rect at `zoom` points a unit.
        public static Rect Mark(Vector2 centre, Vector2 points, float zoom)
        {
            var size = points / zoom;
            return new Rect(centre - size / 2, size);
        }

        // UI-526: how far a row may reach past its place where nothing stands in its way — the widest a row goes, narrowed
        // for the stands around it and for the other forces: their bases keep clear of it, and it covers neither their
        // figures nor the plaque of a force it stands in front of (UI-425), nor a mark on the board (`marks`).
        public static Vector2 Limit(Vector2 feet, IEnumerable<(Vector2 Feet, StandFootprint Footprint)> standing, IEnumerable<Vector2> forces,
            IEnumerable<Rect> marks = null)
        {
            var limit = Narrow(feet, Vector2.one * StandFootprint.RowReach, standing);
            foreach (var other in forces)
            {
                var side = Reach(feet, other, StandFootprint.Army);
                if (InFront(feet, other))
                    side = Mathf.Min(side, PlaqueReach(feet, other));
                if (other.x >= feet.x)
                    limit.y = Mathf.Min(limit.y, side);
                if (other.x <= feet.x)
                    limit.x = Mathf.Min(limit.x, side);
            }
            foreach (var mark in marks ?? new Rect[0])
            {
                if (!AcrossRow(feet, mark.yMin, mark.yMax))
                    continue;
                if (mark.xMax > feet.x)
                    limit.y = Mathf.Min(limit.y, mark.xMin - feet.x - Gap);
                if (mark.xMin < feet.x)
                    limit.x = Mathf.Min(limit.x, feet.x - mark.xMax - Gap);
            }
            var round = StandFootprint.Army.BaseWidth / 2;
            return Vector2.Max(limit, Vector2.one * round);
        }

        // Whether something between `top` and `bottom` on the board lies across a row's band: its figures up to its front.
        private static bool AcrossRow(Vector2 feet, float top, float bottom) =>
            top < feet.y + StandFootprint.Army.BaseDepth / 2 && bottom > feet.y - RowHeight;

        // How far the row may reach towards a force behind it before it covers that force's plaque (the round base's width,
        // 2.2 to 14.8 units below its feet; scripts/export_map_stands.py, PLAQUE).
        private static float PlaqueReach(Vector2 feet, Vector2 other)
        {
            var army = StandFootprint.Army;
            var rowTop = feet.y - RowHeight;
            var rowFront = feet.y + army.BaseDepth / 2;
            var plaqueTop = other.y + PlaqueTop;
            var plaqueBottom = other.y + PlaqueBottom;
            return plaqueTop < rowFront && plaqueBottom > rowTop ? Mathf.Abs(other.x - feet.x) - army.BaseWidth / 2 : float.PositiveInfinity;
        }

        // MapStand.CompareDepth: the lower feet, then the one further right, stand in front.
        private static bool InFront(Vector2 a, Vector2 b) => a.y > b.y || Mathf.Approximately(a.y, b.y) && a.x > b.x;

        // How far the row may reach towards one stand.
        private static float Reach(Vector2 feet, Vector2 other, StandFootprint footprint)
        {
            var army = StandFootprint.Army;
            var across = Mathf.Abs(other.x - feet.x);
            var reach = float.PositiveInfinity;
            // Bases of one aspect are circles once their depth is stretched over it; a row's is a stadium, a straight spine
            // between the round ends of an army's base.
            var radius = army.BaseWidth / 2;
            var clear = radius + footprint.BaseWidth / 2 + Gap;
            var depth = (other.y - feet.y) / StandFootprint.Depth;
            if (Mathf.Abs(depth) < clear)
                reach = across - Mathf.Sqrt(clear * clear - depth * depth) + radius;
            var rowTop = feet.y - RowHeight;
            var rowFront = feet.y + army.BaseDepth / 2;
            var top = other.y - footprint.FigureHeight;
            var front = other.y + footprint.BaseDepth / 2;
            if (top < rowFront && front > rowTop)
                reach = Mathf.Min(reach, across - Mathf.Max(footprint.BaseWidth, footprint.FigureWidth) / 2);
            return reach;
        }
    }
}
