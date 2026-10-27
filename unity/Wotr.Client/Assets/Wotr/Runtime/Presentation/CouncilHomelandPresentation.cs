using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class CouncilHomelandPresentation
    {
        // UI-347 (EXP-15 D14): the console's hint names each click; the second click on a placed companion, which keeps
        // that companion with the Fellowship, is named whenever it is available (owner's addition).
        public static string Hint(PlayerView view, TurnDraft draft)
        {
            if (!Owns(view) || draft.Kind != CommandKind.ChooseCouncilHomelands)
                return "";
            var option = CompanionSetupPresentation.HomelandOption(view);
            if (option == null || option.CharacterActions.Count == 0)
                return "All companions stay with the Fellowship · ✓ confirms.";
            var id = draft.Payload.CharacterId;
            if (option.CharacterActions.All(c => c.CharacterId != id))
                return "Click a glowing companion in the Fellowship panel, then a lit homeland on the map; dragging the portrait works too · ✓ confirms.";
            var name = CharacterLibrary.ShortName(id);
            return Placed(draft, id)
                ? "Click " + name + " again to keep this companion with the Fellowship, or another lit homeland · ✓ confirms."
                : "Click a lit homeland on the map for " + name + ", or another glowing companion · ✓ confirms.";
        }

        public static bool Placed(TurnDraft draft, string id) => draft.Payload.Placements.Any(p => p.UnitId == id);

        public static bool Owns(PlayerView view) => view != null && !view.Winner.HasValue &&
            view.You == Side.FreePeoples && view.DecisionOwner == view.You &&
            view.Decision?.Kind == DecisionKind.CouncilHomelands;

        public static string TokenEffect(int departures) => departures == 0 ? "No tokens"
            : departures == 1 ? "Shadow chooses one" : "Shadow receives both";

        public static string TokenDetail(int departures) => departures == 0
            ? "All companions stay with the Fellowship. The Shadow receives no Council tokens."
            : departures == 1
                ? "After you confirm, the Shadow chooses the Politics or the Travel Council token."
                : "After you confirm, the Shadow receives both Council tokens.";
    }
}
