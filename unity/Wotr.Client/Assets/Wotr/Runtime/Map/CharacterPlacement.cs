using System;
using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-402 (TABLETOP_FIGURES_UX, "Stand places"): which of its region's places each character stands on. A field's
    // characters take the character places (UI-422: six, the nearest beside the field first), a garrison's the garrison's
    // character places (three, beside the gate); a garrison character in a region without a garrison place stands with
    // the field's. The side of the region's field army comes first, so its characters stand nearest it. A character keeps
    // the place it stood on while that place is still its group's, so a newcomer never moves the others; newcomers take
    // the free places in order. More characters than places stand as a stacked pair on the group's last place: the first
    // of them in front, the rest behind it.
    public static class CharacterPlacement
    {
        public sealed class Placement
        {
            public StandKind Kind;
            public int Index;
            // The front figure first, then any stacked behind it.
            public List<CharacterFigure> Members = new List<CharacterFigure>();
            public CharacterFigure Front => Members[0];
        }

        public static List<Placement> Assign(IEnumerable<CharacterFigure> characters, Side? fieldSide, bool hasGarrison,
            Func<string, (StandKind Kind, int Index)?> previous)
        {
            var ordered = characters.OrderBy(c => c.Side == fieldSide ? 0 : 1).ThenBy(c => c.Side)
                .ThenBy(c => c.Id, StringComparer.Ordinal).ToList();
            var result = Group(ordered.Where(c => c.Garrison && hasGarrison).ToList(), StandKind.GarrisonCharacter,
                RegionStands.GarrisonCharacterPlaces, previous);
            result.AddRange(Group(ordered.Where(c => !c.Garrison || !hasGarrison).ToList(), StandKind.Character,
                RegionStands.CharacterPlaces, previous));
            return result;
        }

        private static List<Placement> Group(List<CharacterFigure> group, StandKind kind, int places,
            Func<string, (StandKind Kind, int Index)?> previous)
        {
            var crowded = group.Count > places;
            // A crowded group keeps its last place for the stacked pair.
            var single = crowded ? places - 1 : places;
            var slots = new CharacterFigure[places];
            var waiting = new List<CharacterFigure>();
            foreach (var character in group)
                if (previous(character.Id) is { } place && place.Kind == kind && place.Index < single && slots[place.Index] == null)
                    slots[place.Index] = character;
                else
                    waiting.Add(character);
            var next = 0;
            var stacked = new List<CharacterFigure>();
            foreach (var character in waiting)
            {
                while (next < single && slots[next] != null)
                    next++;
                if (next < single)
                    slots[next] = character;
                else
                    stacked.Add(character);
            }
            var result = new List<Placement>();
            for (var index = 0; index < single; index++)
                if (slots[index] != null)
                    result.Add(new Placement { Kind = kind, Index = index, Members = { slots[index] } });
            if (stacked.Count > 0)
                result.Add(new Placement { Kind = kind, Index = places - 1, Members = stacked });
            return result;
        }
    }
}
