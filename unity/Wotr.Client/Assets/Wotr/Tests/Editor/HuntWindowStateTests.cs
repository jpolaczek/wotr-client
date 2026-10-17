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
        }

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
