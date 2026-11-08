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
