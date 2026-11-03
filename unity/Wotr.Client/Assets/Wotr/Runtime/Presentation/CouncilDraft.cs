using System.Collections.Generic;
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

        // UI-451 (owner): a click on a lit homeland starts its companion there — the selected companion when it is his, else
        // the one companion not yet placed there whose homeland it is (Fate's Gimli moves between his two). A homeland two
        // unplaced companions share (the Shire, Merry and Pippin) waits, gold, for the portrait of the one to start there.
        public static bool Region(TurnDraft draft, ActionOption option, string region)
        {
            if (option == null)
                return false;
            var selected = option.CharacterActions.FirstOrDefault(c => c.CharacterId == draft.Payload.CharacterId);
            if (selected?.Targets.Contains(region) == true)
            {
                Place(draft, selected.CharacterId, region);
                return true;
            }
            var candidates = Candidates(draft, option, region);
            if (candidates.Count == 1)
                Place(draft, candidates[0], region);
            else if (candidates.Count > 1)
                draft.Gesture(() =>
                {
                    draft.Change(p => p.CharacterId = "");
                    draft.ArmedSource = region;
                });
            return candidates.Count > 0;
        }

        // UI-451: a portrait clicked while a shared homeland waits starts that companion there; false otherwise.
        public static bool Portrait(TurnDraft draft, ActionOption option, string id)
        {
            var region = Waiting(draft);
            if (option == null || region.Length == 0 || !Candidates(draft, option, region).Contains(id))
                return false;
            Place(draft, id, region);
            return true;
        }

        // The shared homeland waiting for its companion, or "".
        public static string Waiting(TurnDraft draft) => draft.Kind == CommandKind.ChooseCouncilHomelands ? draft.ArmedSource : "";

        // The companions this homeland may still take: theirs, and not already placed there.
        public static List<string> Candidates(TurnDraft draft, ActionOption option, string region) => option.CharacterActions
            .Where(c => c.Targets.Contains(region) && !draft.Payload.Placements.Any(p => p.UnitId == c.CharacterId && p.RegionId == region))
            .Select(c => c.CharacterId).ToList();

        // Every homeland a companion may still start in, lit at once.
        public static IEnumerable<string> OpenHomelands(TurnDraft draft, ActionOption option) => option.CharacterActions
            .SelectMany(c => c.Targets.Where(region => !draft.Payload.Placements.Any(p => p.UnitId == c.CharacterId && p.RegionId == region)))
            .Distinct();

        // One gesture, so the console's ↶ takes it back whole.
        private static void Place(TurnDraft draft, string id, string region) => draft.Gesture(() =>
        {
            Homeland(draft, id, region);
            draft.Change(p => p.CharacterId = id);
            draft.ArmedSource = "";
        });

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
