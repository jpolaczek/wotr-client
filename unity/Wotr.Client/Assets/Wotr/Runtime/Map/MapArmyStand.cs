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
            Stand.Design(MapArmyArtwork.Feet(Army.Garrison, figures), MapArmyArtwork.Visible(Army.Garrison, figures));
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
