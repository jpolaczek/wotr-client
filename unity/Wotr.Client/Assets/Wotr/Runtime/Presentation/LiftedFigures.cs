using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-403 (TABLETOP_FIGURES_UX, "The lifted figure"): which figures the player holds above the table, by figure id. The
    // hand holds the draft's unsent move — the army selection without a destination (ArmyDraft), the travelling group
    // being chosen (TravelActions, the Council token's chosen group) and the figure the next destination click flies
    // (NazgulFlightDraft) — and the figures the draft sends on their way (an army's march, a character's travel): those
    // stay lifted until the view takes them from their place, unless a planned ghost shows them at their destination.
    // UI-404: when the step leaves, the figures on their way are the ones the flights take over from the hand
    // (MapCanvas.ExpectFlights).
    public static class LiftedFigures
    {
        // The unsent move: figures chosen and not yet given a destination.
        public static HashSet<string> Selection(PlayerView view, TurnDraft draft)
        {
            var held = new HashSet<string>();
            if (view == null || draft == null)
                return held;
            if (ArmyDraft.Current(draft) is { } move && move.To.Length == 0)
                held.UnionWith(move.UnitIds.Concat(move.CharacterIds));
            if (TravelDraft.Group(draft) is { } group && group.Route.Count == 0)
                held.UnionWith(TravelActions.Selected(draft));
            if (CouncilTravel.Active(view, draft))
                held.UnionWith(CouncilTravel.Chosen(draft));
            if (NazgulFlightDraft.Active(view, draft) && NazgulFlightDraft.Next(view, draft) is { } next)
                held.Add(next.Id);
            return held;
        }

        // The figures the draft sends to a destination while they still stand where they leave from: a march (an attack
        // keeps its army where it stands) and every group whose route leaves its origin (a route starts where the group
        // stands; a stay never leaves it).
        public static HashSet<string> OnTheirWay(PlayerView view, TurnDraft draft)
        {
            var moving = new HashSet<string>();
            if (view == null || draft == null)
                return moving;
            if (draft.Kind != CommandKind.AttackArmy)
                foreach (var move in draft.Payload.Moves.Where(move => move.To.Length > 0))
                    moving.UnionWith(move.UnitIds.Concat(move.CharacterIds).Where(id => Location(view, id) == move.From));
            foreach (var group in draft.Payload.CompanionMoves.Where(Leaves))
                moving.UnionWith(group.Ids.Where(id => Location(view, id) == group.Route[0]));
            return moving;
        }

        private static bool Leaves(CompanionMoveChoice group) => group.Route.Count > 1 && group.Route.Last() != group.Route[0];

        private static string Location(PlayerView view, string id) => view.Units.FirstOrDefault(unit => unit.Id == id)?.Location ??
            view.Characters.FirstOrDefault(character => character.Id == id)?.Location ?? "";
    }
}
