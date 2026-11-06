using System.Collections.Generic;
using NUnit.Framework;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    // UI-509: a Hunt report's entries become each stage's last snapshot, per Hunt of the chain.
    public class HuntStageHistoryTests
    {
        private static HuntReportEntryView Entry(int hunt, HuntReportKind kind, HuntView view = null) =>
            new HuntReportEntryView { HuntId = hunt, Kind = kind, Hunt = view };

        private static HuntStageHistory History(params HuntReportEntryView[] entries) =>
            HuntStageHistory.From(new HuntReportView { Entries = new List<HuntReportEntryView>(entries) });

        [Test]
        public void EachEntryFallsOnItsStage()
        {
            var rolled = new HuntView();
            var rerolled = new HuntView();
            var drawn = new HuntView();
            var damage = new HuntView();
            var done = new HuntView();
            var history = History(Entry(1, HuntReportKind.Started, new HuntView()), Entry(1, HuntReportKind.Rolled, rolled),
                Entry(1, HuntReportKind.Rerolled, rerolled), Entry(1, HuntReportKind.TileDrawn, drawn),
                Entry(1, HuntReportKind.DamageDetermined, damage), Entry(1, HuntReportKind.HuntCompleted, done));
            Assert.AreSame(rolled, history.At(1, 0));
            Assert.AreSame(rerolled, history.At(1, 1));
            Assert.AreSame(drawn, history.At(1, 2));
            Assert.AreSame(damage, history.At(1, 3));
            Assert.AreSame(done, history.At(1, 4));
        }

        [Test]
        public void AnEntryWithNoSnapshotLeavesTheLastOneStanding()
        {
            var damage = new HuntView();
            var history = History(Entry(1, HuntReportKind.DamageDetermined, damage), Entry(1, HuntReportKind.Corruption), Entry(1, HuntReportKind.Progressed));
            Assert.AreSame(damage, history.At(1, 4));
        }

        [Test]
        public void NoRerollOfferedMeansNoRerollEntryAndTheRollStillStands()
        {
            var rolled = new HuntView();
            var history = History(Entry(1, HuntReportKind.Rolled, rolled));
            Assert.IsNull(history.At(1, 1));
            Assert.AreSame(rolled, history.Nearest(1, 1));
            Assert.IsNull(history.Nearest(1, 0) == rolled ? null : rolled);
        }

        [Test]
        public void AChainKeepsItsHuntsApartAndNoSnapshotCrossesFromOneToTheNext()
        {
            var first = new HuntView();
            var second = new HuntView();
            var history = History(Entry(1, HuntReportKind.Rolled, first), Entry(2, HuntReportKind.Corruption), Entry(2, HuntReportKind.Rolled, second));
            CollectionAssert.AreEqual(new[] { 1, 2 }, history.Hunts);
            Assert.AreSame(first, history.At(1, 0));
            Assert.AreSame(second, history.At(2, 0));
            Assert.IsNull(history.At(2, 3), "The chain's second Hunt had nothing to show when corruption changed.");
        }

        [Test]
        public void InMordorTheStagesAreTileDamageAndStep()
        {
            var mordor = new HuntView { Mordor = true };
            Assert.AreEqual(-1, HuntStageHistory.StageOf(HuntReportKind.Rolled, true));
            Assert.AreEqual(0, HuntStageHistory.StageOf(HuntReportKind.TileDrawn, true));
            Assert.AreEqual(1, HuntStageHistory.StageOf(HuntReportKind.DamageDetermined, true));
            Assert.AreEqual(2, HuntStageHistory.StageOf(HuntReportKind.Progressed, true));
            var history = History(Entry(1, HuntReportKind.TileDrawn, mordor), Entry(1, HuntReportKind.HuntCompleted, mordor));
            Assert.AreSame(mordor, history.At(1, 0));
            Assert.AreSame(mordor, history.At(1, 2));
        }

        [Test]
        public void NoReportHasNoStages()
        {
            var history = HuntStageHistory.From(null);
            Assert.IsEmpty(history.Hunts);
            Assert.IsNull(history.Nearest(1, 3));
        }
    }
}
