using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class HuntReactionPresentation
    {
        public static string Source(PlayerView view, EffectChoiceView choice)
        {
            if (CardLibrary.Cards.ContainsKey(choice.SourceId))
                return CardLibrary.Title(choice.SourceId);
            var version = CharacterVersion(view, choice.SourceId);
            return version.Length > 0 ? CharacterLibrary.Name(version) : "Response";
        }

        public static string Cost(EffectChoiceView choice) => choice.Cost switch
        {
            "DiscardTableCard" => "Discard this card",
            "TransferNenya" => "Give Nenya to Shadow",
            "None" or "" => CardLibrary.Cards.ContainsKey(choice.SourceId) ? "Keep this card · No die" : "Use ability · No die",
            _ => FigureGrid.Name(choice.Cost)
        };

        // UI-420 (owner): "Gollum · Use ability · No die\n<what it does>\n<Guide ability>" — the source named once, its
        // Level and Leadership only when it has them, and no ability ids.
        public static string Inspect(PlayerView view, EffectChoiceView choice)
        {
            var version = CharacterVersion(view, choice.SourceId);
            return TooltipText.Lines(
                TooltipText.Parts(Source(view, choice), Stats(version), Cost(choice)),
                choice.Detail,
                version.Length > 0 ? FellowshipGuideText.Ability(version) : "");
        }

        // "Level 1 · Leadership 1" of a projected version; nothing for a figure without them, such as Gollum.
        private static string Stats(string version)
        {
            var character = version.Length > 0 ? CharacterLibrary.Find(version) : null;
            if (character == null)
                return "";
            return TooltipText.Parts(character.Level > 0 ? "Level " + character.Level : "",
                character.Leadership > 0 ? "Leadership " + character.Leadership : "");
        }

        private static string CharacterVersion(PlayerView view, string source)
        {
            var character = view.Characters.Concat(view.OwnCompanions)
                .FirstOrDefault(c => c.Id == source || c.DefinitionId == source);
            if (character != null)
                return character.DefinitionId.Length > 0 ? character.DefinitionId : character.Id;
            var revealed = view.Fellowship.RevealedCompanionVersions.FirstOrDefault(p => p.Key == source || p.Value == source).Value;
            if (!string.IsNullOrEmpty(revealed))
                return revealed;
            return view.Fellowship.Guide == source ? FellowshipGuideText.PublicVersion(view.Fellowship, source) : "";
        }
    }
}
