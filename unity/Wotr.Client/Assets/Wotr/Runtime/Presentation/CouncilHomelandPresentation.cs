using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class CouncilHomelandPresentation
    {
        // UI-482 (owner, 2026-11-04): with no companion to place the client answers the step itself (CouncilHomelandSkip), so
        // the console only says what happens; "✓ confirms" returns once a send was refused and the ✓ is the player's.
        public const string AllStay = "All companions stay with the Fellowship.";
        public const string AllStayByHand = "All companions stay with the Fellowship · ✓ confirms.";

        // UI-482 (owner, 2026-11-04): what the Shadow's tokens do, in the hint's tooltip now that the panel shows no token.
        public const string PoliticsToken = "Council token · Politics: advance a Shadow nation politically, without a die.";
        public const string TravelToken = "Council token · Travel: move Nazgûl and Minions, without a die.";

        // UI-347 (EXP-15 D14): the console's hint names each click; the second click on a placed companion, which keeps
        // that companion with the Fellowship, is named whenever it is available (owner's addition). UI-482 (owner,
        // 2026-11-04): every instruction of this step stands here, the Shadow's Council tokens included; the panel keeps
        // only its portraits.
        public static string Hint(PlayerView view, TurnDraft draft)
        {
            if (!Owns(view) || draft.Kind != CommandKind.ChooseCouncilHomelands)
                return "";
            var option = CompanionSetupPresentation.HomelandOption(view);
            if (option == null || option.CharacterActions.Count == 0)
                return AllStay;
            return TooltipText.Parts(Step(draft, option), TokenEffect(draft.Payload.Placements.Count), "✓ confirms") + ".";
        }

        // UI-482: the hint's tooltip keeps the full hint (its line may end in an ellipsis), what the ✓ gives the Shadow and
        // what each token does.
        public static string Detail(PlayerView view, TurnDraft draft)
        {
            var hint = Hint(view, draft);
            if (hint.Length == 0)
                return "";
            var lead = hint == AllStay ? "" : hint;
            return TooltipText.Lines(lead, TokenDetail(draft.Payload.Placements.Count), PoliticsToken, TravelToken);
        }

        // UI-451: a click on a lit homeland starts its companion there; a shared one waits for the companion's portrait.
        private static string Step(TurnDraft draft, ActionOption option)
        {
            var waiting = CouncilDraft.Waiting(draft);
            if (waiting.Length > 0)
                return "Click " + string.Join(" or ", CouncilDraft.Candidates(draft, option, waiting).Select(CharacterLibrary.ShortName)) +
                    " in the Fellowship panel to start in this homeland";
            var id = draft.Payload.CharacterId;
            if (option.CharacterActions.All(c => c.CharacterId != id))
                return "Click a lit homeland to start its companion there, or drag a portrait there";
            var name = CharacterLibrary.ShortName(id);
            return Placed(draft, id)
                ? "Click " + name + " again to keep this companion with the Fellowship, or a lit homeland"
                : "Click a lit homeland on the map for " + name + ", or another glowing companion";
        }

        public static bool Placed(TurnDraft draft, string id) => draft.Payload.Placements.Any(p => p.UnitId == id);

        public static bool Owns(PlayerView view) => view != null && !view.Winner.HasValue &&
            view.You == Side.FreePeoples && view.DecisionOwner == view.You &&
            view.Decision?.Kind == DecisionKind.CouncilHomelands;

        // UI-482: the step the server keeps open with no companion to place, so that its existence reveals no version.
        public static bool NothingToPlace(PlayerView view) =>
            CompanionSetupPresentation.HomelandOption(view) is { } option && option.CharacterActions.Count == 0;

        public static string TokenEffect(int departures) => departures == 0 ? ""
            : departures == 1 ? "Shadow chooses one Council token" : "Shadow receives both Council tokens";

        public static string TokenDetail(int departures) => departures == 0
            ? "All companions stay with the Fellowship. The Shadow receives no Council tokens."
            : departures == 1
                ? "After you confirm, the Shadow chooses the Politics or the Travel Council token."
                : "After you confirm, the Shadow receives both Council tokens.";
    }
}
