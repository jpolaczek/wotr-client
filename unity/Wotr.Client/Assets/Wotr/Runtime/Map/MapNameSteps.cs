using System;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-426 (TABLETOP_FIGURES_UX, "As built (UI-426)"): how far a region's name may step from its point each way — down,
    // up, left or right — found once with the name points. Two bounds, in board units: the free distance from the point to
    // the region's border along that way (the name's edge stays inside on its middle line, as UI-406's vertical room did),
    // and how far its largest extent (MapRegionNames.BoardRect) goes before it meets a stand place or a fortress
    // (MapStandSlots.NameObstacle). A name its own settlement's gem reaches into takes the shortest step that clears the
    // stone (MapRegionNames), clear of the places where they leave room, so no name point moves and the frame does no
    // search.
    public sealed class MapNameSteps
    {
        // Board y grows downwards: Vector2.up is a step down the map.
        public static readonly Vector2[] Ways = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        private const float Step = 1;
        private const float Farthest = 48;
        private readonly float[] border;
        private readonly float[] clear;

        private MapNameSteps(float[] border, float[] clear)
        {
            this.border = border;
            this.clear = clear;
        }

        public static MapNameSteps For(RegionGeometry shape, Vector2 point, Rect name, Func<Rect, bool> obstacle)
        {
            var clear = new float[Ways.Length];
            for (var i = 0; i < Ways.Length; i++)
                clear[i] = Clear(name, Ways[i], obstacle);
            return new MapNameSteps(MapRegionNamePoints.Reach(shape, point, Ways), clear);
        }

        // How far, in points at a zoom, a name drawn with this half size may step one way (an index into Ways): its edge
        // inside the region, and, when `clearOfPlaces`, its largest extent clear of the stand places too.
        public float Allowed(int way, float zoom, Vector2 drawnHalf, bool clearOfPlaces)
        {
            var half = Ways[way].x != 0 ? drawnHalf.x : drawnHalf.y;
            var inside = border[way] * zoom - half;
            return clearOfPlaces ? Mathf.Min(inside, clear[way] * zoom) : inside;
        }

        // The longest run of whole steps one way on which the name's largest extent meets no obstacle.
        private static float Clear(Rect name, Vector2 way, Func<Rect, bool> obstacle)
        {
            var reach = 0f;
            for (var step = Step; step <= Farthest; step += Step)
            {
                if (obstacle(new Rect(name.position + way * step, name.size)))
                    break;
                reach = step;
            }
            return reach;
        }
    }
}
