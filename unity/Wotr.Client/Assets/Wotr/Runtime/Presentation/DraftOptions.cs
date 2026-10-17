using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Selects only options already projected by the server. It never grants an action.
    public static class DraftOptions
    {
        public static IEnumerable<ActionOption> All(PlayerView view, TurnDraft draft) =>
            draft.Payload.RulerTokenId.Length == 0 ? view.Options : view.RulerTokenActions
                .Where(o => o.TokenId == draft.Payload.RulerTokenId).Select(o => o.Action);

        public static ActionOption For(PlayerView view, TurnDraft draft, CommandKind? kind = null)
        {
            var choices = All(view, draft).Where(o => o.Kind == (kind ?? draft.Kind)).ToList();
            if (choices.Count == 0)
                return null;
            if ((kind ?? draft.Kind) == CommandKind.UseCouncilToken)
            {
                var selected = draft.Payload.AbilityId.Length == 0 ? choices[0]
                    : choices.FirstOrDefault(o => o.EligibleIds.Contains(draft.Payload.AbilityId));
                return selected == null ? null : Council(selected);
            }
            var option = choices.Count == 1 ? choices[0] : Sovereigns(choices);
            if (option.RulerTravel != null && draft.Payload.AbilityId == option.RulerTravel.AbilityId)
                return Travel(option, option.RulerTravel.Dice, option.RulerTravel.Travel);
            if (option.Kind == CommandKind.AwakenSovereign)
                return Travel(option, option.EligibleDice, option.Travel.Where(t => t.Id == draft.Payload.CharacterId).ToList());
            return option;
        }

        private static ActionOption Sovereigns(List<ActionOption> choices) => new ActionOption
        {
            Kind = choices[0].Kind,
            Enabled = choices.Any(o => o.Enabled),
            EligibleIds = choices.SelectMany(o => o.EligibleIds).Distinct().ToList(),
            EligibleDice = choices.SelectMany(o => o.EligibleDice).Distinct().ToList(),
            Targets = choices.SelectMany(o => o.Targets).Distinct().ToList(),
            CharacterActions = choices.SelectMany(o => o.CharacterActions).ToList(),
            Travel = choices.SelectMany(o => o.Travel).ToList(),
            Minimum = choices[0].Minimum,
            Maximum = choices[0].Maximum
        };

        private static ActionOption Council(ActionOption source) => new ActionOption
        {
            Kind = source.Kind,
            Enabled = source.Enabled,
            EligibleIds = source.Travel.Count == 0 ? source.EligibleIds : source.Travel.Select(t => t.Id).ToList(),
            Targets = source.Targets,
            Travel = source.Travel,
            Reason = source.Reason
        };

        private static ActionOption Travel(ActionOption source, List<string> dice, List<TravelOption> travel) => new ActionOption
        {
            Kind = source.Kind,
            Enabled = source.Enabled,
            EligibleDice = dice,
            EligibleIds = source.EligibleIds,
            Targets = source.Targets,
            Travel = travel,
            RulerTravel = source.RulerTravel,
            CharacterActions = source.CharacterActions,
            Minimum = source.Minimum,
            Maximum = source.Maximum,
            MaximumGroups = source.MaximumGroups,
            AllowEmptySelection = source.AllowEmptySelection
        };

        public static bool CanSelectToken(PlayerView view, string id) => view.RulerTokenActions.Any(o => o.TokenId == id) ||
            view.Options.Any(o => o.Enabled && o.Kind == CommandKind.UseDismay && o.EligibleIds.Contains(id));

        public static void SelectToken(TurnDraft draft, PlayerView view, string id)
        {
            if (id.Length > 0 && !CanSelectToken(view, id))
                return;
            if (view.You == Side.Shadow && id.Length > 0)
            {
                if (draft.Kind != CommandKind.UseDismay)
                    DraftGesture.Choose(draft, view, CommandKind.UseDismay);
                draft.Change(p =>
                {
                    p.Ids.Clear();
                    p.Ids.Add(id);
                });
                return;
            }
            draft.Clear();
            draft.Change(p => p.RulerTokenId = id);
        }
    }
}
