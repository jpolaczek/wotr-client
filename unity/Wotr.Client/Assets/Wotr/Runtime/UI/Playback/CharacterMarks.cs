using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-309 (OPPONENT_ACTIONS_UX §8 characters): the identities a presentation names, from recorded participants and the
    // presented board only — never a hidden version or a private roster.
    public static class CharacterMarks
    {
        public static bool Named(ActionParticipant figure) => figure != null && !figure.UnitKind.HasValue && figure.DefinitionId.Length > 0;

        // The larger moment: the character's portrait and title.
        public static CharacterMark Moment(ActionParticipant figure, bool removed = false) => Moment(figure.DefinitionId, figure.Side, removed);

        public static CharacterMark Moment(CharacterView figure) => Moment(figure.DefinitionId, figure.Side, false);

        // A moving group, named beside its destination: with an army its units come first (one entry and their count), so
        // characters marching with an army read differently from a group travelling alone; then the Nazgûl and their
        // count; then each character by portrait and short name. Empty when neither a character nor a Nazgûl moves: an
        // army's march keeps its traces only (UI-306).
        public static List<CharacterMark> Group(IReadOnlyCollection<ActionParticipant> movers)
        {
            var characters = movers.Where(Named).ToList();
            var nazgul = movers.Where(mover => mover.UnitKind == UnitKind.Nazgul).ToList();
            var marks = new List<CharacterMark>();
            if (characters.Count == 0 && nazgul.Count == 0)
                return marks;
            var army = movers.Where(mover => mover.UnitKind.HasValue && mover.UnitKind != UnitKind.Nazgul).ToList();
            if (army.Count > 0)
                marks.Add(Count(army[0], army.Count));
            if (nazgul.Count > 0)
                marks.Add(Count(nazgul[0], nazgul.Count));
            marks.AddRange(characters.Select(character => new CharacterMark
            {
                DefinitionId = character.DefinitionId, Art = FigureAppearances.Character(character.DefinitionId, character.Side),
                Side = character.Side, Text = CharacterLibrary.ShortName(character.DefinitionId)
            }));
            return marks;
        }

        private static CharacterMark Moment(string definition, Side side, bool removed) => new CharacterMark
        {
            DefinitionId = definition, Art = FigureAppearances.Character(definition, side), Side = side,
            Text = CharacterLibrary.Name(definition), Removed = removed
        };

        private static CharacterMark Count(ActionParticipant unit, int count) => new CharacterMark
        {
            Art = FigureAppearances.Unit(unit.NationId, unit.UnitKind.Value, unit.Side), Side = unit.Side, Text = "×" + count
        };
    }
}
