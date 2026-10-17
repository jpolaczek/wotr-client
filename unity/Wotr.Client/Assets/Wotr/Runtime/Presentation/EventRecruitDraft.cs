using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class EventRecruitDraft
    {
        public static ActionOption Option(PlayerView view) => view?.Options.FirstOrDefault(o =>
            o.Kind == CommandKind.ChooseEffect && o.Enabled && o.EventRecruitment != null);

        public static IEnumerable<string> Available(TurnDraft draft, RecruitmentOption group) =>
            group.ReserveIds.Where(id => draft.Payload.Placements.All(p => p.UnitId != id));

        public static IEnumerable<string> Targets(TurnDraft draft, ActionOption option)
        {
            if (option?.EventRecruitment == null || draft.Payload.Placements.Count >= option.Maximum)
                return Enumerable.Empty<string>();
            var groups = option.Recruitment.Where(g => draft.ReserveId.Length == 0 || g.ReserveIds.Contains(draft.ReserveId));
            return groups.Where(g => Available(draft, g).Any()).SelectMany(g => g.Targets.Where(region => CanPlace(draft, option, g, region))).Distinct();
        }

        private static bool CanPlace(TurnDraft draft, ActionOption option, RecruitmentOption group, string region)
        {
            if (!group.Targets.Contains(region))
                return false;
            if (option.EventRecruitment.DistinctNations > 0 && option.Recruitment.Where(g => g.Nation == group.Nation)
                .Any(g => draft.Payload.Placements.Any(p => g.ReserveIds.Contains(p.UnitId))))
                return false;
            var maximumRegions = option.EventRecruitment.MaximumRegions;
            var regions = draft.Payload.Placements.Select(p => p.RegionId).Distinct().ToList();
            if (maximumRegions > 0 && !regions.Contains(region) && regions.Count >= maximumRegions)
                return false;
            var limit = option.EventRecruitment.Limits.FirstOrDefault(l => l.Nation == group.Nation &&
                l.RegionId == region && l.Kinds.Contains(group.Kind));
            if (limit == null)
                return false;
            var ids = option.Recruitment.Where(g => g.Nation == limit.Nation && limit.Kinds.Contains(g.Kind))
                .SelectMany(g => g.ReserveIds).ToHashSet();
            return draft.Payload.Placements.Count(p => p.RegionId == region && ids.Contains(p.UnitId)) < limit.Maximum &&
                EventRecruitCapacity.CanComplete(draft, option, group, region);
        }

        public static bool Select(TurnDraft draft, ActionOption option, string id)
        {
            var group = option?.Recruitment.FirstOrDefault(g => g.ReserveIds.Contains(id));
            if (group == null || !Available(draft, group).Contains(id) || draft.Payload.Placements.Count >= option.Maximum)
                return false;
            if (!group.Targets.Any(region => CanPlace(draft, option, group, region)))
                return false;
            // Region first: with no token armed, a token click fills the chosen region. An armed token is switched
            // or released instead (UI-213).
            if (draft.ReserveId.Length == 0 && draft.Payload.RegionId.Length > 0 && CanPlace(draft, option, group, draft.Payload.RegionId))
                return Drop(draft, option, id, draft.Payload.RegionId);
            draft.Change(_ => draft.ReserveId = draft.ReserveId == id ? "" : id);
            return true;
        }

        public static bool Region(TurnDraft draft, ActionOption option, string region)
        {
            if (!Targets(draft, option).Contains(region))
                return false;
            if (draft.ReserveId.Length > 0)
                return Drop(draft, option, draft.ReserveId, region);
            draft.Change(p => p.RegionId = region);
            return true;
        }

        public static bool Drop(TurnDraft draft, ActionOption option, string id, string region)
        {
            var group = option?.Recruitment.FirstOrDefault(g => g.ReserveIds.Contains(id));
            if (group == null || !Available(draft, group).Contains(id) || draft.Payload.Placements.Count >= option.Maximum ||
                !CanPlace(draft, option, group, region))
                return false;
            var armed = draft.ReserveId == id;
            draft.Change(p =>
            {
                p.Placements.Add(new UnitPlacementChoice { UnitId = id, RegionId = region });
                p.RegionId = region;
                draft.ReserveId = armed ? Next(draft, option, group) : "";
            });
            return true;
        }

        // UI-213: an armed token stays armed with its group's next free figure while the card's limits (per region,
        // per nation and in total) leave that figure a region.
        private static string Next(TurnDraft draft, ActionOption option, RecruitmentOption group)
        {
            var id = Available(draft, group).FirstOrDefault();
            if (id == null || draft.Payload.Placements.Count >= option.Maximum)
                return "";
            return group.Targets.Any(region => CanPlace(draft, option, group, region)) ? id : "";
        }

        public static void Remove(TurnDraft draft, string id) => draft.Change(p =>
        {
            p.Placements.RemoveAll(placement => placement.UnitId == id);
            draft.ReserveId = "";
        });

        public static bool Ready(TurnDraft draft, ActionOption option)
        {
            if (draft.ReserveId.Length > 0 || draft.Payload.Ids.Count > 0 || draft.Payload.Placements.Count != option.Maximum)
                return false;
            var check = new TurnDraft();
            check.Choose(CommandKind.ChooseEffect);
            return draft.Payload.Placements.All(p => Drop(check, option, p.UnitId, p.RegionId));
        }

        public static void Reconcile(TurnDraft draft, ActionOption option)
        {
            // Aggregate limits may require a strategic choice about which legal placement to remove.
            draft.Payload.Placements.RemoveAll(p => !option.Recruitment.Any(group =>
                group.ReserveIds.Contains(p.UnitId) && group.Targets.Contains(p.RegionId) &&
                option.EventRecruitment.Limits.Any(limit => limit.Nation == group.Nation &&
                    limit.RegionId == p.RegionId && limit.Kinds.Contains(group.Kind) && limit.Maximum > 0)));
            draft.Payload.Ids.Clear();
            if (!option.Recruitment.Any(g => Available(draft, g).Contains(draft.ReserveId)))
                draft.ReserveId = "";
            if (!option.Targets.Contains(draft.Payload.RegionId))
                draft.Payload.RegionId = "";
        }
    }
}
