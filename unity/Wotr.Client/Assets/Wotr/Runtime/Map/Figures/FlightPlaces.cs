using System.Collections.Generic;
using UnityEngine;

namespace Wotr.Client
{
    // UI-404 (TABLETOP_FIGURES_UX, "Flight profile"): a flight runs between stand points on the board. It leaves from the
    // place its figures stood on before the board changed — the previous place assignment, recorded on every sync before
    // the new one is made — and lands on the place they stand on now. The Fellowship always stands on its region's
    // Fellowship place, and a Companion separating from it leaves from there.
    internal sealed class FlightPlaces
    {
        private readonly MapCanvas map;
        private readonly Dictionary<string, Vector2> previous = new Dictionary<string, Vector2>();

        public FlightPlaces(MapCanvas map)
        {
            this.map = map;
        }

        // On every sync, before the new places are assigned: where each unit and character stands now.
        public void Record(IEnumerable<MapArmyStand> forces, IEnumerable<MapCharacterStand> characters)
        {
            previous.Clear();
            foreach (var force in forces)
                foreach (var unit in force.Army.Units)
                    previous[BoardFigurePosition.UnitPrefix + unit.Id] = force.Stand.Feet;
            foreach (var character in characters)
                previous[BoardFigurePosition.CharacterPrefix + character.Figure.Id] = character.Stand.Feet;
        }

        public Vector2 Source(BoardFigurePosition from)
        {
            if (from.IsFellowship || from.InFellowship)
                return map.Stands.Stands(from.Region).Fellowship;
            return previous.TryGetValue(from.Key, out var feet) ? feet : Destination(from);
        }

        public Vector2 Destination(BoardFigurePosition to)
        {
            if (to.IsFellowship)
                return map.Stands.Stands(to.Region).Fellowship;
            if (to.IsCharacter)
                return map.Characters.Find(to.FigureId)?.Stand.Feet ?? map.Characters.NextPlace(to.Region);
            return map.ForceFeet(to.Region, to.Side, to.Garrison) ?? ArmyPlace(to);
        }

        // A force the map does not show yet stands where an arriving army would: its gate, or its field.
        private Vector2 ArmyPlace(BoardFigurePosition to)
        {
            var gate = to.Garrison ? map.Stands.Stands(to.Region).Garrison : null;
            return gate ?? map.LandingPlace(to.Region);
        }
    }
}
