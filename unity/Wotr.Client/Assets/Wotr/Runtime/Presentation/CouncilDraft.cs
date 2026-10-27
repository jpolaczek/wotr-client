using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class CouncilDraft
    {
        public static bool Handles(CommandKind? kind) => kind is CommandKind.ChooseCompanionVersions
            or CommandKind.ChooseCouncilHomelands or CommandKind.ChooseCouncilToken;

        public static void Version(TurnDraft draft, CompanionVersionOption group, string id) => draft.Change(p =>
        {
            p.Ids.RemoveAll(value => value == group.BaseId || value == group.AlternateId);
            p.Ids.Add(id);
        });

        public static string Version(CommandPayload payload, CompanionVersionOption group) =>
            payload.Ids.FirstOrDefault(id => id == group.BaseId || id == group.AlternateId) ?? (group.Required ? "" : group.BaseId);

        public static void Homeland(TurnDraft draft, string character, string region) => draft.Change(p =>
        {
            p.Placements.RemoveAll(place => place.UnitId == character);
            if (region.Length > 0)
                p.Placements.Add(new UnitPlacementChoice { UnitId = character, RegionId = region });
        });

        public static bool Region(TurnDraft draft, ActionOption option, string region)
        {
            var choice = option.CharacterActions.FirstOrDefault(c => c.CharacterId == draft.Payload.CharacterId);
            if (choice?.Targets.Contains(region) != true)
                return false;
            Homeland(draft, choice.CharacterId, region);
            return true;
        }

        // UI-370 (owner): the Council's setup starts complete — Gandalf the Grey as Guide and every required companion
        // as printed — so Confirm can be clicked at once; the crowns and the version cards still change both.
        private const string Gandalf = "base.character.gandalf-grey";

        public static void Defaults(TurnDraft draft, ActionOption option)
        {
            if (option == null || draft.Kind != CommandKind.ChooseCompanionVersions)
                return;
            draft.Change(p =>
            {
                if (p.GuideId.Length == 0 && option.Targets.Contains(Gandalf))
                    p.GuideId = Gandalf;
                foreach (var group in option.CompanionVersions.Where(group => group.Required && Version(p, group).Length == 0))
                    p.Ids.Add(group.BaseId);
            });
        }

        public static bool Ready(TurnDraft draft, ActionOption option)
        {
            var p = draft.Payload;
            if (draft.Kind == CommandKind.ChooseCouncilToken)
                return p.Ids.Count == 1 && option.EligibleIds.Contains(p.Ids[0]);
            if (draft.Kind == CommandKind.ChooseCouncilHomelands)
                return p.Placements.Select(x => x.UnitId).Distinct().Count() == p.Placements.Count && p.Placements.All(place =>
                    option.CharacterActions.Any(c => c.CharacterId == place.UnitId && c.Targets.Contains(place.RegionId)));
            return option.Targets.Contains(p.GuideId) && p.Ids.All(option.EligibleIds.Contains) && option.CompanionVersions.All(group =>
                p.Ids.Count(id => id == group.BaseId || id == group.AlternateId) == (group.Required ? 1 :
                    p.Ids.Any(id => id == group.BaseId || id == group.AlternateId) ? 1 : 0));
        }

        public static void Reconcile(TurnDraft draft, ActionOption option)
        {
            var p = draft.Payload;
            p.Ids.RemoveAll(id => !option.EligibleIds.Contains(id));
            if (!option.Targets.Contains(p.GuideId))
                p.GuideId = "";
            p.Placements.RemoveAll(place => !option.CharacterActions.Any(c => c.CharacterId == place.UnitId && c.Targets.Contains(place.RegionId)));
            if (!option.CharacterActions.Any(c => c.CharacterId == p.CharacterId))
                p.CharacterId = "";
        }
    }
}
