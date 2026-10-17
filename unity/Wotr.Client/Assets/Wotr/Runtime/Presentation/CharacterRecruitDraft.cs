using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class CharacterRecruitDraft
    {
        public static IEnumerable<string> Targets(TurnDraft draft, CharacterActionOption choice) => choice?.Recruitment
            .Where(g => draft.ReserveId.Length == 0 || g.ReserveIds.Contains(draft.ReserveId))
            .SelectMany(g => g.Targets.Where(region => g.ReserveIds.Any(id => CanPlace(draft, choice, id, region)))).Distinct()
            ?? Enumerable.Empty<string>();

        public static bool CanPlace(TurnDraft draft, CharacterActionOption choice, string id, string region)
        {
            var placements = draft.Payload.Placements.Concat(new[] { new UnitPlacementChoice { UnitId = id, RegionId = region } }).ToList();
            return placements.Count <= (choice.RulerRecruitment == null ? 1 : choice.TargetCount) && Fits(choice, placements);
        }

        public static bool Fits(CharacterActionOption choice, IReadOnlyList<UnitPlacementChoice> placements)
        {
            if (placements.Select(p => p.UnitId).Distinct().Count() != placements.Count || placements.Any(p =>
                !choice.Recruitment.Any(g => g.ReserveIds.Contains(p.UnitId) && g.Targets.Contains(p.RegionId))))
                return false;
            var rule = choice.RulerRecruitment;
            if (rule == null)
                return placements.Count <= 1;
            if (Settlements(choice, placements))
                return true;
            return placements.Any(local => local.RegionId == rule.LocalRegion &&
                choice.Recruitment.Any(g => g.ReserveIds.Contains(local.UnitId) && (g.Kind == UnitKind.Regular ||
                    g.Kind == (rule.LocalAllowsElite ? UnitKind.Elite : UnitKind.Leader))) &&
                Settlements(choice, placements.Where(p => p != local).ToList(), true));
        }

        private static bool Settlements(CharacterActionOption choice, IReadOnlyList<UnitPlacementChoice> placements, bool local = false) =>
            placements.Count <= choice.RulerRecruitment.SettlementCount && placements.Select(p => p.RegionId).Distinct().Count() == placements.Count &&
            placements.All(p => choice.RulerRecruitment.Settlements.Contains(p.RegionId) &&
                (!local || !choice.RulerRecruitment.LocalMustDiffer || p.RegionId != choice.RulerRecruitment.LocalRegion) &&
                choice.Recruitment.Any(g => g.ReserveIds.Contains(p.UnitId) && g.Kind is UnitKind.Regular or UnitKind.Leader));

        public static bool Select(TurnDraft draft, CharacterActionOption choice, string id)
        {
            if (!choice.Recruitment.Any(g => g.ReserveIds.Contains(id) && g.Targets.Any(r => CanPlace(draft, choice, id, r))))
                return false;
            if (CanPlace(draft, choice, id, draft.Payload.RegionId))
                return Drop(draft, choice, id, draft.Payload.RegionId);
            draft.Change(_ => draft.ReserveId = draft.ReserveId == id ? "" : id);
            return true;
        }

        public static bool Region(TurnDraft draft, CharacterActionOption choice, string region)
        {
            if (!Targets(draft, choice).Contains(region))
                return false;
            if (draft.ReserveId.Length > 0)
                return Drop(draft, choice, draft.ReserveId, region);
            draft.Change(p => p.RegionId = region);
            return true;
        }

        public static bool Drop(TurnDraft draft, CharacterActionOption choice, string id, string region)
        {
            if (!CanPlace(draft, choice, id, region))
                return false;
            draft.Change(p =>
            {
                p.Placements.Add(new UnitPlacementChoice { UnitId = id, RegionId = region });
                p.RegionId = region;
                draft.ReserveId = "";
            });
            return true;
        }

        public static bool Ready(TurnDraft draft, CharacterActionOption choice) => draft.ReserveId.Length == 0 &&
            (choice.RulerRecruitment == null || draft.Payload.Placements.Count == choice.TargetCount) && Fits(choice, draft.Payload.Placements);
    }
}
