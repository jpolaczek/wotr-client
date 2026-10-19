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

        // UI-264: every decision kind has a host; only a card's step (by presentation) may still fall back to the window.
        [Test]
        public void EveryDecisionKindHasAnObjectHost()
        {
            foreach (DecisionKind kind in System.Enum.GetValues(typeof(DecisionKind)))
            {
                if (kind is DecisionKind.EffectChoice or DecisionKind.CharacterEvent or DecisionKind.Reaction)
                    continue;
                Assert.AreNotEqual(DecisionHost.Window, DecisionRoutes.Of(Own(kind, "")), kind.ToString());
            }
        }

        // UI-264: the special-die pick and the Ruler discard live in the dice pool. The rest are routed to the expansion
        // panels that host them until their tasks move them onto objects.
        [TestCase(DecisionKind.SpecialDieChoice, DecisionHost.Dice)]
        [TestCase(DecisionKind.RulerDiscard, DecisionHost.Dice)]
        [TestCase(DecisionKind.KeepActionDie, DecisionHost.Resources)]  // UI-268: Vilya socket
        [TestCase(DecisionKind.Dismay, DecisionHost.Resources)]         // UI-276: glowing Ruler dice
        [TestCase(DecisionKind.CouncilToken, DecisionHost.Resources)]   // UI-270: Shadow pool token slots
        [TestCase(DecisionKind.CouncilVersions, DecisionHost.Setup)]    // UI-269: Fellowship portraits
        [TestCase(DecisionKind.CouncilHomelands, DecisionHost.Setup)]   // UI-270: Fellowship portraits and regions
        [TestCase(DecisionKind.CorruptionTile, DecisionHost.Sovereigns)] // UI-274: beside the sovereign's portrait
        public void ExpansionDecisionsHaveTheirHosts(DecisionKind kind, DecisionHost host) =>
            Assert.AreEqual(host, DecisionRoutes.Of(Own(kind, "")));

        // UI-264: the expansions' card steps: a sovereign is chosen at the sovereigns, a two-way mode is a console
        // button; a Sequential corruption draw is a CorruptionTile decision and goes by its kind.
        [TestCase(DecisionKind.EffectChoice, "Sovereign", DecisionHost.Sovereigns)]
        [TestCase(DecisionKind.EffectChoice, "SovereignCardBranch", DecisionHost.Console)]
        [TestCase(DecisionKind.EffectChoice, "EffectTargets", DecisionHost.Console)]
        [TestCase(DecisionKind.CorruptionTile, "CorruptionSequential", DecisionHost.Sovereigns)]
        public void ExpansionCardStepsHaveObjectHosts(DecisionKind kind, string presentation, DecisionHost host) =>
            Assert.AreEqual(host, DecisionRoutes.Of(Own(kind, presentation)));

        private static PlayerView Own(DecisionKind kind, string presentation) => new PlayerView
        {
            You = Side.Shadow, DecisionOwner = Side.Shadow,
            Decision = new DecisionView { Id = "d", Kind = kind, Presentation = presentation }
        };

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
