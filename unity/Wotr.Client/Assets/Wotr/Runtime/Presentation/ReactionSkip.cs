using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-291 (owner's decision): the console's Pass also declines the player's own reaction window — a Guide's or a
    // character's skill, a table card's or a hand card's response, a free Event — so no separate Decline or Skip button
    // remains. Its tooltip names what passing skips.
    public static class ReactionSkip
    {
        private const string FreeEventWindow = "ents.follow-up";

        public static ActionOption Option(PlayerView view)
        {
            if (view?.Decision?.Kind != DecisionKind.Reaction || view.DecisionOwner != view.You || view.Winner.HasValue)
                return null;
            return view.Options.FirstOrDefault(o => o.Kind == CommandKind.DeclineReaction && o.Enabled);
        }

        public static string Tooltip(PlayerView view)
        {
            var skipped = Skipped(view);
            if (skipped.Count == 0)
                return "Pass · Skip this response";
            if (skipped.Count == 1)
                return "Pass · Skip " + skipped[0];
            return "Pass · Skip these responses:\n" + string.Join("\n", skipped);
        }

        public static IReadOnlyList<string> Skipped(PlayerView view)
        {
            var respond = view.Options.FirstOrDefault(o => o.Kind == CommandKind.RespondEffect && o.Enabled);
            if (respond != null)
                return respond.EffectChoices.Where(c => respond.EligibleIds.Contains(c.Id)).Select(Name).Distinct().ToList();
            if (view.Decision.Window == FreeEventWindow)
                return new[] { "the free Character Event of " + CardLibrary.Title(view.Decision.SourceId) };
            return new string[0];
        }

        // The skill's owner (a character, or the card that holds the response) and what the skill does.
        private static string Name(EffectChoiceView choice)
        {
            var source = choice.SourceId.Length > 0 ? choice.SourceId : choice.Id;
            var owner = source.Contains(".character.") && !CardLibrary.Cards.ContainsKey(source)
                ? CharacterLibrary.Name(source) : CardLibrary.Title(source);
            return choice.Title.Length > 0 ? owner + " · " + choice.Title : owner;
        }
    }
}
