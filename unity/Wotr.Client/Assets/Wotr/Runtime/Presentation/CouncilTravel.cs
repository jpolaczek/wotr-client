using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-377 (owner's request): the Council token's travel is chosen above the token, not in the decision window: the
    // figures it may move, the group being planned and the console's hint. The destinations stay on the map (TravelDraft).
    public static class CouncilTravel
    {
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
            Option(view, draft)?.EligibleIds.ToList() ?? new List<string>();

        public static bool Chosen(TurnDraft draft, string id) => TravelDraft.Group(draft)?.Ids.Contains(id) == true;

        public static bool Planned(TurnDraft draft, string id) => TravelDraft.PlannedGroup(draft, id) >= 0 && !Chosen(draft, id);

        // The console's hint while the token's travel is planned; empty for any other step.
        public static string Hint(PlayerView view, TurnDraft draft)
        {
            var option = Option(view, draft);
            if (option == null)
                return "";
            // UI-105: in the player's action the destination's click moves the chosen Nazgûl at once.
            var group = TravelDraft.Group(draft);
            if (group != null && group.Ids.Count > 0)
                return string.Join(", ", group.Ids.Select(id => CharacterLibrary.Name(view, id)).Distinct()) +
                    " · Click a highlighted region to move there.";
            return "Council token · Click the Nazgûl above the token, then their destination on the map.";
        }

        // What the ✓ above the token does.
        public static string ConfirmTooltip(PlayerView view, TurnDraft draft)
        {
            var moved = draft.Payload.CompanionMoves.Where(m => m.Route.Count > 1).SelectMany(m => m.Ids).ToList();
            return moved.Count == 0 ? "Use the Council token: choose a Nazgûl and its destination first"
                : "Use the Council token: move " + string.Join(", ", moved.Select(id => CharacterLibrary.Name(view, id)));
        }
    }
}
