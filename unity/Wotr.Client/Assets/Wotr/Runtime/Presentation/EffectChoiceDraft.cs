using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class EffectChoiceDraft
    {
        public static string FigureRegion(EffectChoiceView choice) => choice.SourceId.Length > 0 ? choice.SourceId : choice.TargetId;

        public static string RegionChoice(ActionOption option, string region)
        {
            var choices = option.EffectChoices.Where(c => c.Id == region || c.TargetId == region).ToList();
            return choices.Count == 1 ? choices[0].Id : option.EligibleIds.Contains(region) ? region : "";
        }

        public static void ChooseRegion(TurnDraft draft, ActionOption option, string region)
        {
            var choice = RegionChoice(option, region);
            if (choice.Length > 0)
                Choose(draft, option, choice, region);
            else if (option.EffectChoices.Count(c => c.TargetId == region) > 1)
                draft.Change(p =>
                {
                    p.RegionId = region;
                    p.Ids.Clear();
                });
        }

        public static string FellowshipDestination(PlayerView view, TurnDraft draft) => draft.Kind == CommandKind.ChooseEffect
            ? view.Options.Where(o => o.Kind == draft.Kind).SelectMany(o => o.EffectChoices)
                .FirstOrDefault(c => c.SourceId == "fellowship" && draft.Payload.Ids.Contains(c.Id))?.TargetId ?? ""
            : "";

        public static IEnumerable<UnitPlacementChoice> Placements(PlayerView view, TurnDraft draft)
        {
            if (draft.Kind != CommandKind.ChooseEffect)
                yield break;
            var option = view.Options.First(o => o.Kind == draft.Kind);
            if (view.Decision?.Presentation is "ReserveFigures" or "FellowshipCompanions")
            {
                foreach (var choice in option.EffectChoices.Where(c => draft.Payload.Ids.Contains(c.Id) && c.TargetId.Length > 0))
                    yield return new UnitPlacementChoice { UnitId = choice.Id, RegionId = choice.TargetId };
                yield break;
            }
            if (view.Decision?.Presentation == "ArmyFigures")
            {
                foreach (var choice in option.EffectChoices.Where(c => draft.Payload.Ids.Contains(c.Id) && c.SourceId.Length > 0 && c.TargetId.Length > 0))
                    yield return new UnitPlacementChoice { UnitId = choice.Id, RegionId = choice.TargetId };
                yield break;
            }
            if (view.Decision?.Presentation != "CompanionDestination")
                yield break;
            var destination = option.EffectChoices.FirstOrDefault(c => c.SelectionGroup == "destination" && draft.Payload.Ids.Contains(c.Id));
            if (destination == null)
                yield break;
            foreach (var character in view.Characters.Where(c => draft.Payload.Ids.Contains(c.Id) || c.DefinitionId is "base.character.strider" or "base.character.aragorn"))
                yield return new UnitPlacementChoice { UnitId = character.Id, RegionId = destination.TargetId };
        }

        private static bool Dependencies(CommandPayload payload, EffectChoiceView choice) =>
            choice.RequiresAny.Count == 0 || choice.RequiresAny.Any(payload.Ids.Contains);

        public static bool CanChoose(TurnDraft draft, ActionOption option, string id)
        {
            var choice = option.EffectChoices.FirstOrDefault(c => c.Id == id);
            if (choice == null || choice.Exclusive)
                return true;
            if (!Dependencies(draft.Payload, choice))
                return false;
            return choice.GroupMaximum == 1 || draft.Payload.Ids.Contains(id) ||
                option.EffectChoices.Count(c => c.SelectionGroup == choice.SelectionGroup && !c.Exclusive && draft.Payload.Ids.Contains(c.Id)) < choice.GroupMaximum;
        }

        public static void Choose(TurnDraft draft, ActionOption option, string id, string region = null)
        {
            if (!CanChoose(draft, option, id))
                return;
            var choice = option.EffectChoices.FirstOrDefault(c => c.Id == id);
            draft.Change(p =>
            {
                if (region != null)
                    p.RegionId = region;
                var optionalSingle = choice != null && choice.GroupMinimum == 0 && choice.GroupMaximum == 1 && p.Ids.Contains(id);
                if (choice == null || choice.Exclusive || choice.SelectionGroup.Length == 0)
                    p.Ids.Clear();
                else
                    p.Ids.RemoveAll(selected => option.EffectChoices.Any(c => c.Id == selected &&
                        (c.Exclusive || choice.GroupMaximum == 1 && c.SelectionGroup == choice.SelectionGroup)));
                if (!p.Ids.Remove(id) && !optionalSingle)
                    p.Ids.Add(id);
                p.Ids.RemoveAll(selected => option.EffectChoices.Any(c => c.Id == selected && !Dependencies(p, c)));
            });
        }

        public static bool Ready(TurnDraft draft, ActionOption option)
        {
            var ids = draft.Payload.Ids;
            if (ids.Count < option.Minimum || ids.Count > option.Maximum || ids.Distinct().Count() != ids.Count || !ids.All(option.EligibleIds.Contains))
                return false;
            var selected = option.EffectChoices.Where(c => ids.Contains(c.Id)).ToList();
            if (selected.Any(c => c.Exclusive))
                return ids.Count == 1;
            return selected.All(c => Dependencies(draft.Payload, c)) &&
                option.EffectChoices.Where(c => c.SelectionGroup.Length > 0 && !c.Exclusive).GroupBy(c => c.SelectionGroup)
                    .All(group => group.Count(c => ids.Contains(c.Id)) >= group.First().GroupMinimum && group.Count(c => ids.Contains(c.Id)) <= group.First().GroupMaximum);
        }

        public static void Reconcile(TurnDraft draft, ActionOption option)
        {
            draft.Payload.Ids.RemoveAll(id => !option.EligibleIds.Contains(id));
            int removed;
            do
            {
                removed = draft.Payload.Ids.RemoveAll(id => option.EffectChoices.Any(choice =>
                    choice.Id == id && !Dependencies(draft.Payload, choice)));
            } while (removed > 0);
            if (draft.Payload.RegionId.Length > 0 && !option.Targets.Contains(draft.Payload.RegionId) &&
                !option.EffectChoices.Any(choice => FigureRegion(choice) == draft.Payload.RegionId))
                draft.Payload.RegionId = "";
        }
    }
}
