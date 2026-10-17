using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.Client.Tests
{
    public sealed class BattleWindowTests
    {
        [Test]
        public void ClosingAndRepeatedProjectionKeepTheWindowClosedAndNewBattleOpensIt()
        {
            var board = new BoardScreen();
            var view = BattleScreenFixture.Create(CommandKind.CommitCombatCard);
            board.Apply(view);
            Assert.IsTrue(board.Battles.IsOpen, "UI-149: a new battle opens its window.");
            board.Battles.Close();
            board.Battles.Preview();
            Assert.IsTrue(board.Battles.IsOpen);
            Assert.IsFalse(board.Battles.Pinned);
            board.Battles.Open();
            board.Battles.CloseTransient();
            Assert.IsTrue(board.Battles.IsOpen);
            board.Battles.Close();
            board.Battles.Apply(view);
            Assert.IsFalse(board.Battles.IsOpen);
            view.Battle.Id = "another";
            board.Battles.Apply(view);
            Assert.IsTrue(board.Battles.IsOpen, "The next battle opens its window again.");
            Assert.IsFalse(board.Battles.SelectRegion("rivendell"));
            Assert.IsTrue(board.Battles.SelectRegion(view.Battle.RegionId));
        }

        [Test]
        public void NonparticipantsRemainDistinctFromCombatStrengthAndLeadership()
        {
            var view = BattleScreenFixture.Create(CommandKind.AdvanceBattle, mode: BattleMode.Relief);
            var force = view.Battle.Attacker;
            view.Units.Add(new UnitView { Id = "rear", Nation = "rohan", Kind = UnitKind.Regular, Side = force.Side, Location = force.RegionId });
            view.Units.Add(new UnitView { Id = "garrison", Nation = "gondor", Kind = UnitKind.Elite, Side = force.Side,
                Location = view.Battle.RegionId, Garrison = true });
            var strength = force.Strength;
            var text = BattleForceText.Nonparticipants(force, view.Battle, view);
            StringAssert.Contains("Rear guard", text);
            StringAssert.Contains("Rohan", text);
            StringAssert.Contains("Garrison · not fighting", text);
            Assert.AreEqual(strength, force.Strength);
            Assert.IsFalse(force.ParticipantIds.Contains("rear"));
            Assert.IsFalse(force.ParticipantIds.Contains("garrison"));
        }

        [Test]
        public void CombatChoicesExistOnlyForTheDecisionOwnerAndUseTheSameDraftTokens()
        {
            var view = BattleScreenFixture.Create(CommandKind.CommitCombatCard);
            view.DecisionOwner = view.You;
            var option = view.Options.Single(o => o.Kind == CommandKind.CommitCombatCard);
            var choices = new BattleCombatHand(view, option);
            CollectionAssert.AreEquivalent(option.EligibleIds, choices.Root.Query<Button>().ToList().Select(b => ((DraftToken)b.userData).Id));
            view.DecisionOwner = view.You == Side.FreePeoples ? Side.Shadow : Side.FreePeoples;
            Assert.AreEqual(0, new BattleCombatHand(view, option).Root.childCount);
            Assert.AreEqual(-1, BattleStages.Index("future-stage"));
            Assert.AreEqual(2, BattleStages.Index("DefenderReroll"));
            Assert.AreEqual(3, BattleStages.Index("AttackerLosses"));
        }
    }
}
