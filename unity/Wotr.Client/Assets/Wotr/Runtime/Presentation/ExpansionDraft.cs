using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class ExpansionDraft
    {
        public const string CouncilPolitics = "council.advance-politics";
        public const string CouncilTravel = "council.move-shadow-characters";

        public static bool Handles(CommandKind? kind) => CouncilDraft.Handles(kind) || kind is
            CommandKind.ChooseSpecialDie or CommandKind.DiscardRulerDie or CommandKind.UseDismay or
            CommandKind.UseNarya or CommandKind.UseVilya or CommandKind.AwakenSovereign or
            CommandKind.RemoveCorruptedSovereign or CommandKind.AttemptCorruption or CommandKind.UseCouncilToken;

        public static bool Ready(TurnDraft draft, ActionOption option)
        {
            var p = draft.Payload;
            if (CouncilDraft.Handles(draft.Kind))
                return CouncilDraft.Ready(draft, option);
            if (draft.Kind == CommandKind.UseCouncilToken)
                return p.AbilityId == CouncilPolitics ? option.Targets.Contains(p.NationId)
                    : p.AbilityId == CouncilTravel && FellowshipDraft.Ready(draft, option);
            if (!option.EligibleDice.Contains(p.DieId))
                return false;
            return draft.Kind switch
            {
                CommandKind.UseDismay => p.Ids.Count == 1 && option.EligibleIds.Contains(p.Ids[0]),
                CommandKind.UseNarya => option.EligibleIds.Contains(p.RingId) && option.Targets.Contains(p.NationId),
                CommandKind.UseVilya => option.EligibleIds.Contains(p.RingId),
                CommandKind.AttemptCorruption or CommandKind.RemoveCorruptedSovereign => option.EligibleIds.Contains(p.CharacterId),
                CommandKind.AwakenSovereign => option.CharacterActions.Any(c => c.CharacterId == p.CharacterId && c.Dice.Contains(p.DieId)) &&
                    TravelDraft.EndAllowed(p.Route, option.Travel.FirstOrDefault()),
                _ => true
            };
        }

        public static void Initialize(TurnDraft draft, PlayerView view, ActionOption option, string token)
        {
            var p = draft.Payload;
            p.RulerTokenId = token;
            if (token.Length > 0)
            {
                p.AbilityId = view.RulerTokenActions.First(o => o.TokenId == token && o.Action.Kind == draft.Kind).AbilityId;
                if (option.EligibleDice.Count == 1)
                    p.DieId = option.EligibleDice[0];
            }
            if (draft.Kind is CommandKind.UseNarya or CommandKind.UseVilya)
                p.RingId = option.EligibleIds.Single();
            if (draft.Kind == CommandKind.UseNarya && option.Targets.Count == 1)
                p.NationId = option.Targets[0];
        }

        public static void CouncilToken(TurnDraft draft, string id) => draft.Change(p =>
        {
            p.AbilityId = id;
            p.NationId = "";
            p.CompanionMoves.Clear();
            draft.CompanionFocus = -1;
        });

        public static void TravelMode(TurnDraft draft, ActionOption option, bool ruler) => draft.Change(p =>
        {
            p.AbilityId = ruler ? option.RulerTravel.AbilityId : "";
            p.DieId = "";
            p.Route.Clear();
            p.CompanionMoves.Clear();
            draft.CompanionFocus = -1;
        });

        public static void Reconcile(TurnDraft draft, ActionOption option)
        {
            if (CouncilDraft.Handles(draft.Kind))
            {
                CouncilDraft.Reconcile(draft, option);
                return;
            }
            var p = draft.Payload;
            if (!option.EligibleIds.Contains(p.CharacterId))
                p.CharacterId = "";
            if (!option.Targets.Contains(p.NationId))
                p.NationId = "";
            if (draft.Kind == CommandKind.UseCouncilToken && option.Travel.Count > 0)
                FellowshipDraft.Reconcile(draft, option);
            if (draft.Kind == CommandKind.AwakenSovereign && !TravelDraft.PrefixAllowed(p.Route, option.Travel.FirstOrDefault()))
                p.Route.Clear();
        }
    }
}
