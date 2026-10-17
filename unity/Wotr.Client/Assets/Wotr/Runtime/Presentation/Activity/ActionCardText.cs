using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class ActionCardText
    {
        private static readonly Dictionary<string, string> words = new Dictionary<string, string>();
        public static string Actor(Side? side) => side.HasValue ? BoardScreen.SideName(side.Value) : "System";
        public static string Heading(ActionLogSummary action) => action.Legacy ? "Earlier entry" :
            (action.Initiator.HasValue ? Actor(action.Initiator) + " · " : "") + Words(action.Command?.ToString() ?? action.Context.Kind.ToString());
        public static string Status(ActionLogSummary action) => action.Legacy || action.Status == ActionLogStatus.Unknown ? "Incomplete history"
            : action.Status == ActionLogStatus.InProgress ? "In progress" : "Completed";
        public static string Region(string id, BoardPresentation data) => data.Regions.TryGetValue(id, out var region) ? region.Name : FigureGrid.Name(id);
        public static string Face(DieFace? face) => face.HasValue ? Words(face.Value.ToString()) : "Unrecorded face";

        public static string Cost(ActionCost cost)
        {
            var prefix = cost.Owner.HasValue ? Actor(cost.Owner) + " · " : "";
            return prefix + (cost.Kind switch
            {
                ActionCostKind.Die => InGameResourceText.Die(cost.Source) + Face(cost.Face) + " die" +
                    (cost.Use.HasValue && cost.Use != cost.Face ? " → " + Face(cost.Use) : ""),
                ActionCostKind.Free => "Free",
                ActionCostKind.Card => cost.Id.Length > 0 ? CardLibrary.Title(cost.Id) : cost.Count + " card" + (cost.Count == 1 ? "" : "s"),
                ActionCostKind.Figure => "Figure · " + FigureGrid.Name(cost.Id),
                ActionCostKind.Ability => "Ability · " + Source(cost.AbilityId.Length > 0 ? cost.AbilityId : cost.Source),
                _ => InGameResourceText.Name(cost.Kind, cost.Id, cost.Owner) +
                    (cost.Source.Length > 0 ? " · " + Source(cost.Source) : "")
            });
        }

        public static IEnumerable<ActionCost> ExtraCosts(ActionLogSummary action, ActionLogStep step) =>
            step.Costs.Where(c => !(step.Actor == action.Initiator && step.Command == action.Command &&
                action.Costs.Any(root => SameCost(root, c))));

        private static bool SameCost(ActionCost a, ActionCost b) => a.Kind == b.Kind && a.Owner == b.Owner && a.Id == b.Id &&
            a.Face == b.Face && a.Use == b.Use && a.AbilityId == b.AbilityId && a.Source == b.Source && a.Count == b.Count;

        public static IEnumerable<string> Participants(IEnumerable<ActionParticipant> participants) =>
            participants.GroupBy(p => (p.Side, p.NationId, p.UnitKind, p.DefinitionId, p.Garrison))
                .Select(g => g.Key.UnitKind.HasValue
                    ? $"{g.Count()}× {FigureGrid.Symbol(g.Key.UnitKind.Value)} {g.Key.UnitKind} · {FigureGrid.Name(g.Key.NationId)}" + (g.Key.Garrison ? " · Garrison" : "")
                    : CharacterLibrary.Name(g.Key.DefinitionId) + (g.Key.Garrison ? " · Garrison" : ""));

        public static string Route(ActionMovement movement, BoardPresentation data) =>
            string.Join(" → ", (movement.Route.Count > 0 ? movement.Route : new List<string> { movement.From, movement.To })
                .Where(s => s.Length > 0).Select(s => Region(s, data)));

        public static string Compact(ActionLogView group, BoardPresentation data)
        {
            var step = group.Steps.LastOrDefault();
            if (step == null)
                return "Loading details…";
            var move = group.Steps.SelectMany(s => s.Movements).LastOrDefault();
            if (move != null)
                return string.Join(" · ", Participants(move.Participants)) + "\n" + Route(move, data);
            return step.Text;
        }

        // The heading already names the side: "Shadow discarded a die" reads "Discarded a die".
        public static string WithoutSide(string text, Side? actor)
        {
            var prefix = actor.HasValue ? BoardScreen.SideName(actor.Value) + " " : "";
            if (prefix.Length == 0 || !text.StartsWith(prefix, StringComparison.Ordinal) || text.Length == prefix.Length)
                return text;
            var rest = text.Substring(prefix.Length);
            return char.ToUpperInvariant(rest[0]) + rest.Substring(1);
        }

        public static string Source(string id)
        {
            if (id is ExpansionDraft.CouncilPolitics or ExpansionDraft.CouncilTravel)
                return ExpansionLabels.CouncilToken(id);
            const string sovereignPrefix = "kings.sovereign.";
            if (id.StartsWith(sovereignPrefix, StringComparison.Ordinal))
            {
                var identity = id.Substring(sovereignPrefix.Length);
                var card = CharacterLibrary.Find("kings.character." + identity + ".awakened");
                // The public figure's name is shared by its cards. This does not
                // infer its historical status or select a version from current state.
                if (card != null)
                    return card.Title.Split(new[] { " — " }, StringSplitOptions.None)[0];
            }
            return CharacterLibrary.Find(id)?.Title ?? CardLibrary.Title(id);
        }

        public static string Words(string value)
        {
            if (words.TryGetValue(value, out var found))
                return found;
            var result = new StringBuilder();
            for (var i = 0; i < value.Length; i++)
            {
                if (i > 0 && char.IsUpper(value[i]) && char.IsLower(value[i - 1]))
                    result.Append(' ');
                result.Append(value[i]);
            }
            return words[value] = result.ToString();
        }
    }
}
