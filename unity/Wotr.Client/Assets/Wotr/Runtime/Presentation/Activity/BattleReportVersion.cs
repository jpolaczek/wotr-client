using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class BattleReportVersion
    {
        public static BattleReportView Through(BattleReportView source, ActionLogView action)
        {
            if (source.Legacy || action.Steps.Count == 0) return source;
            var known = action.Steps.Select(s => s.Id).ToHashSet();
            var last = action.Steps.Last();
            var revision = last.Revision;
            var boundary = source.Steps.FindLastIndex(s => known.Contains(s.Id));
            // A committed command includes its automatic consequences in the same revision.
            // An individual historical event still stops at its exact position.
            var steps = source.Steps.Where((s, i) => s.Revision < revision ||
                s.Revision == revision && (last.Command.HasValue || i <= boundary)).ToList();
            if (steps.Count == source.Steps.Count && action.Action.Status == ActionLogStatus.Completed)
                return source;
            var ended = steps.Any(s => s.Kind == "BattleEnded");
            var report = new BattleReportView
            {
                MatchId = source.MatchId, Id = source.Id, ActionId = source.ActionId,
                Version = steps.LastOrDefault()?.Id ?? "", Initiator = source.Initiator, Turn = source.Turn,
                Costs = source.Costs, PartialHistory = source.PartialHistory,
                Status = ended ? BattleReportStatus.AwaitingSettlement : BattleReportStatus.InProgress,
                Steps = steps,
                Initial = steps.Select(s => s.Facts.Battle).FirstOrDefault(b => b != null),
                Latest = steps.Select(s => s.Facts.Battle).LastOrDefault(b => b != null),
                Place = steps.Select(s => s.Facts.BattlePlace).LastOrDefault(p => p != null),
                Outcome = steps.Select(s => s.Facts.Outcome).LastOrDefault(o => o != null)
            };
            foreach (var round in source.Rounds)
            {
                var ids = round.StepIds.ToHashSet();
                var visible = steps.Where(s => ids.Contains(s.Id)).ToList();
                if (!visible.Any(s => s.Facts.Battle is { } battle &&
                    (battle.CardsRevealed || battle.Attacker.Dice.Count > 0 || battle.Defender.Dice.Count > 0))) continue;
                report.Rounds.Add(new BattleReportRound
                {
                    Number = round.Number, Completed = visible.Any(s => s.Kind == "BattleRoundCompleted"),
                    StepIds = visible.Select(s => s.Id).ToList(),
                    Initial = visible.Select(s => s.Facts.Battle).FirstOrDefault(b => b != null),
                    Result = visible.Select(s => s.Facts.Battle).LastOrDefault(b => b != null)
                });
            }
            return report;
        }
    }
}
