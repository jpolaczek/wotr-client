using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-571 (owner, 2026-11-22: "an optional question when ending a move with the confirm at the bottom: a window saying
    // we did not make every possible move and asking whether we really want to end"): what the console's ✓ would leave
    // unused, in the player's terms, or null when nothing that could still act is left.
    // - A die's action of several steps (an Army die's second march, Muster points, a Character die's figures): the
    //   server's ActionSequence counts what is left and its option is enabled only while a figure can still act.
    // - A card's flights of Nazgûl and other Shadow characters planned on the map: the figures not yet moved that still
    //   have somewhere to go.
    public static class UnusedMoves
    {
        // What Finish (FinishActionSequence) would leave of a die's action of several steps.
        public static string Sequence(PlayerView view)
        {
            var sequence = view?.ActionSequence;
            if (view?.Decision?.Kind != DecisionKind.ActionSequence || sequence == null || sequence.Owner != view.You ||
                sequence.Remaining <= 0)
                return null;
            var option = view.Options.FirstOrDefault(o => o.Kind == sequence.Kind && o.Enabled);
            if (option == null)
                return null;
            return sequence.Kind switch
            {
                CommandKind.MoveArmies => Armies(sequence),
                CommandKind.Recruit => Recruitment(sequence.Remaining),
                CommandKind.MoveCompanions or CommandKind.MoveShadowCharacters => Figures(option.Travel
                    .Count(t => !sequence.UsedIds.Contains(t.Id) && CanGo(t))),
                _ => null
            };
        }

        // What confirming a card's planned flights would leave unmoved.
        public static string Flights(PlayerView view, TurnDraft draft)
        {
            if (view == null || draft == null || !NazgulFlightDraft.Active(view, draft))
                return null;
            return Figures(NazgulFlightDraft.Unmoved(view, draft).Count(CanGo));
        }

        private static bool CanGo(TravelOption travel) => travel.Targets.Any(region => region != travel.Origin);

        private static string Armies(ActionSequenceView sequence)
        {
            var total = sequence.CompletedSteps + sequence.Remaining;
            return "You moved " + sequence.CompletedSteps + " of " + total + " armies this die allows. " +
                (sequence.Remaining == 1 ? "Another army can still move." : sequence.Remaining + " more armies can still move.");
        }

        private static string Recruitment(int points) =>
            (points == 1 ? "1 recruitment point is" : points + " recruitment points are") + " still unused.";

        private static string Figures(int count) => count switch
        {
            0 => null,
            1 => "1 figure that can still move has not moved.",
            _ => count + " figures that can still move have not moved."
        };
    }
}
