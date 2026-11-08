using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-401 (TABLETOP_FIGURES_UX, "Stand places, scale, depth and hit shape"): everything that stands on the map stands
    // on its region's place (UI-422, MapStandSlots) and is sized in board units. Its feet are Camera.ToScreen(place) and
    // its size the zoom clamped to a readable range (MapStandScale), applied by translate and scale only (MapStand). A
    // region shows figures when it is focused, or at the Detail zoom while its group's footprint fits its capacity;
    // otherwise tokens. One stand layer holds the armies, the characters (UI-402), the Fellowship, the planned ghosts and
    // the kings' markers, sorted by their feet on the board whenever what stands there changes, never per frame, so
    // nearer figures (lower on the map) are drawn in front. A figure never moves to make room: it keeps its place at
    // every zoom and while panning.
    public sealed class MapStandLayout
    {
        public VisualElement Layer { get; }
        public MapStandScale Scale { get; private set; }
        private readonly MapCanvas map;
        // Regions whose group fitted their capacity at the last layout (the hysteresis of the capacity rule).
        private readonly HashSet<string> roomy = new HashSet<string>();
        private float minimumZoom;
        private bool unsorted;

        public MapStandLayout(MapCanvas map)
        {
            this.map = map;
            Layer = new VisualElement { name = "map-stands", pickingMode = PickingMode.Ignore };
            Layer.AddToClassList("map-stands");
            Layer.StretchToParentSize();
            map.Add(Layer);
        }

        // A region's places for its current definition (an expansion may make it a stronghold).
        public RegionStands Stands(string region)
        {
            map.Data.Regions.TryGetValue(region, out var current);
            return MapStandSlots.Load(map.Data).For(map.Data.Shapes[region], current, map.Garrisoned(region));
        }

        // Once per layout: the scale every stand is drawn at for the camera's zoom.
        public void Measure()
        {
            if (minimumZoom <= 0)
                minimumZoom = MapStandSlots.WorstCaseZoom(map.Data.Geometry);
            Scale = new MapStandScale(map.Camera.Zoom, minimumZoom, MapArmyArtwork.DesignUnit, map.FigureSize);
        }

        // Figures when the region is focused (inspected, holding a selected force or just reached by a flight), or at the
        // Detail zoom while the footprint fits the region's capacity; otherwise tokens.
        public bool Figures(string region, bool focused)
        {
            if (focused)
                return true;
            if (!map.MarkerVisibility.Detail)
                return false;
            var fits = Scale.Fits(Stands(region).Capacity, roomy.Contains(region));
            if (fits)
                roomy.Add(region);
            else
                roomy.Remove(region);
            return fits;
        }

        public void Add(MapStand stand)
        {
            Layer.Add(stand.Root);
            unsorted = true;
        }

        // Sets the board point under a stand's feet; a new point sorts the layer again at the next sync.
        public void Stand(MapStand stand, Vector2 feet)
        {
            if (stand.Feet == feet)
                return;
            stand.Feet = feet;
            unsorted = true;
        }

        // Called when what stands on the map changed, never per frame: nearer stands are drawn in front.
        public void Sort()
        {
            if (!unsorted)
                return;
            unsorted = false;
            Layer.Sort(MapStand.CompareDepth);
        }

        // Stands an object on a board point at an element scale; false, with nothing placed, while it is out of reach of
        // the view (MapCanvas.NearView), so it slides in and out with the map.
        public bool Place(MapStand stand, Vector2 feet, float scale)
        {
            var point = map.Camera.ToScreen(feet);
            if (!map.NearView(point))
                return false;
            stand.Place(point, scale);
            return true;
        }

        // The element scale of an army's miniature (and its ghosts) at this zoom.
        public float FigureScale => Scale.Of(MapArmyArtwork.DesignUnit);

        // Where each of a region's forces stands: a garrison at its fortress's gate; the main field force (the one with
        // army units, otherwise the larger) on the field; the other side's field force (Nazgûl beside an army) on the
        // planned place, an army's place, since UI-402 gives the characters the character places. Returns the side of the
        // force on the field, whose characters stand nearest it.
        internal Side? Assign(string region, IEnumerable<MapArmyStand> forces)
        {
            var stands = Stands(region);
            var field = 0;
            Side? fieldSide = null;
            foreach (var force in forces.OrderByDescending(f => f.Army.Regular + f.Army.Elite > 0)
                         .ThenByDescending(f => f.Army.Total).ThenBy(f => f.Army.Side))
            {
                var gate = force.Army.Garrison ? stands.Garrison : null;
                if (gate == null && field == 0)
                    fieldSide = force.Army.Side;
                // UI-522: the gate and the field have their place's row reach; the planned place an army's round base,
                // which narrows the field's row like any stand beside it (FitRows).
                force.AtPlanned = gate == null && field > 0;
                force.PlaceReach = gate != null ? stands.GarrisonReach : force.AtPlanned ? MapArmyStand.RoundReach : stands.FieldReach;
                Stand(force.Stand, gate ?? (field++ == 0 ? stands.Field : stands.Planned));
            }
            return fieldSide;
        }

        // UI-522: each of a region's forces on a row stand as wide as its place's room and the stands beside it now allow:
        // its characters', the Fellowship's, a planned ghost's and a force on the planned place.
        // UI-526: past its place a row stops short of whatever stands anywhere on the map (`standingAnywhere`, `everyForce`),
        // not only in its region, and of the board's marks (`marks`).
        internal void FitRows(IReadOnlyList<MapArmyStand> forces, IEnumerable<(Vector2 Feet, StandFootprint Footprint)> standing,
            IReadOnlyList<(Vector2 Feet, StandFootprint Footprint)> standingAnywhere, IReadOnlyList<Rect> marks,
            IReadOnlyList<MapArmyStand> everyForce)
        {
            var beside = standing.ToList();
            beside.AddRange(forces.Where(force => force.AtPlanned).Select(force => (force.Stand.Feet, StandFootprint.Army)));
            var around = beside.Concat(standingAnywhere).ToList();
            foreach (var force in forces.Where(force => !force.AtPlanned))
            {
                var feet = force.Stand.Feet;
                var room = MapArmyRowRoom.Narrow(feet, force.PlaceReach, beside);
                var others = everyForce.Where(other => other != force).Select(other => other.Stand.Feet);
                force.Room(room, Vector2.Max(room, MapArmyRowRoom.Limit(feet, around, others, marks)));
            }
        }

        // The army pass: every force on its place at its region's level; the bounds of the shown ones are reserved, so
        // region names hide under them.
        internal void LayoutArmies(IEnumerable<MapArmyStand> forces, Func<string, bool> focused, List<Rect> used)
        {
            foreach (var force in forces)
                if (LayoutArmy(force, focused(force.Region)))
                    used.Add(force.Bounds);
        }

        // One force: hidden while its feet are out of reach of the view; true when it is shown.
        internal bool LayoutArmy(MapArmyStand force, bool focused)
        {
            var point = map.Camera.ToScreen(force.Stand.Feet);
            var army = force.Army;
            force.Visible = map.NearView(point);
            Show(force.Button, force.Visible, map.Figures?.Arriving(army.RegionId, army.Side, army.Garrison) == true);
            if (!force.Visible)
                return false;
            force.ShowLevel(Figures(force.Region, focused));
            force.Stand.Place(point, force.Figures == true ? FigureScale : Scale.Token);
            return true;
        }

        // UI-402: one character, at its region's level like the armies: hidden behind the front of a stacked pair or while
        // its feet are out of reach of the view, dimmed while a flight brings it; true when it is shown.
        internal bool LayoutCharacter(MapCharacterStand character, bool focused, bool arriving)
        {
            var point = map.Camera.ToScreen(character.Stand.Feet);
            character.Visible = !character.Stacked && map.NearView(point);
            Show(character.Button, character.Visible, arriving);
            if (!character.Visible)
                return false;
            character.ShowLevel(Figures(character.Region, focused));
            character.Stand.Place(point, character.Figures == true ? FigureScale : Scale.CharacterToken);
            return true;
        }

        // A figure a flight is bringing shows faintly until it lands; a hidden one gives up the focus.
        private void Show(Button button, bool visible, bool arriving)
        {
            button.style.opacity = arriving ? .25f : 1f;
            if (!visible && button.panel?.focusController.focusedElement == button)
                map.Focus();
            button.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
