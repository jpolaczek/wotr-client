using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.Tests
{
    public sealed class HuntPresentationTests
    {
        [Test]
        public void ZeroDiceRequiresAKnownRollInThisHunt()
        {
            var empty = new HuntView();
            var view = new PlayerView { HuntReport = new HuntReportView { Completed = true,
                Entries = { new HuntReportEntryView { HuntId = 1, Kind = HuntReportKind.Rolled, Hunt = empty } } } };
            Assert.AreEqual(HuntFocus.Roll, HuntPresentation.Focus(view));
            view.Hunt = new HuntView();
            view.HuntReport.Entries.Add(new HuntReportEntryView { HuntId = 2, Kind = HuntReportKind.Started, Hunt = view.Hunt });
            Assert.AreEqual(HuntFocus.None, HuntPresentation.Focus(view), "An earlier roll must not fabricate the next result.");
            view.Hunt.Mordor = true;
            view.HuntReport.Entries.Add(new HuntReportEntryView { HuntId = 2, Kind = HuntReportKind.Rolled, Hunt = view.Hunt });
            Assert.AreEqual(HuntFocus.None, HuntPresentation.Focus(view));
        }

        [Test]
        public void AllocationNeverUsesThePreviousHuntAsItsMainObject()
        {
            var view = new PlayerView { Phase = GamePhase.HuntAllocation, You = Side.FreePeoples,
                DecisionOwner = Side.Shadow, HuntReport = new HuntReportView { Completed = true,
                    Entries = { new HuntReportEntryView { Hunt = new HuntView { TileId = "previous-tile" } } } } };
            Assert.AreEqual("Hunt allocation", HuntPresentation.Title(view));
            Assert.AreEqual("Allocate dice", HuntPresentation.Activity(view));
            Assert.AreEqual("Waiting for Shadow", HuntPresentation.Owner(view));
            Assert.AreEqual(HuntFocus.Allocation, HuntPresentation.Focus(view));
        }

        [Test]
        public void AContinuationStageDoesNotPretendToBeTheOpponentsDecision()
        {
            var view = new PlayerView { You = Side.FreePeoples, DecisionOwner = Side.Shadow,
                Hunt = new HuntView { Stage = "BeforeDraw", Dice = { new HuntDieView { Result = 1 } } } };
            Assert.AreEqual("Hunt roll", HuntPresentation.Activity(view));
            Assert.AreEqual("Waiting for Shadow", HuntPresentation.Owner(view));
            Assert.AreEqual(HuntFocus.Roll, HuntPresentation.Focus(view));
            view.You = Side.Shadow;
            view.Decision = new DecisionView { Kind = DecisionKind.HuntReroll };
            Assert.AreEqual("Choose rerolls", HuntPresentation.Activity(view));
            Assert.AreEqual("Your decision · Shadow", HuntPresentation.Owner(view));
        }

        [Test]
        public void GuideChoiceTakesPriorityOverTheTileAndKnownRoll()
        {
            var view = new PlayerView { You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
                Hunt = new HuntView { TileId = "tile", Dice = { new HuntDieView { Result = 6 } } },
                Decision = new DecisionView { Kind = DecisionKind.Guide } };
            Assert.AreEqual(HuntFocus.Guide, HuntPresentation.Focus(view));
            view.Decision.Kind = DecisionKind.HuntDamage;
            Assert.AreEqual(HuntFocus.Tile, HuntPresentation.Focus(view));
        }

        [Test]
        public void ALastReportNeverClaimsTheCurrentUnrelatedDecision()
        {
            var view = new PlayerView { You = Side.Shadow, DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Kind = DecisionKind.Action } };
            Assert.AreEqual("No Hunt report yet", HuntPresentation.Activity(view));
            Assert.AreEqual(HuntFocus.None, HuntPresentation.Focus(view));
            view.HuntReport = new HuntReportView { Completed = true };
            Assert.AreEqual("Last Hunt", HuntPresentation.Title(view));
            Assert.AreEqual("Complete", HuntPresentation.Activity(view));
            Assert.IsEmpty(HuntPresentation.Owner(view));
        }
    }
}
