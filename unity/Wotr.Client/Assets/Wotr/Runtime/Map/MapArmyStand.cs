using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-126, UI-401: one force on the map — its button and miniature, its projection and the place it stands on.
    internal sealed class MapArmyStand
    {
        // UI-403: every unit of the force is in the hand: its base disappears, its hit shape stays (Styles/Armies.uss).
        public const string LiftedClass = "lifted-away";
        public Button Button;
        public MapArmyArtwork Artwork;
        public MapStand Stand;
        public ArmyMarker Army;
        public bool Selected;
        public bool Visible;
        // The level it was last laid out at: figures on a base, or a token.
        public bool? Figures;
        // UI-522: how far its place's row stand reaches on either side (board units) and whether it stands on the planned
        // place, set where it is stood.
        public static readonly Vector2 RoundReach = Vector2.one * StandFootprint.Army.BaseWidth / 2;
        public Vector2 PlaceReach = RoundReach;
        public bool AtPlanned;
        // UI-403: its units held in the hand (or shown by a planned ghost elsewhere); its base shows the others.
        private readonly HashSet<string> lifted = new HashSet<string>();

        public string Region => Army.RegionId;
        public Rect Bounds => Stand.Bounds;

        // Switches between figures and the token, with the point under its feet that each level stands on.
        public void ShowLevel(bool figures)
        {
            if (Figures == figures)
                return;
            Figures = figures;
            Artwork.Layout(figures);
            Design();
        }

        // The point under its feet and its visible part at its level; UI-522: a row's wider stand widens the visible part.
        private void Design()
        {
            var figures = Figures == true;
            var visible = MapArmyArtwork.Visible(Army.Garrison, figures, figures ? Artwork.SpreadLeft : 0, figures ? Artwork.SpreadRight : 0);
            Stand.Design(MapArmyArtwork.Feet(Army.Garrison, figures), visible);
        }

        // UI-522: how far its row stand may reach on either side now, in board units; a new reach lays its row out again.
        public void Room(Vector2 reach)
        {
            var points = reach * MapArmyArtwork.DesignUnit;
            if (Vector2.Distance(Artwork.RowReach, points) < .01f)
                return;
            Artwork.RowReach = points;
            ShowArmy();
        }

        // UI-403 (SetLiftMask): the units that left the table; the base shows only what stays behind, and disappears when
        // none stays. Nothing changes while the same units are lifted.
        public void Mask(ICollection<string> ids)
        {
            var now = Army.Units.Where(unit => ids.Contains(unit.Id)).Select(unit => unit.Id).ToList();
            if (lifted.SetEquals(now))
                return;
            lifted.Clear();
            lifted.UnionWith(now);
            ShowArmy();
        }

        // Its projection, without the lifted units: a base of the units that stay, or the whole force with its artwork
        // hidden once every unit is lifted (its stand still takes a click).
        public void ShowArmy()
        {
            var staying = Army.Units.Where(unit => !lifted.Contains(unit.Id)).ToList();
            var empty = lifted.Count > 0 && staying.Count == 0;
            Button.EnableInClassList(LiftedClass, empty);
            Artwork.Apply(lifted.Count == 0 || empty ? Army : Staying(staying));
            if (Figures.HasValue)
                Design();
        }

        private ArmyMarker Staying(IEnumerable<ArmyUnit> units)
        {
            var army = ArmyMarker.From(Army.RegionId, Army.Side, Army.Garrison, units);
            army.Effect = Army.Effect;
            army.SovereignIds = Army.SovereignIds;
            army.CrownedTitles = Army.CrownedTitles;
            return army;
        }
    }
}
