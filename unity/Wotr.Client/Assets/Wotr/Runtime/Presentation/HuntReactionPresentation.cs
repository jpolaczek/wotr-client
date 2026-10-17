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

        public static string Inspect(PlayerView view, EffectChoiceView choice)
        {
            var text = Source(view, choice) + "\n" + choice.Detail + "\n" + Cost(choice);
            var version = CharacterVersion(view, choice.SourceId);
            if (version.Length == 0)
                return text;
            var ability = FellowshipGuideText.Ability(version);
            return text + "\n" + CharacterLibrary.Description(version) + (ability.Length > 0 ? "\n" + ability : "");
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
