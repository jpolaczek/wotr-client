using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class MapEffectsOverlay
    {
        private readonly MapCanvas map;
        private readonly Dictionary<string, Label> markers = new Dictionary<string, Label>();

        public MapEffectsOverlay(MapCanvas map)
        {
            this.map = map;
            map.CameraChanged += Layout;
            map.RegisterCallback<GeometryChangedEvent>(_ => Layout());
        }

        public void Apply(PlayerView view)
        {
            var grouped = view.Effects.SelectMany(effect => EffectsPresentation.LocalRegions(view, effect)
                .Where(map.Data.Regions.ContainsKey).Select(region => (region, effect)))
                .GroupBy(item => item.region).ToDictionary(g => g.Key,
                    g => g.Select(item => item.effect).GroupBy(e => EffectsPresentation.Key(e.SourceId, e.Owner)).Select(e => e.First()).ToList());
            foreach (var id in markers.Keys.Where(k => !grouped.ContainsKey(k)).ToList())
            {
                markers[id].RemoveFromHierarchy();
                markers.Remove(id);
            }
            foreach (var group in grouped)
            {
                if (!markers.TryGetValue(group.Key, out var marker))
                {
                    marker = new Label { name = "map-effects-" + group.Key, pickingMode = PickingMode.Ignore };
                    marker.AddToClassList("map-effects-marker");
                    map.Add(marker);
                    markers.Add(group.Key, marker);
                }
                marker.text = "◇" + (group.Value.Count > 1 ? " " + group.Value.Count : "");
                marker.tooltip = string.Join("\n", group.Value.Select(e => e.Title.Length > 0 ? e.Title : CardLibrary.Title(e.SourceId)));
            }
            Layout();
        }

        private void Layout()
        {
            foreach (var entry in markers)
            {
                var shape = map.Data.Shapes[entry.Key];
                var anchor = map.Camera.ToScreen(new Vector2(shape.Anchor[0], shape.Anchor[1]));
                var visible = map.NearView(anchor);
                entry.Value.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (!visible)
                    continue;
                entry.Value.style.left = anchor.x - 10;
                entry.Value.style.top = anchor.y + 15;
            }
        }
    }
}
