using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wotr.Client
{
    // UI-305 (OPPONENT_ACTIONS_UX §6.5): the part of the map a presentation may frame — the map minus the top bar, the
    // notices, the console, the side columns and every open panel or card. Each occluder that overlaps what is left cuts
    // it to the largest band beside it, the largest occluders first.
    public static class SafeMapArea
    {
        public static Rect Largest(Rect area, IEnumerable<Rect> occluders)
        {
            foreach (var occluder in occluders.OrderByDescending(rect => rect.width * rect.height))
                if (occluder.Overlaps(area))
                    area = Bands(area, occluder).OrderByDescending(band => band.width * band.height).First();
            return area;
        }

        private static IEnumerable<Rect> Bands(Rect area, Rect cut)
        {
            yield return Rect.MinMaxRect(area.xMin, area.yMin, Mathf.Clamp(cut.xMin, area.xMin, area.xMax), area.yMax);
            yield return Rect.MinMaxRect(Mathf.Clamp(cut.xMax, area.xMin, area.xMax), area.yMin, area.xMax, area.yMax);
            yield return Rect.MinMaxRect(area.xMin, area.yMin, area.xMax, Mathf.Clamp(cut.yMin, area.yMin, area.yMax));
            yield return Rect.MinMaxRect(area.xMin, Mathf.Clamp(cut.yMax, area.yMin, area.yMax), area.xMax, area.yMax);
        }
    }
}
