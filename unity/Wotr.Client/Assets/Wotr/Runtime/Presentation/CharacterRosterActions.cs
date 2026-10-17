using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Resolves presentation ownership only; legal costs and targets come from options.
    public static class CharacterRosterActions
    {
        public static string Figure(PlayerView view, string id) => string.IsNullOrEmpty(id) ? "" : view.OwnCharacterRoster
            .Where(row => row.Side == view.You && (row.FigureId == id || row.CurrentDefinitionId == id ||
                row.FutureVersions.Any(version => version.DefinitionId == id)))
            .Select(row => row.FigureId).FirstOrDefault() ?? "";

        public static string DecisionFigure(PlayerView view, TurnDraft draft) =>
            view.DecisionOwner == view.You && CharacterDraft.UsesCharacters(draft.Kind)
                ? Figure(view, draft.Payload.CharacterId) : "";

        public static IEnumerable<ComponentAction> For(PlayerView view, string figure, string version,
            IEnumerable<ComponentAction> components)
        {
            var row = view.OwnCharacterRoster.FirstOrDefault(item => item.Side == view.You && item.FigureId == figure);
            if (row == null)
                return Enumerable.Empty<ComponentAction>();
            return components.Where(item =>
                item.Kind == CommandKind.EnterCharacter
                    ? item.Character?.CharacterId == version && row.FutureVersions.Any(candidate => candidate.DefinitionId == version)
                    : version == row.CurrentDefinitionId && (Figure(view, item.OwnerId) == figure ||
                        item.Kind == CommandKind.UseNarya && row.Identity == "gandalf"));
        }

        // UI-108: an entry starts with a double-click in the Characters list and ends on the map, so it never
        // hosts its decision in the inspector.
        public static bool OwnsDecision(PlayerView view, TurnDraft draft) =>
            view?.Decision?.Kind == DecisionKind.Action && !view.Winner.HasValue && draft.Kind != CommandKind.EnterCharacter &&
            DecisionFigure(view, draft).Length > 0;

        public static string EnteringFigure(PlayerView view, TurnDraft draft) =>
            draft.Kind == CommandKind.EnterCharacter && draft.Payload.RegionId.Length == 0 && view.DecisionOwner == view.You
                ? Figure(view, draft.Payload.CharacterId) : "";

        public static bool HasReaction(PlayerView view, string id) =>
            id.Length > 0 && view?.Decision != null && view.DecisionOwner == view.You && view.Decision.Kind != DecisionKind.Action &&
            (Figure(view, view.Decision.SourceId) == id || view.Options.Any(option =>
                option.EffectChoices.Any(choice => Figure(view, choice.SourceId) == id)));
    }
}
