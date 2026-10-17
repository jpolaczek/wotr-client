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
