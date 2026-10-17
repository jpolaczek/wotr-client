using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class RecruitDraft
    {
        public static bool Select(TurnDraft draft, PlayerView view, string id)
        {
            var option = view.Options.FirstOrDefault(o => o.Kind == CommandKind.Recruit && o.Enabled);
            var group = option?.Recruitment.FirstOrDefault(g => g.ReserveIds.Contains(id));
            if (group == null)
                return false;
            if (draft.Kind != CommandKind.Recruit)
                DraftGesture.Choose(draft, view, CommandKind.Recruit);
            if (!CanAdd(draft, view, group.Kind) || draft.Payload.Placements.Any(p => p.UnitId == id))
                return false;
            draft.Change(_ => draft.ReserveId = id);
            return true;
        }

        public static bool CanAdd(TurnDraft draft, PlayerView view, UnitKind kind) =>
            draft.Payload.Placements.Count < 2 && (draft.Payload.Placements.Count == 0 || kind != UnitKind.Elite &&
                !view.Units.Any(u => u.Kind == UnitKind.Elite && draft.Payload.Placements.Any(p => p.UnitId == u.Id)));

        public static IEnumerable<string> Targets(TurnDraft draft, PlayerView view)
        {
            var group = view.Options.FirstOrDefault(o => o.Kind == CommandKind.Recruit)?.Recruitment
                .FirstOrDefault(g => g.ReserveIds.Contains(draft.ReserveId));
            return group?.Targets.Where(t => !draft.Payload.Placements.Any(p => p.RegionId == t)) ?? Enumerable.Empty<string>();
        }

        public static bool Place(TurnDraft draft, PlayerView view, string region)
        {
            if (!Targets(draft, view).Contains(region))
                return false;
            draft.Change(p =>
            {
                p.Placements.Add(new UnitPlacementChoice { UnitId = draft.ReserveId, RegionId = region });
                draft.ReserveId = "";
            });
            return true;
        }

        public static void Remove(TurnDraft draft, string id) => draft.Change(p => p.Placements.RemoveAll(c => c.UnitId == id));
    }
}
