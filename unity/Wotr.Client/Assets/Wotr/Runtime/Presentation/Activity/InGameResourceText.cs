using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class InGameResourceText
    {
        public static string Name(ActionCostKind kind, string id, Side? owner = null) => kind switch
        {
            ActionCostKind.Ring => id.Length == 0 ? "Ring" : ExpansionLabels.Ring(id),
            ActionCostKind.CouncilToken => id switch
            {
                ExpansionDraft.CouncilPolitics => "Council · Politics",
                ExpansionDraft.CouncilTravel => "Council · Travel",
                _ => "Council token"
            },
            ActionCostKind.RulerToken => (owner == Side.Shadow ? "Dismay" : "Ruler token") +
                (id.StartsWith("kings.token.") ? " · " + FigureGrid.Name(id.Split('.').Last()) : ""),
            _ => ActionCardText.Words(kind.ToString())
        };

        public static string Die(string source) => source.Length == 0 || source == "standard" ? "" :
            ExpansionLabels.DieSource(new DieView { Source = source }) + " · ";

        public static string Change(ActionResourceChange change)
        {
            var owner = change.From == "dismay" || change.To == "dismay" ? Side.Shadow : (Side?)null;
            return Name(change.Kind, change.Id, owner) + " · " + Place(change.From) + " → " + Place(change.To);
        }

        private static string Place(string value) => value switch
        {
            "free-peoples" => "Free Peoples",
            "shadow" => "Shadow",
            _ => FigureGrid.Name(ActionCardText.Words(value))
        };
    }
}
