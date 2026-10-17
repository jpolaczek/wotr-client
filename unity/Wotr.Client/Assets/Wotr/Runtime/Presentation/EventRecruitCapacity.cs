using System;
using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // A draft prefix must still permit the full mandatory result. It never chooses that result.
    internal static class EventRecruitCapacity
    {
        public static bool CanComplete(TurnDraft draft, ActionOption option, RecruitmentOption candidate, string region)
        {
            var placements = draft.Payload.Placements.Concat(new[] { new UnitPlacementChoice
                { UnitId = EventRecruitDraft.Available(draft, candidate).First(), RegionId = region } }).ToList();
            var groups = option.Recruitment.ToDictionary(g => (g.Nation, g.Kind));
            var units = option.Recruitment.SelectMany(g => g.ReserveIds.Select(id => (Id: id, Group: g))).ToDictionary(p => p.Id, p => p.Group);
            var remaining = groups.ToDictionary(g => g.Key, g => g.Value.ReserveIds.Count - placements.Count(p => units[p.UnitId] == g.Value));
            var limits = option.EventRecruitment.Limits.Select(limit => new RecruitmentLimit
            {
                Nation = limit.Nation,
                RegionId = limit.RegionId,
                Kinds = limit.Kinds,
                Maximum = limit.Maximum - placements.Count(p => p.RegionId == limit.RegionId &&
                    units[p.UnitId].Nation == limit.Nation && limit.Kinds.Contains(units[p.UnitId].Kind))
            }).Where(limit => limit.Maximum > 0).ToList();
            var needed = option.Maximum - placements.Count;
            if (option.EventRecruitment.DistinctNations > 0)
            {
                var used = placements.Select(p => units[p.UnitId].Nation).ToHashSet();
                return option.Recruitment.Where(g => !used.Contains(g.Nation) && g.ReserveIds.Count > 0 && g.Targets.Count > 0)
                    .Select(g => g.Nation).Distinct().Count() >= needed;
            }
            var regions = placements.Select(p => p.RegionId).Distinct().ToList();
            var maximumRegions = option.EventRecruitment.MaximumRegions;
            if (maximumRegions == 1)
                return Capacity(limits.Where(l => l.RegionId == regions[0]), remaining) >= needed;
            if (maximumRegions > 1)
            {
                var existing = limits.Where(l => regions.Contains(l.RegionId)).Sum(l => l.Maximum);
                var extra = limits.Where(l => !regions.Contains(l.RegionId)).GroupBy(l => l.RegionId)
                    .Select(g => g.Sum(l => l.Maximum)).OrderByDescending(count => count).Take(maximumRegions - regions.Count).Sum();
                return Math.Min(remaining.Values.Sum(), existing + extra) >= needed;
            }
            return Capacity(limits, remaining) >= needed;
        }

        private static int Capacity(IEnumerable<RecruitmentLimit> limits, Dictionary<(string Nation, UnitKind Kind), int> available)
        {
            var quotas = limits.ToList();
            var remaining = new Dictionary<(string Nation, UnitKind Kind), int>(available);
            var count = 0;
            foreach (var group in quotas.Where(l => l.Kinds.Count == 1).GroupBy(l => (l.Nation, Kind: l.Kinds[0])))
            {
                var take = Math.Min(group.Sum(l => l.Maximum), remaining.TryGetValue(group.Key, out var reserve) ? reserve : 0);
                remaining[group.Key] = reserve - take;
                count += take;
            }
            foreach (var group in quotas.Where(l => l.Kinds.Count > 1).GroupBy(l => l.Nation))
            {
                remaining.TryGetValue((group.Key, UnitKind.Regular), out var regulars);
                remaining.TryGetValue((group.Key, UnitKind.Elite), out var elites);
                count += Math.Min(group.Sum(l => l.Maximum), regulars + elites);
            }
            return count;
        }
    }
}
