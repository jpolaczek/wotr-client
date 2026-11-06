using System.Collections.Generic;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-509: how each stage of each Hunt of a chain stood, from the public report that comes with every view: a stage's last entry.
    // An entry with no snapshot of its own (a change of corruption or place) leaves the last one standing.
    public sealed class HuntStageHistory
    {
        private readonly Dictionary<(int Hunt, int Stage), HuntView> stages = new Dictionary<(int, int), HuntView>();
        private readonly List<int> hunts = new List<int>();

        public IReadOnlyList<int> Hunts => hunts;

        public static HuntStageHistory From(HuntReportView report)
        {
            var history = new HuntStageHistory();
            if (report == null)
                return history;
            HuntView last = null;
            var hunt = 0;
            foreach (var entry in report.Entries)
            {
                if (entry.HuntId != hunt)
                {
                    hunt = entry.HuntId;
                    last = null;
                    history.hunts.Add(hunt);
                }
                last = entry.Hunt ?? last;
                if (last == null)
                    continue;
                var stage = StageOf(entry.Kind, last.Mordor);
                if (stage >= 0)
                    history.stages[(hunt, stage)] = last;
            }
            return history;
        }

        public HuntView At(int hunt, int stage) => stages.TryGetValue((hunt, stage), out var view) ? view : null;

        // What stood at this stage: its own entry, or the last one before it in the Hunt, since nothing changed in between.
        public HuntView Nearest(int hunt, int stage)
        {
            for (var earlier = stage; earlier >= 0; earlier--)
                if (At(hunt, earlier) is { } view)
                    return view;
            return null;
        }

        // A stage of HuntWindowState.Stages: Roll, Re-roll, Tile, Damage, Result; or Tile, Damage, Step in Mordor.
        public static int StageOf(HuntReportKind kind, bool mordor)
        {
            var stage = RollStage(kind);
            return stage < 0 || !mordor ? stage : stage < 2 ? -1 : stage == 4 ? 2 : stage - 2;
        }

        private static int RollStage(HuntReportKind kind) => kind switch
        {
            HuntReportKind.Started => -1,
            HuntReportKind.Rolled => 0,
            HuntReportKind.Rerolled => 1,
            HuntReportKind.TileDrawn or HuntReportKind.TileReplaced => 2,
            HuntReportKind.Revealed or HuntReportKind.Progressed or HuntReportKind.HuntCompleted => 4,
            _ => 3
        };
    }
}
