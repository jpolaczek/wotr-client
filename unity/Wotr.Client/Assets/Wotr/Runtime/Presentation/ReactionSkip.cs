using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-291 (owner's decision): the console's Pass also declines the player's own reaction window — a Guide's or a
    // character's skill, a table card's or a hand card's response, a free Event — so no separate Decline button remains.
    // Its tooltip names what passing skips. UI-423: the Hunt and battle windows add their own Skip effect, which sends the
    // same Pass.
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

        // UI-423 (owner's playtest pattern, like UI-419): the Hunt and battle windows' Skip effect declines the window as
        // the console's Pass does and names each response it skips, one a line ("Skip Draw matching card (Gandalf the
        // Grey)").
        public static string SkipTooltip(PlayerView view)
        {
            var skipped = Skipped(view);
            if (skipped.Count == 0)
                return "Skip this response";
            return string.Join("\n", skipped.Select(name => "Skip " + name));
        }

        // UI-423: a Hunt window response's tooltip ends with how to skip it: the window's Skip effect or the console's Pass.
        public static string SkipLine(PlayerView view) =>
            Option(view) != null ? "Or click Skip effect (or Pass) to skip it." : "";

        // The console's hint for a reaction window whose responses glow at their sources.
        public static string Guidance(PlayerView view)
        {
            var skipped = Skipped(view);
            return "Use a glowing response, or Pass to skip " +
                (skipped.Count == 0 ? "this response window" : string.Join(" or ", skipped)) + ".";
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

        // What the skill does, then its owner (UI-420: "Draw matching card (Gandalf the Grey)"), so the tooltip keeps one
        // separator.
        private static string Name(EffectChoiceView choice) =>
            choice.Title.Length > 0 ? choice.Title + " (" + Owner(choice) + ")" : Owner(choice);

        // A response's owner: a character, or the card that holds the response.
        public static string Owner(EffectChoiceView choice)
        {
            var source = choice.SourceId.Length > 0 ? choice.SourceId : choice.Id;
            return source.Contains(".character.") && !CardLibrary.Cards.ContainsKey(source)
                ? CharacterLibrary.Name(source) : CardLibrary.Title(source);
        }
    }
}
