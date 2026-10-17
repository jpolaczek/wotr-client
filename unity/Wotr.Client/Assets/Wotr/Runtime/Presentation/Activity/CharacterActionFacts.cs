using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Only recorded figures and explicitly returned choices describe the past.
    public static class CharacterActionFacts
    {
        public static string Figure(ActionFigureChange change, BoardPresentation data)
        {
            var figure = change.After ?? change.Before;
            if (figure == null)
                return "";
            var name = ActionCardText.Participants(new[] { figure }).First();
            if (figure.DefinitionId.Length > 0)
                name = Version(figure.DefinitionId) + (figure.Garrison ? " · Garrison" : "");
            if (change.Before?.DefinitionId.Length > 0 && change.After?.DefinitionId.Length > 0 &&
                change.Before.DefinitionId != change.After.DefinitionId)
                name = Version(change.Before.DefinitionId) + " → " + name;
            if (change.From == change.To || change.From.Length == 0)
                return name + (change.To.Length == 0 ? "" : " · " + ActionCardText.Region(change.To, data));
            return name + " · " + ActionCardText.Region(change.From, data) + " → " + ActionCardText.Region(change.To, data);
        }

        public static IEnumerable<string> Setup(ActionLogStep step)
        {
            // Other Targets may be private tile IDs or a different choice shape.
            if (step.Command != CommandKind.ChooseCompanionVersions)
                yield break;
            foreach (var id in step.Facts.Targets)
                yield return "Selected version · " + Version(id);
        }

        public static string Guide(ActionFacts facts)
        {
            var id = facts.FellowshipAfter?.GuideId;
            if (string.IsNullOrEmpty(id))
                return "";
            var recorded = facts.Figures.Select(change => change.After)
                .LastOrDefault(figure => figure?.Id == id && figure.DefinitionId.Length > 0);
            // Figure identity alone must not silently become the base card's version.
            var name = recorded == null ? FigureGrid.Name(CharacterLibrary.Identity(id)) : Version(recorded.DefinitionId);
            return "Guide · " + name;
        }

        private static string Version(string id) => CharacterLibrary.Name(id) + " · " + CharacterLibrary.Edition(id) +
            (id == "fate.character.gimli-dwarven-lord" ? " · Alternate" : "");

        public static bool IsFellowshipContext(ActionLogStep step)
        {
            if (step.Context.BattleId.Length > 0 || step.Context.HuntChainId.Length > 0)
                return false;
            return step.Command is CommandKind.ChooseCompanionVersions or CommandKind.ChooseCouncilHomelands or CommandKind.SelectGuide ||
                step.Kind == "CompanionRevealed" && step.Facts.Figures.Any(change => change.To == "fellowship");
        }
    }
}
