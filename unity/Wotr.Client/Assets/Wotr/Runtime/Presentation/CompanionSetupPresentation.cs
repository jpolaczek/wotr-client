using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class CompanionSetupPresentation
    {
        public static bool Owns(PlayerView view) => view != null && !view.Winner.HasValue &&
            view.You == Side.FreePeoples && view.DecisionOwner == view.You &&
            view.Decision?.Kind is DecisionKind.CouncilVersions or DecisionKind.CouncilHomelands;

        public static string Missing(TurnDraft draft, ActionOption option)
        {
            if (option == null)
                return "Waiting for the current choices";
            var required = option.CompanionVersions.FirstOrDefault(group =>
                group.Required && string.IsNullOrEmpty(CouncilDraft.Version(draft.Payload, group)));
            if (required != null)
                return "Choose a version for " + CharacterLibrary.Name(required.FigureId);
            return option.Targets.Contains(draft.Payload.GuideId) ? "" : "Choose the starting Guide";
        }

        public static string Definition(TurnDraft draft, ActionOption option, string figure)
        {
            var group = option.CompanionVersions.FirstOrDefault(candidate => candidate.FigureId == figure);
            return group == null ? figure : CouncilDraft.Version(draft.Payload, group);
        }
    }
}
