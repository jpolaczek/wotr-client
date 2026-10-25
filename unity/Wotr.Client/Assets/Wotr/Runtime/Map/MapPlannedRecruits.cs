using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-131: figures planned by a card or an ability, drawn faintly where they will arrive. Each region's ghost
    // has × that returns its planned figures to the reserve; the ghost itself is never interactive. UI-212: the same
    // ghosts show a card's planned Nazgûl flights, without ×.
    public sealed class MapPlannedRecruits
    {
        public event Action<string> Removed;
        private readonly MapCanvas map;
        private readonly Dictionary<string, Ghost> ghosts = new Dictionary<string, Ghost>();
        private readonly List<string> stale = new List<string>();

        private readonly string prefix;
        private readonly bool removable;

        public MapPlannedRecruits(MapCanvas map, string prefix = "planned-recruit", bool removable = true)
        {
            this.map = map;
            this.prefix = prefix;
            this.removable = removable;
        }

        public IEnumerable<string> Regions => ghosts.Keys;

        public void Apply(IReadOnlyList<PlannedReinforcement> placements, Side side, bool interactive) =>
            Apply(placements.GroupBy(p => p.RegionId).ToDictionary(g => g.Key,
                g => ArmyMarker.From(g.Key, side, false, g.Select(p => (p.Nation, p.Kind)))), interactive);

        // UI-294: ghosts of other figures, such as the companions a card's journey moves: one marker per region.
        public void Apply(IReadOnlyDictionary<string, ArmyMarker> markers, bool interactive)
        {
            var regions = markers.Where(p => map.Data.Shapes.ContainsKey(p.Key)).ToDictionary(p => p.Key, p => p.Value);
            stale.Clear();
            stale.AddRange(ghosts.Keys.Where(region => !regions.ContainsKey(region)));
            foreach (var region in stale)
            {
                ghosts[region].Remove();
                ghosts.Remove(region);
            }
            foreach (var pair in regions)
            {
                if (!ghosts.TryGetValue(pair.Key, out var ghost))
                {
                    ghost = new Ghost(map, pair.Key, prefix, removable ? () => Removed?.Invoke(pair.Key) : null);
                    ghosts.Add(pair.Key, ghost);
                }
                ghost.Apply(pair.Value, interactive);
            }
            Layout();
        }

        public void Layout()
        {
            foreach (var pair in ghosts)
                pair.Value.Layout(map);
        }

        private sealed class Ghost
        {
            private readonly Button marker;
            private readonly MapArmyArtwork artwork;
            private readonly Button remove;
            private readonly string region;

            public Ghost(MapCanvas map, string region, string prefix, Action removed)
            {
                this.region = region;
                marker = new Button { name = prefix + "-" + region, focusable = false, pickingMode = PickingMode.Ignore };
                marker.AddToClassList("army");
                marker.AddToClassList("army-ghost");
                marker.AddToClassList("planned-recruit");
                artwork = new MapArmyArtwork(marker);
                map.Add(marker);
                if (removed == null)
                    return;
                remove = IconButton.Create("close", "Return the figures planned here to the reserve", removed);
                remove.name = prefix + "-remove-" + region;
                remove.AddToClassList("planned-recruit-remove");
                // A press on the remove icon belongs to the button, not to the map's region click or pan.
                remove.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) e.StopPropagation(); });
                map.Add(remove);
            }

            public void Apply(ArmyMarker army, bool interactive)
            {
                marker.EnableInClassList("fp", army.Side == Side.FreePeoples);
                marker.EnableInClassList("sh", army.Side == Side.Shadow);
                artwork.Apply(army);
                artwork.Layout(true);
                remove?.SetEnabled(interactive);
            }

            public void Layout(MapCanvas map)
            {
                var shape = map.Data.Shapes[region];
                var anchor = map.Camera.ToScreen(new Vector2(shape.Anchor[0], shape.Anchor[1]));
                var visible = map.NearView(anchor);
                marker.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (remove != null)
                    remove.style.display = marker.style.display;
                if (!visible)
                    return;
                var rect = map.GhostRect(region);
                map.ReserveMarker(rect);
                marker.style.left = rect.x;
                marker.style.top = rect.y;
                marker.BringToFront();
                if (remove == null)
                    return;
                remove.style.left = rect.xMax - 10;
                remove.style.top = rect.y - 6;
                remove.BringToFront();
            }

            public void Remove()
            {
                marker.RemoveFromHierarchy();
                remove?.RemoveFromHierarchy();
            }
        }
    }
}
