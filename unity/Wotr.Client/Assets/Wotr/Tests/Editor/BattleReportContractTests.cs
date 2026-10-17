using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    public class BattleReportContractTests
    {
        [Test]
        public void ReadsActualHttpReportWithTwoRoundsLossStayAndHistoricalParticipants()
        {
            var raw = File.ReadAllText(Path.Combine(Application.dataPath, "Wotr/Tests/Editor/BattleReportWire.json"));
            var report = JsonConvert.DeserializeObject<BattleReportView>(raw);
            Assert.AreEqual(BattleReportStatus.Completed, report.Status);
            Assert.IsFalse(report.PartialHistory);
            Assert.AreEqual(new[] { 1, 2 }, report.Rounds.Select(r => r.Number));
            Assert.IsTrue(report.Rounds.All(r => r.Completed));
            Assert.AreEqual(1, report.Rounds[0].Result.Attacker.Dice[0].Result);
            Assert.AreEqual(6, report.Rounds[1].Result.Attacker.Dice[0].Result);
            Assert.AreEqual(ActionCostKind.Die, report.Costs.Single().Kind);
            Assert.AreEqual(DieFace.Army, report.Costs[0].Face);
            Assert.AreEqual("minas-tirith", report.Initial.Attacker.Units.Single().Location);
            Assert.AreEqual("minas-tirith", report.Place.Units.Single(u => u.Side == Side.FreePeoples).Location);
            Assert.IsTrue(report.Steps.Any(s => s.Command == CommandKind.AdvanceBattle && s.Context.BattleRound == 2));
            Assert.IsTrue(report.Steps.Any(s => s.Facts.LossDetails.Count > 0));
            Assert.IsNotEmpty(report.Version);
            Assert.IsFalse(raw.Contains("randomSamples"));
            Assert.IsFalse(raw.Contains("privateChoices"));
        }

        [Test]
        public void OldPlayerViewAndPartialReportKeepUnknownDataUnknown()
        {
            var old = JsonConvert.DeserializeObject<PlayerView>("{\"matchId\":\"old\",\"revision\":7}");
            Assert.IsNull(old.LastBattleRound);
            var report = JsonConvert.DeserializeObject<BattleReportView>("{\"id\":\"old-battle\",\"legacy\":true,\"partialHistory\":true}");
            Assert.AreEqual(BattleReportStatus.Unknown, report.Status);
            Assert.IsNull(report.Initiator);
            Assert.IsNull(report.Turn);
            Assert.IsNull(report.Initial);
            Assert.IsNull(report.Place);
            Assert.IsEmpty(report.Rounds);
            Assert.IsEmpty(report.Costs);
        }
    }
}
