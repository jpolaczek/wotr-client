using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-244: the player's own decision is routed to the object host the base coverage matrix gives it.
    public sealed class DecisionRoutesTests
    {
        [TestCase(DecisionKind.Action, "", DecisionHost.Map)]
        [TestCase(DecisionKind.Guide, "", DecisionHost.Fellowship)]
        [TestCase(DecisionKind.FellowshipPhase, "", DecisionHost.Fellowship)]
        [TestCase(DecisionKind.RemoveExcess, "", DecisionHost.RegionPanel)]
        [TestCase(DecisionKind.EffectChoice, "Region", DecisionHost.Map)]
        [TestCase(DecisionKind.EffectChoice, "ArmyFigures", DecisionHost.RegionPanel)]
        [TestCase(DecisionKind.EffectChoice, "TableCards", DecisionHost.Effects)]
        [TestCase(DecisionKind.EffectChoice, "ActionDie", DecisionHost.Dice)]
        [TestCase(DecisionKind.EffectChoice, "CaptainOrder", DecisionHost.Console)]
        [TestCase(DecisionKind.EffectChoice, "FellowshipCompanions", DecisionHost.Fellowship)]
        [TestCase(DecisionKind.EffectChoice, "Unmigrated", DecisionHost.Window)]
        public void TheOwnDecisionGoesToItsObjectHost(DecisionKind kind, string presentation, DecisionHost host)
        {
            var view = new PlayerView
            {
                You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "d", Kind = kind, Presentation = presentation }
            };
            Assert.AreEqual(host, DecisionRoutes.Of(view));
        }

        [Test]
        public void BattleChoicesStayInTheBattleAndTheOpponentsDecisionHasNoHost()
        {
            var view = new PlayerView
            {
                You = Side.Shadow, DecisionOwner = Side.Shadow, Battle = new BattleView(),
                Decision = new DecisionView { Id = "d", Kind = DecisionKind.EffectChoice, Presentation = "CharacterLeadership" }
            };
            Assert.AreEqual(DecisionHost.Battle, DecisionRoutes.Of(view));
            view.Decision.Presentation = "Unmigrated";
            Assert.AreEqual(DecisionHost.Battle, DecisionRoutes.Of(view), "A battle's unmigrated choice stays in its window.");
            view.DecisionOwner = Side.FreePeoples;
            Assert.AreEqual(DecisionHost.None, DecisionRoutes.Of(view));
        }
    }
}
