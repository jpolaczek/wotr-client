using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class CardDrawDraft
    {
        public const string Palantir = "kings.palantir.muster-as-event";

        public static void MatchCost(TurnDraft draft, ActionOption option)
        {
            if (option.Kind != CommandKind.DrawCard)
                return;
            var cost = option.DrawCosts.FirstOrDefault(c => c.DieId == draft.Payload.DieId);
            draft.Payload.AbilityId = cost?.AbilityId ?? "";
        }

        public static bool Ready(TurnDraft draft, ActionOption option) =>
            option.EligibleDice.Contains(draft.Payload.DieId) && option.Targets.Contains(draft.Payload.Deck) &&
            (option.DrawCosts.Count == 0 || option.DrawCosts.Any(c => c.DieId == draft.Payload.DieId && c.AbilityId == draft.Payload.AbilityId));

        public static string Label(EventCostOption cost) => cost.AbilityId == Palantir ? "Event · Palantír" : cost.Face.ToString();
    }
}
