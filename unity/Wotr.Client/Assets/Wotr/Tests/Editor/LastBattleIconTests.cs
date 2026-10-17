using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    // UI-112: the ⚔ control announces an unread Combat round and hides once the report showed it.
    public sealed class LastBattleIconTests
    {
        private const string Match = "match";
        private string root;

        [SetUp] public void SetUp() => root = Path.Combine(Path.GetTempPath(), "wotr-battle-read-" + Guid.NewGuid().ToString("N"));
        [TearDown] public void TearDown() { if (Directory.Exists(root)) Directory.Delete(root, true); }

        [Test]
        public void ReportOfTheLastRoundHidesTheIconUntilTheNextRoundOrBattle()
        {
            var report = new BattleReport(null, BoardPresentation.Load());
            var read = new List<string>();
            report.RoundRead += read.Add;
            report.Apply(View("battle-1", 2));
            Assert.IsTrue(Visible(report));
            report.Show(Report("battle-other"));
            Assert.IsTrue(Visible(report), "A report of another battle leaves the icon.");
            var unfinished = Report("battle-1");
            unfinished.Rounds[1].Completed = false;
            report.Show(unfinished);
            Assert.IsTrue(Visible(report), "A report without the completed last round leaves the icon.");
            report.Show(Report("battle-1"));
            Assert.IsFalse(Visible(report));
            report.Apply(View("battle-1", 2));
            Assert.IsFalse(Visible(report), "A later snapshot of the same round keeps the icon hidden.");
            report.Show(Report("battle-1"));
            Assert.AreEqual(new[] { Key(Match, "battle-1", 2) }, read, "Each round is read once.");
            report.Apply(View("battle-1", 3));
            Assert.IsTrue(Visible(report), "The next round of the battle is unread.");
            report.Apply(View("battle-2", 1));
            Assert.IsTrue(Visible(report), "A new battle is unread.");
        }

        [Test]
        public async Task ReadRoundStaysHiddenAfterARestartOfThisMatchOnly()
        {
            using var api = new GameApi("http://localhost");
            var store = await NotificationReadStore.Open(Files(Match));
            var first = new BattleReport(null, BoardPresentation.Load());
            using (new BattleReportController(api, Match, first, store))
            {
                first.Apply(View("battle-1", 2));
                first.Show(Report("battle-1"));
            }
            await store.Flushed;
            CollectionAssert.Contains(store.Steps, Key(Match, "battle-1", 2));

            var restarted = new BattleReport(null, BoardPresentation.Load());
            using (new BattleReportController(api, Match, restarted, await NotificationReadStore.Open(Files(Match))))
            {
                restarted.Apply(View("battle-1", 2));
                Assert.IsFalse(Visible(restarted), "The saved marker hides the read round from the first snapshot.");
                restarted.Apply(View("battle-1", 3));
                Assert.IsTrue(Visible(restarted));
            }

            var other = new BattleReport(null, BoardPresentation.Load());
            other.MarkRead(store.Steps);
            other.Apply(View("battle-1", 2, "other-match"));
            Assert.IsTrue(Visible(other), "The same battle ID in another match is another round.");
        }

        [Test]
        public void MarkersIgnoreOpponentStepIds()
        {
            var report = new BattleReport(null, BoardPresentation.Load());
            var view = View("battle-1", 2);
            report.MarkRead(new[] { "battle-1", view.LastBattleRound.Id + "/2" });
            report.Apply(view);
            Assert.IsTrue(Visible(report));
            report.Apply(new PlayerView { MatchId = Match });
            Assert.IsFalse(Visible(report), "No recorded round, no icon.");
        }

        private LocalMatchFiles Files(string match) => new LocalMatchFiles(root, "http://localhost/", "account", match);

        private static string Key(string match, string battle, int round) =>
            BattleReport.RoundKey(match, new BattleView { Id = battle, Round = round });

        private static bool Visible(BattleReport report) => report.Open.style.display.value == DisplayStyle.Flex;

        private static PlayerView View(string battle, int round, string match = Match) => new PlayerView
        {
            MatchId = match,
            LastBattleRound = new BattleView { Id = battle, RegionId = "lossarnach", Round = round, Stage = "RoundComplete" }
        };

        // The recorded two-round report of BattleReportContractTests, moved to this match and battle.
        private static BattleReportView Report(string battle)
        {
            var raw = File.ReadAllText(Path.Combine(Application.dataPath, "Wotr/Tests/Editor/BattleReportWire.json"));
            var report = JsonConvert.DeserializeObject<BattleReportView>(raw);
            report.MatchId = Match;
            report.Id = battle;
            return report;
        }
    }
}
