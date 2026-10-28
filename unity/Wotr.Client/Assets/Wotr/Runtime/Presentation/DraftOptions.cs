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

        internal static ActionOption Council(ActionOption source) => new ActionOption
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

        // UI-276 (D20): a Dismay token is picked by the Ruler die's click, so only the Free Peoples' Ruler tokens select.
        public static bool CanSelectToken(PlayerView view, string id) => view.RulerTokenActions.Any(o => o.TokenId == id);

        // The own dice a Ruler token's actions can be paid with (its Ruler die).
        public static IReadOnlyList<string> TokenDice(PlayerView view, string id) => id.Length == 0 ? new List<string>()
            : view.RulerTokenActions.Where(o => o.TokenId == id && o.Action.Enabled).SelectMany(o => o.Action.EligibleDice)
                .Where(die => view.Dice.Any(d => d.Id == die && d.Owner == view.You && d.Location == DieLocation.Available))
                .Distinct().ToList();

        public static void SelectToken(TurnDraft draft, PlayerView view, string id)
        {
            if (id.Length > 0 && !CanSelectToken(view, id))
                return;
            draft.Clear();
            draft.Change(p => p.RulerTokenId = id);
        }
    }
}
