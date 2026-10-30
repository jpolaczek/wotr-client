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
    // UI-344 (D1): after the first board, a new marker emerges by fading in and a marker gone fades out where it stood.
    // UI-406 (TABLETOP_FIGURES_UX, "Kings"): the marker is a small puck lying on the table — the dark iron puck over its
    // contact shadow, the crown under the Eye on its top face — and its lit halo lies behind it (Styles/Armies.uss).
    public sealed class MapSovereignMarkers
    {
        public const string ClassName = "sovereign-marker";
        public const string LitClass = "legal-target";
        public const string LeavingClass = "sovereign-marker-leaving";
        // UI-406: the puck's parts, back to front.
        private static readonly string[] Parts = { "sovereign-glow", "sovereign-shadow", "sovereign-puck", "sovereign-emblem" };
        private const long LeaveMilliseconds = 450;
        private bool seeded;
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
                Leave(markers[id]);
                markers.Remove(id);
            }
            foreach (var king in data.CorruptedSovereigns)
            {
                if (!markers.TryGetValue(king.Id, out var marker))
                {
                    marker = Create(king.Id);
                    markers.Add(king.Id, marker);
                    if (seeded)
                        Appearance.Arrive(marker.Button);
                }
                marker.Region = king.RegionId;
                map.Stands.Stand(marker.Stand, map.BesideForces(king.RegionId));
                // UI-420 (owner): the marker reads as the king's portrait does.
                marker.Description = SovereignPresentation.Describe(king, data.Regions[king.RegionId].Name);
            }
            map.Stands.Sort();
            seeded = true;
            Light(hint);
        }

        private void Leave(Marker marker)
        {
            var button = marker.Button;
            button.pickingMode = PickingMode.Ignore;
            if (!seeded || !Appearance.Moving(button))
            {
                marker.Stand.Remove();
                return;
            }
            button.AddToClassList(LeavingClass);
            button.schedule.Execute(marker.Stand.Remove).ExecuteLater(LeaveMilliseconds);
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

        // UI-401: a king's marker stands on its place beside his capital's forces (MapCanvas.BesideForces) at its own size,
        // the puck's middle on the place.
        private void Place(Marker marker)
        {
            var visible = map.Stands.Place(marker.Stand, marker.Stand.Feet, 1);
            marker.Button.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible)
                map.ReserveMarker(marker.Stand.Bounds);
        }

        private Marker Create(string id)
        {
            var button = new Button(() => Selected?.Invoke(id)) { name = "sovereign-marker-" + id };
            button.AddToClassList(ClassName);
            foreach (var part in Parts)
                button.Add(Part(part));
            button.userData = new DraftToken(DraftObject.Sovereign, id);
            // A press on the marker belongs to it, not to the map's region click or pan.
            button.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) e.StopPropagation(); });
            var stand = new MapStand(button, Size);
            stand.Design(Size / 2, new Rect(Vector2.zero, Size));
            map.Stands.Add(stand);
            return new Marker { Button = button, Stand = stand };
        }

        // The element's name doubles as its class, which Styles/Armies.uss selects.
        private static VisualElement Part(string name)
        {
            var element = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            element.AddToClassList(name);
            return element;
        }

        private sealed class Marker
        {
            public Button Button;
            public MapStand Stand;
            public string Region = "";
            public string Description = "";
        }
    }
}
