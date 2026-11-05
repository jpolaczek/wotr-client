using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-499: the local count of a ranked game's clock.
    public sealed class MoveClockCountdownTests
    {
        private static MoveClockView Clock(long remaining, string decision = "d1", Side owner = Side.Shadow, bool paused = false) => new MoveClockView
        {
            DecisionId = decision, Owner = owner, RemainingMilliseconds = remaining, LimitMilliseconds = 30000, WarningMilliseconds = 10000, Paused = paused
        };

        [Test]
        public void ItCountsDownInRealTimeAndStopsAtZero()
        {
            var countdown = new MoveClockCountdown();
            countdown.Sync(Clock(30000), 100);
            Assert.AreEqual(30, countdown.Remaining(100), 0.001);
            Assert.AreEqual(5, countdown.Remaining(125), 0.001);
            Assert.AreEqual(0, countdown.Remaining(200), 0.001);
        }

        [Test]
        public void AReSyncMovesTheDeadlineDownButNeverUpByASyncError()
        {
            var countdown = new MoveClockCountdown();
            countdown.Sync(Clock(30000), 100);
            countdown.Sync(Clock(29000), 101.5);
            Assert.AreEqual(28.5, countdown.Remaining(101.5), 0.001, "An earlier estimate than before: it comes down.");
            countdown.Sync(Clock(29000), 103);
            Assert.AreEqual(27, countdown.Remaining(103), 0.001, "A view that arrived late must not add time back.");
        }

        [Test]
        public void AChangeTooLargeToBeASyncErrorMovesItUp()
        {
            var countdown = new MoveClockCountdown();
            countdown.Sync(Clock(10000), 100);
            countdown.Sync(Clock(40000), 105);
            Assert.AreEqual(40, countdown.Remaining(105), 0.001, "A restart gave the downtime back (RANK-06).");
        }

        [Test]
        public void ANewDecisionStartsAfreshEvenWithMoreTimeThanASyncError()
        {
            var countdown = new MoveClockCountdown();
            countdown.Sync(Clock(5000), 100);
            countdown.Sync(Clock(7000, "d2", Side.FreePeoples), 101);
            Assert.AreEqual(7, countdown.Remaining(101), 0.001);
            Assert.AreEqual("d2", countdown.DecisionId);
            Assert.AreEqual(Side.FreePeoples, countdown.Owner);
        }

        [Test]
        public void TheWarningBeginsAtItsPoint()
        {
            var countdown = new MoveClockCountdown();
            countdown.Sync(Clock(30000), 100);
            Assert.IsFalse(countdown.InWarning(119.9));
            Assert.IsTrue(countdown.InWarning(120));
        }

        // RANK-07: the same time comes back after a pause, and the deadline follows from it.
        [Test]
        public void APausedClockStandsStillAndResumesWithItsTime()
        {
            var countdown = new MoveClockCountdown();
            countdown.Sync(Clock(20000), 100);
            countdown.Sync(Clock(18000, paused: true), 102);
            Assert.IsTrue(countdown.Paused);
            Assert.AreEqual(18, countdown.Remaining(102), 0.001);
            Assert.AreEqual(18, countdown.Remaining(500), 0.001);
            countdown.Sync(Clock(18000), 500);
            Assert.IsFalse(countdown.Paused);
            Assert.AreEqual(17, countdown.Remaining(501), 0.001);
        }

        [Test]
        public void NoClockIsNoCount()
        {
            var countdown = new MoveClockCountdown();
            countdown.Sync(Clock(5000), 100);
            countdown.Sync(null, 101);
            Assert.IsFalse(countdown.Active);
            Assert.IsFalse(countdown.InWarning(101));
        }

        [TestCase(0, "0:00")]
        [TestCase(0.2, "0:01")]
        [TestCase(1, "0:01")]
        [TestCase(27, "0:27")]
        [TestCase(59.5, "1:00")]
        [TestCase(161, "2:41")]
        [TestCase(-3, "0:00")]
        public void ItReadsWholeSecondsRoundedUp(double seconds, string text) =>
            Assert.AreEqual(text, MoveClockCountdown.Format(seconds));
    }
}
