using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-247: a character's reaction in a battle — Gandalf the White's White Rider, the Witch-king's Sorcerer draw — is
    // used on that character's portrait in the Battle window with one click; the console's Pass declines (UI-291). A
    // character with more than one reaction at once keeps the list.
    public static class BattleReactions
    {
        public static ActionOption Option(PlayerView view) =>
            view?.Battle != null && view.Decision?.Kind == DecisionKind.Reaction && view.DecisionOwner == view.You
                ? view.Options.FirstOrDefault(o => o.Kind == CommandKind.RespondEffect && o.Enabled) : null;

        // The one reaction this character offers now, or null.
        public static EffectChoiceView Of(PlayerView view, CharacterView character)
        {
            var option = Option(view);
            if (option == null)
                return null;
            var choices = option.EffectChoices.Where(c => option.EligibleIds.Contains(c.Id) &&
                (c.SourceId == character.DefinitionId || c.SourceId == character.Id)).ToList();
            return choices.Count == 1 ? choices[0] : null;
        }

        // Whether this reaction is used on a portrait, so the decision's lists do not repeat it.
        public static bool OnPortrait(PlayerView view, string id) => Option(view) != null &&
            new[] { view.Battle.Attacker, view.Battle.Defender }.Where(f => f != null)
                .SelectMany(f => f.Characters).Any(c => Of(view, c)?.Id == id);

        // Every offered reaction is on a portrait, so the decision needs no separate Respond step.
        public static bool AllOnPortraits(PlayerView view)
        {
            var option = Option(view);
            return option != null && option.EligibleIds.Count > 0 && option.EligibleIds.All(id => OnPortrait(view, id));
        }

        public static void Choose(TurnDraft draft, string id) => ReactionDraft.Choose(draft, id);

        public static string Offer(EffectChoiceView reaction) =>
            "Click to use " + reaction.Title + (reaction.Detail.Length > 0 ? " · " + reaction.Detail : "");
    }
}
