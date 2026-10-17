using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class BattleExcessTrayTests
    {
        [Test]
        public void CompletedBattleExcessKeepsAllFiguresAndReportWithoutInactiveDice()
        {
            var view = Excess("Complete");
            var data = BoardPresentation.Load();
            var root = Render(view, data);
            Assert.AreEqual("Return excess units to the reserve", root.Q<Label>(className: "title").text);
            Assert.IsNull(root.Q("battle-forces"));
            Assert.IsEmpty(root.Query<Button>(className: "battle-die").ToList());
            Assert.AreEqual(12, root.Query<Button>(className: "figure").ToList().Count);
            Assert.IsNotNull(root.Q<Button>("confirm-decision"));
            var report = new BattleReport(null, data);
            report.Apply(view);
            Assert.AreEqual(DisplayStyle.Flex, report.Open.style.display.value);
            Assert.AreEqual("Complete", view.Battle.Stage);
            Assert.IsNotNull(view.LastBattleRound);
        }

        [Test]
        public void PendingExcessDuringActiveBattleStillShowsItsContext()
        {
            var root = Render(Excess("Finish"), BoardPresentation.Load());
            Assert.IsNotNull(root.Q("battle-forces"));
            Assert.IsNotEmpty(root.Query<Button>(className: "battle-die").ToList());
            Assert.AreEqual(12, root.Query<Button>(className: "figure").ToList().Count);
        }

        private static VisualElement Render(PlayerView view, BoardPresentation data)
        {
            var root = new VisualElement();
            var draft = new TurnDraft();
            draft.Choose(CommandKind.RemoveExcess);
            new DecisionTray(root, data).Render(view, draft, false, false);
            return root;
        }

        private static PlayerView Excess(string stage)
        {
            var view = new PlayerView
            {
                You = Side.Shadow,
                Decision = new DecisionView { Id = "excess", Kind = DecisionKind.RemoveExcess },
                Battle = new BattleView { Id = "battle", RegionId = "dol-amroth", Mode = BattleMode.Relief, Stage = stage, Round = 1 }
            };
            var option = new ActionOption { Kind = CommandKind.RemoveExcess, Minimum = 2, Maximum = 2 };
            for (var index = 0; index < 12; index++)
            {
                var unit = new UnitView
                {
                    Id = "unit-" + index,
                    Nation = index < 5 ? "sauron" : "southrons",
                    Kind = UnitKind.Regular,
                    Side = Side.Shadow,
                    Location = "dol-amroth"
                };
                view.Units.Add(unit);
                option.EligibleIds.Add(unit.Id);
            }
            view.Options.Add(option);
            view.Battle.Attacker.Units.AddRange(view.Units.Skip(5));
            view.Battle.Attacker.Dice.Add(new BattleDieView { Id = "roll", Result = 1, HitTarget = 5 });
            view.LastBattleRound = view.Battle;
            return view;
        }
    }
}
