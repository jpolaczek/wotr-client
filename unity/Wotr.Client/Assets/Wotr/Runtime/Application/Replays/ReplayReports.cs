using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // REPLAY-05: battle reports and Log action cards of a replay, from the file and cut at the step shown: a battle's
    // report appears once it was complete; before that the battle shows through its own window only.
    public sealed class ReplayReports
    {
        private readonly List<ReplayBattleReport> battles;
        private readonly BattleReport report;
        private readonly ActionLogPanel log;
        private long cutoff;

        public ReplayReports(ReplayArchive archive, BattleReport report, ActionLogPanel log)
        {
            battles = archive.Battles();
            this.report = report;
            this.log = log;
            report.Requested += Requested;
            log.Inspect += Inspect;
        }

        public void Cut(long revision) => cutoff = revision;

        public BattleReportView Report(string battle) =>
            battles.FirstOrDefault(b => b.Report.Id == battle && b.Revision <= cutoff)?.Report;

        private void Requested(string battle, int version)
        {
            var found = Report(battle);
            if (found != null)
                report.Show(found);
        }

        // The card of an action from the Log, with its battle's report when that was complete at the step shown.
        private void Inspect(ActionOpenRequest request, bool pinned)
        {
            var stored = log.Model.Find(request.ActionId);
            var view = stored == null ? null : ActionVersion.Through(stored, request.Version, request.IncludeConsequences);
            if (view == null)
            {
                log.SetCardStatus("This recorded version is unavailable", true);
                return;
            }
            var battle = ActionContextModel.For(view, request.StepId).BattleId;
            var found = battle.Length == 0 ? null : Report(battle);
            log.ShowAction(view, true, found == null ? null : BattleReportVersion.Through(found, view));
        }
    }
}
