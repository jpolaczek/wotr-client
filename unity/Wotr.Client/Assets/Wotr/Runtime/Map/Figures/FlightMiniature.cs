using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-404: what a flight draws — the same miniature its destination marker shows on the table, built by the same class:
    // the flying units on an army's base (MapArmyArtwork, with their count), a character on its stand
    // (MapCharacterArtwork), or the Fellowship's figure. Its design size, the point under its feet and its design unit
    // place it on the table at the scale of the figures beside it. Its own contact shadow is hidden (Styles/Armies.uss):
    // the flight's shadow stays on the ground while the miniature rises.
    internal sealed class FlightMiniature
    {
        public const string ArmyShadowClass = "flight-army-shadow";
        public const string CharacterShadowClass = "flight-character-shadow";
        public const string FellowshipShadowClass = "flight-fellowship-shadow";

        public Button Element { get; }
        public Vector2 Size { get; }
        public Vector2 Feet { get; }
        public Rect Visible { get; private set; }
        public float DesignUnit { get; }
        public string ShadowClass { get; }
        // The shadow's opacity under the standing miniature: its own contact shadow's, none for the Fellowship's figure.
        public float StandingShadow { get; }

        private FlightMiniature(Vector2 size, Vector2 feet, float designUnit, string shadowClass, float standingShadow)
        {
            Element = new Button { focusable = false, pickingMode = PickingMode.Ignore };
            Element.name = "flight-miniature";
            Size = size;
            Feet = feet;
            DesignUnit = designUnit;
            ShadowClass = shadowClass;
            StandingShadow = standingShadow;
        }

        // The flying units of one force, on a base of the destination force's kind; an Initial king among them keeps his
        // crown (`destination`: the force they join, as the map shows it).
        public static FlightMiniature Army(IReadOnlyList<BoardFigureMove> group, bool chosen, ArmyMarker destination)
        {
            var to = group[0].To;
            var feet = MapArmyArtwork.Feet(to.Garrison, true);
            var miniature = new FlightMiniature(MapArmyArtwork.HolderSize, feet, MapArmyArtwork.DesignUnit, ArmyShadowClass, 1);
            var button = miniature.Element;
            button.AddToClassList("army");
            button.AddToClassList(to.Side == Side.FreePeoples ? "fp" : "sh");
            button.EnableInClassList("garrison", to.Garrison);
            button.EnableInClassList("selected-force", chosen);
            var artwork = new MapArmyArtwork(button);
            artwork.Layout(true);
            artwork.Apply(Flying(group, destination));
            miniature.Visible = MapArmyArtwork.Visible(to.Garrison, true);
            return miniature;
        }

        private static ArmyMarker Flying(IReadOnlyList<BoardFigureMove> group, ArmyMarker destination)
        {
            var to = group[0].To;
            var army = ArmyMarker.From(to.Region, to.Side, to.Garrison, Units(group));
            var flying = army.Units.Select(unit => unit.Id).ToHashSet();
            for (var i = 0; destination != null && i < destination.SovereignIds.Count; i++)
                if (flying.Contains(destination.SovereignIds[i]) && i < destination.CrownedTitles.Count)
                {
                    army.SovereignIds.Add(destination.SovereignIds[i]);
                    army.CrownedTitles.Add(destination.CrownedTitles[i]);
                }
            return army;
        }

        private static IEnumerable<ArmyUnit> Units(IEnumerable<BoardFigureMove> group) => group
            .Select(move => move.To)
            .Where(to => to.UnitKind.HasValue)
            .Select(to => new ArmyUnit(to.FigureId, to.Nation, to.UnitKind.Value));

        // One character on its own stand.
        public static FlightMiniature Character(BoardFigurePosition to, bool chosen)
        {
            var feet = MapCharacterArtwork.Feet(true);
            var miniature = new FlightMiniature(MapCharacterArtwork.HolderSize, feet, MapCharacterArtwork.DesignUnit,
                CharacterShadowClass, 1);
            var artwork = new MapCharacterArtwork(miniature.Element);
            artwork.Apply(Figure(to), null, 0);
            artwork.Layout(true);
            miniature.Element.EnableInClassList(MapCharacterArtwork.SelectedClass, chosen);
            miniature.Visible = artwork.Visible(true);
            return miniature;
        }

        private static CharacterFigure Figure(BoardFigurePosition to) => new CharacterFigure
        {
            Id = to.FigureId,
            RegionId = to.Region,
            Side = to.Side,
            Garrison = to.Garrison,
            Art = to.Appearance
        };

        // The Fellowship's figure, as MapFellowshipMarker draws it on its place.
        public static FlightMiniature Fellowship(string symbol)
        {
            var miniature = new FlightMiniature(MapFellowshipMarker.RegionSize, MapFellowshipMarker.RegionFeet,
                MapFellowshipMarker.DesignUnit, FellowshipShadowClass, 0);
            MapFellowshipMarker.Dress(miniature.Element, symbol);
            miniature.Visible = new Rect(Vector2.zero, MapFellowshipMarker.RegionSize);
            return miniature;
        }
    }
}
