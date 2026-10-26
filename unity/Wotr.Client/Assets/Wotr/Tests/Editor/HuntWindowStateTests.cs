using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-162: the Hunt window opens once per Hunt and its stage ribbon follows the server's Hunt stage.
    public sealed class HuntWindowStateTests
    {
        private static PlayerView View(HuntView hunt) => new PlayerView
        {
            MatchId = "m",
            Revision = 7,
            Hunt = hunt,
            HuntReport = new HuntReportView { Id = "report", StartedRevision = 5 }
        };

        [Test]
        public void OneKeyPerHuntKeepsTheWindowFromReopeningWithinIt()
        {
            Assert.AreEqual("", HuntWindowState.Key(new PlayerView { MatchId = "m" }), "No Hunt, no key.");
            var view = View(new HuntView { SourceId = "fellowship", Stage = "Roll" });
            var key = HuntWindowState.Key(view);
            view.Revision = 9;
            view.Hunt.Stage = "Draw";
            Assert.AreEqual(key, HuntWindowState.Key(view), "Later stages of the same Hunt keep its key.");
            view.HuntReport = new HuntReportView { Id = "report-2", StartedRevision = 9 };
            Assert.AreNotEqual(key, HuntWindowState.Key(view), "The next Hunt opens the window again.");
            // UI-343: a report records a chain; its second Hunt is a new Hunt for the window.
            var first = Entry(1, HuntReportKind.Started);
            view.HuntReport.Entries.Add(first);
            var chainKey = HuntWindowState.Key(view);
            view.HuntReport.Entries.Add(Entry(1, HuntReportKind.Rolled));
            Assert.AreEqual(chainKey, HuntWindowState.Key(view), "The same Hunt keeps its key through its entries.");
            view.HuntReport.Entries.Add(Entry(2, HuntReportKind.Started));
            Assert.AreNotEqual(chainKey, HuntWindowState.Key(view), "The chain's next Hunt is a new Hunt.");
            view.HuntReport.StartedRevision = 11;
            var staged = HuntWindowState.Key(view);
            view.HuntReport.StartedRevision = 9;
            Assert.AreEqual(staged, HuntWindowState.Key(view), "A presented report keys the same as the live one.");
        }

        private static HuntReportEntryView Entry(int hunt, HuntReportKind kind) => new HuntReportEntryView { HuntId = hunt, Kind = kind };

        [Test]
        public void TheRibbonFollowsTheRollTheTileAndTheDamage()
        {
            var hunt = new HuntView { Stage = "Roll", Dice = new List<HuntDieView> { new HuntDieView() } };
            var view = View(hunt);
            CollectionAssert.AreEqual(new[] { "roll", "reroll", "tile", "damage", "result" }, HuntWindowState.Stages(hunt).Select(s => s.Key));
            Assert.AreEqual(0, HuntWindowState.Index(view, hunt));
            view.Decision = new DecisionView { Kind = DecisionKind.HuntReroll };
            Assert.AreEqual(1, HuntWindowState.Index(view, hunt));
            view.Decision = null;
            hunt.Stage = "Draw";
            Assert.AreEqual(2, HuntWindowState.Index(view, hunt));
            hunt.Stage = "Damage";
            Assert.AreEqual(3, HuntWindowState.Index(view, hunt));
            hunt.Stage = "Done";
            Assert.AreEqual(4, HuntWindowState.Index(view, hunt));
            // Once the Hunt is over the window keeps its last state at the Result stage.
            hunt.Stage = "Roll";
            view.Hunt = null;
            view.HuntReport.Completed = true;
            Assert.AreEqual(4, HuntWindowState.Index(view, hunt));
        }

        // UI-340: the stages a presented Fellowship move records (wotr-server HuntRuntime): its start before its stage is set,
        // the roll, the drawn tile with its damage pending, the damage, the table cards' window — never back.
        [Test]
        public void ARecordedFellowshipMoveGoesForwardFromRoll()
        {
            var hunt = new HuntView { SourceId = HuntWindowState.MoveSource, Stage = "BeforeDraw" };
            var view = View(hunt);
            Assert.AreEqual(0, HuntWindowState.Index(view, hunt), "Started: Roll, not Tile.");
            hunt.Dice = new List<HuntDieView> { new HuntDieView { Result = 6, Success = true }, new HuntDieView { Result = 2 } };
            Assert.AreEqual(0, HuntWindowState.Index(view, hunt), "Rolled: Roll while the dice show.");
            hunt.Dice[1].Rerolled = true;
            Assert.AreEqual(1, HuntWindowState.Index(view, hunt), "Rerolled: Re-roll.");
            hunt.Stage = "Draw";
            Assert.AreEqual(2, HuntWindowState.Index(view, hunt), "The draw's window: Tile.");
            hunt.Stage = "DetermineDamage";
            hunt.TileId = "base.hunt.standard.1.1.1";
            hunt.DamagePending = true;
            Assert.AreEqual(2, HuntWindowState.Index(view, hunt), "Tile drawn: Tile while its damage is pending.");
            hunt.Stage = "Table";
            hunt.DamagePending = false;
            Assert.AreEqual(3, HuntWindowState.Index(view, hunt), "Damage determined, table cards' window: Damage, not back to Tile.");
            hunt.Stage = "Guide";
            Assert.AreEqual(3, HuntWindowState.Index(view, hunt));
        }

        [Test]
        public void ACardsHuntShowsNoStageUntilItRollsOrDraws()
        {
            var hunt = new HuntView { SourceId = "base.sh.character.05", Stage = "BeforeDraw" };
            var view = View(hunt);
            Assert.AreEqual(-1, HuntWindowState.Index(view, hunt), "Started: neither Roll nor Tile yet.");
            hunt.Stage = "Draw";
            Assert.AreEqual(2, HuntWindowState.Index(view, hunt), "A tile-only Hunt goes to Tile.");
        }

        [Test]
        public void AHuntInMordorHasNoRoll()
        {
            var hunt = new HuntView { Mordor = true, Stage = "Draw" };
            var view = View(hunt);
            CollectionAssert.AreEqual(new[] { "tile", "damage", "step" }, HuntWindowState.Stages(hunt).Select(s => s.Key));
            Assert.AreEqual(0, HuntWindowState.Index(view, hunt));
            hunt.Stage = "Corruption";
            Assert.AreEqual(1, HuntWindowState.Index(view, hunt));
            hunt.Stage = "Reveal";
            Assert.AreEqual(2, HuntWindowState.Index(view, hunt));
        }
    }
}
