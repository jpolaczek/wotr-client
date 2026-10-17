using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class ActionNoticeText
    {
        public static string Short(ActionLogStep step, BoardPresentation data) =>
            ActionCardText.Actor(step.Actor) + " · " + Sentence(step, data);

        // UI-106: what was done, without the side: the notice's own enamel already names the opponent.
        public static string Sentence(ActionLogStep step, BoardPresentation data)
        {
            if (step.Movements.LastOrDefault() is { } move)
            {
                var units = move.Participants.Where(p => p.UnitKind.HasValue).GroupBy(p => p.UnitKind.Value)
                    .Select(g => g.Count() + " " + g.Key);
                var figures = string.Join(" + ", units.Concat(move.Participants.Where(p => !p.UnitKind.HasValue)
                    .Select(p => CharacterLibrary.Name(p.DefinitionId))));
                return ActionCardText.Words(move.Kind.ToString()) + " " + figures + " · " + ActionCardText.Route(move, data);
            }
            var description = step.Command.HasValue ? ActionCommandText.Describe(step.Command.Value) : step.Text;
            var subject = step.Context.NationId.Length > 0 ? FigureGrid.Name(step.Context.NationId)
                : step.Context.DefinitionId.Length > 0 ? Definition(step, data)
                : step.Context.Regions.Count > 0 ? ActionCardText.Region(step.Context.Regions.Last(), data) : "";
            return description + (subject.Length > 0 ? " · " + subject : "");
        }

        private static string Definition(ActionLogStep step, BoardPresentation data)
        {
            var id = step.Context.DefinitionId;
            if (data.HuntTiles.TryGetValue(id, out var tile))
                return "Hunt tile · " + FigureGrid.Name(tile.Value);
            if (CardLibrary.Cards.TryGetValue(id, out var card))
                return card.Title;
            if (id.Contains(".character.") || id.Contains(".sovereign."))
            {
                var name = CharacterLibrary.Name(id);
                return name == FigureGrid.Name(id) ? "Character" : name;
            }
            var die = step.Facts.Dice.FirstOrDefault(item => item.Id == id);
            if (die != null)
                return (die.After ?? die.Before) is { } face ? ActionCardText.Face(face) + " die" : "Action die";
            if (step.Context.HuntChainId.Length > 0)
                return "Hunt";
            return step.Context.Kind switch
            {
                ActionContextKind.Dice => "Action die",
                ActionContextKind.Hunt => "Hunt",
                ActionContextKind.Fellowship => "Fellowship",
                ActionContextKind.Cards => "Card effect",
                ActionContextKind.Characters => "Character",
                ActionContextKind.Resources => "Resource",
                _ => ""
            };
        }
    }
}
