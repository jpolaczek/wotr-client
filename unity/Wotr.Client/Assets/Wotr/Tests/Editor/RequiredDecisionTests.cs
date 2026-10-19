using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-254: a decision the game waits on raises a distinct notice and reaches the window that already handles it —
    // for excess units, the region panel of the region holding them, kept open while the decision waits.
    public sealed class RequiredDecisionTests
    {
        private static UnitView Unit(string id, string region) =>
            new UnitView { Id = id, Nation = "gondor", Kind = UnitKind.Regular, Side = Side.FreePeoples, Location = region };

        private static PlayerView Excess() => new PlayerView
        {
            MatchId = "match",
            You = Side.FreePeoples,
            DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = "excess", Kind = DecisionKind.RemoveExcess, RequiredCount = 2 },
            Units = { Unit("gondor-1", "osgiliath"), Unit("gondor-2", "osgiliath") },
            Options =
            {
                new ActionOption { Kind = CommandKind.RemoveExcess, Enabled = true, Minimum = 2, Maximum = 2, EligibleIds = { "gondor-1", "gondor-2" } }
            }
        };

        [Test]
        public void OnlyTheDecisionsTheGameWaitsOnAreRequired()
        {
            foreach (var kind in new[]
            {
                DecisionKind.RemoveExcess, DecisionKind.Losses, DecisionKind.HuntDamage, DecisionKind.DiscardCards,
                DecisionKind.SpecialDieChoice, DecisionKind.RulerDiscard, DecisionKind.CouncilVersions,
                DecisionKind.CouncilHomelands, DecisionKind.CouncilToken, DecisionKind.CorruptionTile
            })
                Assert.IsTrue(RequiredDecisions.Is(kind), kind.ToString());
            foreach (var kind in new[]
            {
                DecisionKind.Action, DecisionKind.ActionSequence, DecisionKind.CombatCard, DecisionKind.EffectChoice,
                DecisionKind.Reaction, DecisionKind.KeepActionDie, DecisionKind.Dismay
            })
                Assert.IsFalse(RequiredDecisions.Is(kind), kind.ToString());
            var view = Excess();
            Assert.IsTrue(RequiredDecisions.Waits(view));
            view.DecisionOwner = Side.Shadow;
            Assert.IsFalse(RequiredDecisions.Waits(view), "The opponent's decision is not this player's.");
        }

        [Test]
        public void ExcessOutsideABattleOpensTheRegionHoldingItsFigures()
        {
            var view = Excess();
            var attention = DecisionAttention.For(view);
            Assert.AreEqual(DecisionContext.Region, attention.Context);
            Assert.AreEqual("osgiliath", attention.Region);
            view.Battle = new BattleView { RegionId = "osgiliath", Stage = "Complete" };
            Assert.AreEqual(DecisionContext.Battle, DecisionAttention.For(view).Context, "Excess after a battle stays in the battle window.");
        }

        [Test]
        public void ACardsFigureChoiceOpensTheRegionHoldingItsFigures()
        {
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 1, Maximum = 1 };
            option.EffectChoices.Add(new EffectChoiceView { Id = "gondor-1", TargetId = "dol-amroth" });
            var view = new PlayerView
            {
                You = Side.FreePeoples,
                DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "folly", Kind = DecisionKind.EffectChoice, Presentation = "ArmyFigure" },
                Units = { Unit("gondor-1", "dol-amroth") },
                Options = { option }
            };
            var attention = DecisionAttention.For(view);
            Assert.AreEqual(DecisionContext.Region, attention.Context);
            Assert.AreEqual("dol-amroth", attention.Region);
            Assert.IsFalse(RequiredDecisions.Waits(view), "A card's choice keeps the plain notice.");
        }

        [Test]
        public void TheRequiredNoticeIsADistinctVariantThatGoesWithItsDecision()
        {
            var board = new BoardScreen();
            var view = Excess();
            board.Notice.Apply(view);
            Assert.IsTrue(board.Notice.Required);
            Assert.IsTrue(board.Notice.Button.ClassListContains(DecisionNotice.RequiredClass));
            Assert.AreEqual(DisplayStyle.Flex, board.Notice.Button.style.display.value);
            Assert.AreEqual("! Return 2 excess units", board.Notice.Button.text);
            view.Decision = new DecisionView { Id = "next", Kind = DecisionKind.Action };
            view.Options.Clear();
            board.Notice.Apply(view);
            Assert.IsFalse(board.Notice.Required, "An ordinary decision keeps the plain notice.");
            Assert.AreEqual("◎ Choose an action", board.Notice.Button.text);
            view.Decision = null;
            board.Notice.Apply(view);
            Assert.IsFalse(board.Notice.Required);
            Assert.AreEqual(DisplayStyle.None, board.Notice.Button.style.display.value, "The notice goes with its decision.");
        }

        [Test]
        public void TheDecisionPanelHoldsUntilItsDecisionIsResolved()
        {
            var board = new BoardScreen();
            var view = Excess();
            board.Hover.Apply(view);
            board.Hover.OpenDecision("osgiliath", view.Decision.Id);
            Assert.IsTrue(board.Hover.HoldsDecisionOf("excess"));
            Assert.AreEqual(DisplayStyle.Flex, board.Hover.Panel.Root.style.display.value);
            board.Hover.Open("osgiliath");
            Assert.IsFalse(board.Hover.HoldsDecision, "An ordinary open is an inspection.");
            board.Hover.OpenDecision("osgiliath", view.Decision.Id);
            view.Decision = new DecisionView { Id = "next", Kind = DecisionKind.Action };
            view.Options.Clear();
            board.Hover.Apply(view);
            Assert.IsFalse(board.Hover.HoldsDecision);
            Assert.AreEqual(DisplayStyle.None, board.Hover.Panel.Root.style.display.value, "The resolved decision takes its panel away.");
        }
    }
}
