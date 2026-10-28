using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-377 (owner's request): the Council token's travel is chosen above the token, not in the decision window.
    // UI-379 (owner, 2026-10-28; T26, LORDS p. 30–31: the token is the Character die's Move Nazgûl and Minions — any or
    // all of them, each to its own destination, in one action): the player chooses a group — one or more lit figures,
    // above the token or on the map, a second click releasing one — then clicks its destination, where the group stands
    // faintly with its count, and chooses the next group. Finish above the token (or the console's ✓) sends every planned
    // move; planning every figure the token may move sends them at once. Only planned groups are in the command, one per
    // origin, garrison and destination as the server groups a flight; the chosen group is kept locally
    // (TurnDraft.ArmedSource, comma-separated figure ids) and never sent.
    public static class CouncilTravel
    {
        private const char Separator = ',';

        // The token's own travel in the player's action: its option once the token's printed use is chosen.
        public static ActionOption Option(PlayerView view, TurnDraft draft)
        {
            if (view?.Decision?.Kind != DecisionKind.Action || view.DecisionOwner != view.You ||
                draft?.Kind != CommandKind.UseCouncilToken || draft.Payload.AbilityId != ExpansionDraft.CouncilTravel)
                return null;
            var option = DraftOptions.For(view, draft);
            return option?.Enabled == true && option.Travel.Count > 0 ? option : null;
        }

        public static bool Active(PlayerView view, TurnDraft draft) => Option(view, draft) != null;

        // The figures the token may move, in the server's order.
        public static IReadOnlyList<string> Figures(PlayerView view, TurnDraft draft) =>
            Option(view, draft)?.Travel.Select(t => t.Id).ToList() ?? new List<string>();

        // The group being chosen, not yet given a destination.
        public static IReadOnlyList<string> Chosen(TurnDraft draft) => draft.ArmedSource.Length == 0
            ? new List<string>() : draft.ArmedSource.Split(Separator).ToList();

        public static bool IsChosen(TurnDraft draft, string id) => Chosen(draft).Contains(id);

        // A figure already given its destination.
        public static bool Planned(TurnDraft draft, string id) => draft.Payload.CompanionMoves.Any(g => g.Ids.Contains(id));

        public static int PlannedCount(TurnDraft draft) => draft.Payload.CompanionMoves.Sum(g => g.Ids.Count);

        // The figures not yet given a destination, where they stand.
        public static IEnumerable<TravelOption> Unmoved(PlayerView view, TurnDraft draft) =>
            Option(view, draft)?.Travel.Where(t => !Planned(draft, t.Id)) ?? Enumerable.Empty<TravelOption>();

        // A figure's click: an unmoved one joins the chosen group or leaves it; a planned one gives up its destination
        // and is chosen again. One click is one undo step.
        public static bool Choose(PlayerView view, TurnDraft draft, string id)
        {
            var option = Option(view, draft);
            if (option == null || option.Travel.All(t => t.Id != id))
                return false;
            draft.Gesture(() =>
            {
                var chosen = Chosen(draft).ToList();
                if (Planned(draft, id))
                {
                    draft.Change(payload => Unplan(payload, id));
                    chosen.Add(id);
                }
                else if (!chosen.Remove(id))
                    chosen.Add(id);
                draft.ArmedSource = string.Join(Separator.ToString(), chosen);
            });
            return true;
        }

        // Where the whole chosen group can go: a region every chosen figure reaches, other than where one stands.
        public static HashSet<string> Targets(PlayerView view, TurnDraft draft)
        {
            var travel = ChosenTravel(view, draft);
            var targets = new HashSet<string>();
            if (travel.Count == 0)
                return targets;
            targets.UnionWith(travel[0].Targets);
            foreach (var other in travel.Skip(1))
                targets.IntersectWith(other.Targets);
            targets.ExceptWith(travel.Select(t => t.Origin));
            return targets;
        }

        // The chosen group goes to a lit destination; a walker takes its shortest path there.
        public static bool Move(PlayerView view, TurnDraft draft, string region, BoardPresentation data)
        {
            var travel = ChosenTravel(view, draft);
            if (travel.Count == 0 || !Targets(view, draft).Contains(region))
                return false;
            var routes = travel.Select(t => (Travel: t, Route: TravelDraft.Path(t, data, region))).ToList();
            if (routes.Any(r => r.Route == null))
                return false;
            var flying = Option(view, draft).Travel.Where(t => t.Flight).Select(t => t.Id).ToHashSet();
            draft.Gesture(() =>
            {
                draft.Change(payload =>
                {
                    foreach (var (move, route) in routes)
                        NazgulFlightDraft.Add(payload, view, move, route, flying);
                });
                draft.ArmedSource = "";
            });
            return true;
        }

        // Every figure the token may move has its destination: the move is complete and goes at once.
        public static bool Complete(PlayerView view, TurnDraft draft) =>
            Option(view, draft) is { } option && option.Travel.All(t => Planned(draft, t.Id));

        // The console's hint while the token's travel is planned; empty for any other step.
        public static string Hint(PlayerView view, TurnDraft draft)
        {
            if (Option(view, draft) == null)
                return "";
            var chosen = Chosen(draft);
            if (chosen.Count > 0)
                return Describe(view, chosen) + " · Click a highlighted region to move there; click a chosen one to release it.";
            if (PlannedCount(draft) == 0)
                return "Council token · Click the Nazgûl to move (above the token or on the map), then their destination.";
            return "Council token · " + Describe(view, draft.Payload.CompanionMoves.SelectMany(g => g.Ids).ToList()) +
                " planned · Choose more and their destination, or Finish above the token.";
        }

        // What Finish above the token does.
        public static string ConfirmTooltip(PlayerView view, TurnDraft draft)
        {
            var planned = draft.Payload.CompanionMoves.SelectMany(g => g.Ids).ToList();
            return planned.Count == 0 ? "Finish · Choose a Nazgûl and its destination first"
                : "Finish · Use the Council token to move " + Describe(view, planned);
        }

        // "Nazgûl ×2, Witch-king": the figures by name, a repeated name counted.
        public static string Describe(PlayerView view, IEnumerable<string> ids) => string.Join(", ", ids
            .Select(id => CharacterLibrary.Name(view, id))
            .GroupBy(name => name)
            .Select(group => group.Count() > 1 ? group.Key + " ×" + group.Count() : group.Key));

        private static List<TravelOption> ChosenTravel(PlayerView view, TurnDraft draft)
        {
            var chosen = Chosen(draft);
            return Option(view, draft)?.Travel.Where(t => chosen.Contains(t.Id) && !Planned(draft, t.Id)).ToList() ??
                new List<TravelOption>();
        }

        private static void Unplan(CommandPayload payload, string id)
        {
            foreach (var group in payload.CompanionMoves)
                group.Ids.Remove(id);
            payload.CompanionMoves.RemoveAll(group => group.Ids.Count == 0);
        }
    }
}
