using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class DecisionAttentionTests
    {
        [Test]
        public void OpponentsPrivateDecisionNeverCreatesANoticeEvenWhenPublicContextExists()
        {
            var view = new PlayerView { You = Side.Shadow, DecisionOwner = Side.FreePeoples,
                Battle = new BattleView { RegionId = "moria" }, Decision = new DecisionView { Kind = DecisionKind.CouncilVersions } };
            Assert.IsNull(DecisionAttention.For(view));
        }

        [Test]
        public void CandidateRegionsDoNotInventALocationForAnUnchosenAction()
        {
            var view = new PlayerView { You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Kind = DecisionKind.Action } };
            view.Options.Add(new ActionOption { Kind = CommandKind.MoveArmies, Targets = { "moria", "hollin" } });
            Assert.IsEmpty(DecisionAttention.For(view).Region);
            Assert.AreEqual(DecisionContext.Resources, DecisionAttention.For(view).Context);
        }

        // UI-395 (owner): Nenya's socket and Pass answer the Hunt's Eye in the console, so the Hunt window holds nothing and
        // no window or region is the decision's; a table card's response beside it keeps the Hunt window.
        [Test]
        public void ANenyaResponseLeadsToNoWindow()
        {
            var view = new PlayerView { You = Side.FreePeoples, DecisionOwner = Side.FreePeoples, Hunt = new HuntView(),
                Decision = new DecisionView { Kind = DecisionKind.Reaction, SourceId = "lords.character.galadriel" } };
            view.Options.Add(new ActionOption { Kind = CommandKind.RespondEffect, EligibleIds = { NamedRings.NenyaReaction } });
            view.Options.Add(new ActionOption { Kind = CommandKind.DeclineReaction });
            Assert.IsTrue(HuntActions.ConsoleReaction(view));
            Assert.IsFalse(HuntActions.UsesPanel(view), "The Hunt window holds nothing to choose.");
            Assert.AreEqual(DecisionContext.Resources, DecisionAttention.For(view).Context);
            Assert.IsEmpty(DecisionAttention.For(view).Region);
            view.Options[0].EligibleIds.Add("card.mithril-coat");
            Assert.IsFalse(HuntActions.ConsoleReaction(view));
            Assert.IsTrue(HuntActions.UsesPanel(view), "Mithril's response stands in the Hunt window.");
        }

        [Test]
        public void MordorDecisionOpensTheTrackWithoutNavigatingToItsOldMapRegion()
        {
            var view = new PlayerView { You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Kind = DecisionKind.FellowshipRoute } };
            view.Fellowship.RegionId = "morannon";
            view.Fellowship.MordorStep = 2;
            Assert.AreEqual(DecisionContext.Fellowship, DecisionAttention.For(view).Context);
            Assert.IsEmpty(DecisionAttention.For(view).Region);
        }

        [Test]
        public void NestedLossDecisionPointsToTheCurrentLossRegion()
        {
            var view = new PlayerView { You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Kind = DecisionKind.Losses },
                Battle = new BattleView { RegionId = "moria" }, EventLoss = new EventLossView { RegionId = "hollin" } };
            Assert.AreEqual("hollin", DecisionAttention.For(view).Region);
        }
    }
}
