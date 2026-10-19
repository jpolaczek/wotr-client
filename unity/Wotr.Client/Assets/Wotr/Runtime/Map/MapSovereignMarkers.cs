using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-274: a corrupted king is no figure, so a dark marker stands by his capital, beside its forces. A click chooses
    // him when a decision or the selected die lights him (the tooltip says what it does), and otherwise shows his
    // information.
    public sealed class MapSovereignMarkers
    {
        public const string ClassName = "sovereign-marker";
        public const string LitClass = "legal-target";
        private static readonly Vector2 Size = new Vector2(30, 34);
        public event Action<string> Selected;
        private readonly MapCanvas map;
        private readonly Dictionary<string, Marker> markers = new Dictionary<string, Marker>();
        private readonly List<string> stale = new List<string>();
        private Func<string, string> hint = _ => null;

        public MapSovereignMarkers(MapCanvas map) => this.map = map;

        public Button Find(string id) => markers.TryGetValue(id, out var marker) ? marker.Button : null;

        public void Apply(BoardPresentation data)
        {
            stale.Clear();
            stale.AddRange(markers.Keys.Where(id => data.CorruptedSovereigns.All(s => s.Id != id)));
            foreach (var id in stale)
            {
                markers[id].Button.RemoveFromHierarchy();
                markers.Remove(id);
            }
            foreach (var king in data.CorruptedSovereigns)
            {
                if (!markers.TryGetValue(king.Id, out var marker))
                {
                    marker = Create(king.Id);
                    markers.Add(king.Id, marker);
                }
                marker.Region = king.RegionId;
                marker.Description = king.Title + " · Corrupted · " + data.Regions[king.RegionId].Name;
            }
            Light(hint);
        }

        // What a click on each king does now, or null when it only informs.
        public void Light(Func<string, string> clickHint)
        {
            hint = clickHint;
            foreach (var pair in markers)
            {
                var text = hint(pair.Key);
                pair.Value.Button.EnableInClassList(LitClass, text != null);
                ChoiceGlow.Apply(pair.Value.Button, text != null);
                pair.Value.Button.tooltip = text != null ? text + "\n" + pair.Value.Description : pair.Value.Description;
            }
        }

        public void Layout()
        {
            foreach (var marker in markers.Values)
                Place(marker);
        }

        private void Place(Marker marker)
        {
            var shape = map.Data.Shapes[marker.Region];
            var anchor = map.Camera.ToScreen(new Vector2(shape.Anchor[0], shape.Anchor[1]));
            var visible = map.NearView(anchor);
            marker.Button.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible)
                return;
            var rect = map.BesideForces(marker.Region, Size);
            map.ReserveMarker(rect);
            marker.Button.style.left = rect.x;
            marker.Button.style.top = rect.y;
        }

        private Marker Create(string id)
        {
            var button = new Button(() => Selected?.Invoke(id)) { name = "sovereign-marker-" + id };
            button.AddToClassList(ClassName);
            button.userData = new DraftToken(DraftObject.Sovereign, id);
            // A press on the marker belongs to it, not to the map's region click or pan.
            button.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) e.StopPropagation(); });
            map.Add(button);
            return new Marker { Button = button };
        }

        private sealed class Marker
        {
            public Button Button;
            public string Region = "";
            public string Description = "";
        }
    }
}
