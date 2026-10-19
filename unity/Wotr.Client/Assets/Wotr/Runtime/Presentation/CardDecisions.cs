using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-211 (CARD_CHOICES_UX): the decisions a card's effect asks of its player outside a battle and the Hunt. Their
    // choices are on objects or in the turn console, and the console's ✓ confirms them.
    public static class CardDecisions
    {
        public static bool Owns(PlayerView view, TurnDraft draft) =>
            view?.Decision != null && view.DecisionOwner == view.You && draft.Kind.HasValue &&
            view.Battle == null && view.Hunt == null && view.EventLoss == null && Card(view.Decision.Kind);

        private static bool Card(DecisionKind kind) => kind is DecisionKind.EffectChoice or DecisionKind.CharacterEvent or
            DecisionKind.ArmyEvent or DecisionKind.RemoveExcess;

        // The card a decision comes from, for the console's headline; empty when the decision names no card.
        public static string CardTitle(PlayerView view)
        {
            var source = view?.Decision?.SourceId ?? "";
            return source.Length > 0 && CardLibrary.Cards.TryGetValue(source, out var card) ? card.Title : "";
        }

        // What has been chosen so far, for the console's hint; empty before the first choice.
        public static string Progress(PlayerView view, TurnDraft draft)
        {
            var payload = draft.Payload;
            switch (draft.Kind)
            {
                case CommandKind.MoveShadowCharacters:
                case CommandKind.MoveCompanions:
                    return Count(payload.CompanionMoves.Sum(m => m.Ids.Count), Moved(view, draft));
                case CommandKind.MoveArmies:
                    return Count(payload.Moves.Count(m => m.To.Length > 0), "armies moved") + Armies(view, draft);
                case CommandKind.ChooseEffect when payload.Placements.Count > 0:
                    return "Recruited " + payload.Placements.Count + Limit(view, draft);
                case CommandKind.ChooseEffect:
                case CommandKind.RemoveExcess:
                    return payload.Ids.Count == 0 ? "" : "Chosen " + payload.Ids.Count + Limit(view, draft);
                default:
                    return "";
            }
        }

        // UI-216: an event moving several armies shows how many it allows.
        private static string Armies(PlayerView view, TurnDraft draft)
        {
            var option = DraftOptions.For(view, draft);
            return option == null || option.Maximum <= 1 || !draft.Payload.Moves.Any(m => m.To.Length > 0) ? "" : " of " + option.Maximum;
        }

        private static string Moved(PlayerView view, TurnDraft draft) =>
            view.Decision?.Kind == DecisionKind.CharacterEvent && draft.Kind == CommandKind.MoveShadowCharacters
                ? NazgulFlightDraft.OnlyNazgul(view, draft) ? "Nazgûl moved" : "figures moved" : "characters moved";

        private static string Count(int count, string what) => count == 0 ? "" : what.Substring(0, 1).ToUpperInvariant() + what.Substring(1) + " " + count;

        private static string Limit(PlayerView view, TurnDraft draft)
        {
            var option = DraftOptions.For(view, draft);
            return option == null || option.Maximum <= 0 ? "" : " of " + option.Maximum;
        }
    }
}
