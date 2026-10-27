using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.EditorTests
{
    public sealed class BattleConclusionTests
    {
        [TestCase(UnitKind.Regular, false, "Eliminate a Regular · Corrupted sovereign's weakness.")]
        [TestCase(UnitKind.Elite, false, "No regular available · Elite removed.")]
        [TestCase(UnitKind.Elite, true, "Elite → Regular.")]
        public void SiegeCostUsesActualFigureAndPreviewInsteadOfBattleHits(UnitKind kind, bool replacement, string expected)
        {
            var view = BattleScreenFixture.Create(CommandKind.ExtendSiege);
            var id = view.Options[0].EligibleIds[0];
            view.Units.Single(u => u.Id == id).Kind = kind;
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ExtendSiege);
            draft.Toggle(id);
            var preview = new ActionPreview { Legal = true };
            preview.RemovedIds.Add(id);
            if (replacement) preview.Placements.Add(new UnitPlacementChoice { UnitId = "regular", RegionId = "lossarnach" });
            var text = BattleSummary.Describe(draft, view, preview);
            StringAssert.StartsWith(expected, text);
            StringAssert.DoesNotContain("hit", text);
        }

        [Test]
        public void HistoricalReportRetainsItsVersionAndNeverCreatesRerollInputs()
        {
            var report = JsonConvert.DeserializeObject<BattleReportView>(File.ReadAllText(Path.Combine(Application.dataPath, "Wotr/Tests/Editor/BattleReportWire.json")));
            var panel = new BattleReport(null, BoardPresentation.Load());
            var view = new PlayerView { MatchId = report.MatchId };
            panel.Apply(view);
            panel.Show(report);
            var originalVersion = panel.RequestVersion;
            view.LastBattleRound = new BattleView { Id = "new-battle", RegionId = "lossarnach" };
            panel.Apply(view);
            Assert.AreSame(report, panel.Displayed);
            Assert.AreEqual(originalVersion, panel.RequestVersion);
            Assert.IsNotEmpty(panel.Root.Query<Button>(className: "battle-die").ToList());
            Assert.IsTrue(panel.Root.Query<Button>(className: "battle-die").ToList().All(b => b.userData == null && !b.enabledSelf));
            StringAssert.Contains("Round 2", panel.Root.Q<Label>("battle-report-round").text);
            panel.Close();
            Assert.Greater(panel.RequestVersion, originalVersion, "A late GET must not reopen a closed report.");
        }

        [Test]
        public void NoRollReportKeepsSettlementStatusAndDoesNotInventRoundOne()
        {
            var panel = new BattleReport(null, BoardPresentation.Load());
            panel.Show(new BattleReportView { Status = BattleReportStatus.AwaitingSettlement });
            Assert.AreEqual("Awaiting Settlement", panel.Root.Q<Label>("battle-report-status").text);
            Assert.AreEqual("No recorded Combat roll", panel.Root.Q<Label>("battle-report-round").text);
            Assert.IsFalse(panel.Root.Q<Button>("next-battle-round").enabledSelf);
            Assert.IsEmpty(panel.Root.Query<Button>(className: "battle-die").ToList());
        }
    }
}
