using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class BattleReportDetails
    {
        public static void Render(VisualElement root, BattleReportView report, BattleReportRound round,
            BoardPresentation data, CardInspector inspector)
        {
            foreach (var cost in report.Costs)
                Add(root, "Action cost · " + ActionCardText.Cost(cost));
            var snapshot = round?.Result ?? round?.Initial ?? report.Latest ?? report.Initial;
            if (snapshot != null)
            {
                var recorded = new PlayerView();
                if (report.Place != null)
                {
                    recorded.Units = report.Place.Units;
                    recorded.Characters = report.Place.Characters;
                }
                new BattleContext(root, snapshot, recorded, null, null, null, inspector).Refresh();
            }
            if (report.Place != null)
                Add(root, "Recorded control · " + (report.Place.Controller.HasValue ? ActionCardText.Actor(report.Place.Controller) : "Neutral"));
            if (report.Outcome != null)
                Add(root, "Victory points · Free Peoples " + report.Outcome.FreePeoplesPoints + " · Shadow " + report.Outcome.ShadowPoints);
            var ids = round?.StepIds.ToHashSet();
            var steps = report.Steps.Where(s => ids == null || ids.Contains(s.Id) ||
                s.Kind == "BattleEnded" || s.Command == CommandKind.AdvanceBattle || s.Command == CommandKind.StopBattle);
            foreach (var step in steps)
            {
                Add(root, ActionCardText.Words(step.Command?.ToString() ?? step.Kind), "battle-report-step");
                var facts = ActionFactText.Lines(step, data).Distinct().ToArray();
                if (facts.Length == 0) Add(root, ReadableText.Of(step.Text, data));
                foreach (var line in facts) Add(root, line);
            }
        }

        private static void Add(VisualElement parent, string text, string css = "battle-report-detail")
        {
            var label = new Label(text) { enableRichText = false };
            label.AddToClassList(css);
            parent.Add(label);
        }
    }
}
