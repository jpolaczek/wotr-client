using System.Collections.Generic;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.EditorTests.DraftReconciliationTests;

namespace Wotr.EditorTests
{
    public class DraftRouteReconciliationTests
    {
        [Test]
        public void MovingGroupFocusFollowsTheSameArmyWhenAnEarlierSourceDisappears()
        {
            var view = View(CommandKind.MoveArmies);
            var draft = Armies(view);
            ArmyDraft.Edit(draft, 1);
            Assert.IsFalse(draft.Reconcile(view));
            view.Options[0].Armies.RemoveAt(0);
            Assert.IsTrue(draft.Reconcile(view));
            Assert.AreEqual(0, draft.MoveFocus);
            Assert.AreEqual("second", ArmyDraft.Current(draft).From);
            Assert.AreEqual("target", ArmyDraft.Current(draft).To);
        }

        [TestCase(CommandKind.MoveArmies)]
        [TestCase(CommandKind.AttackArmy)]
        public void ChangingTheUseOfTheSameDieRequiresNewCostSelectionButKeepsTheMove(CommandKind kind)
        {
            var view = View(kind);
            var draft = Armies(view);
            draft.Payload.Moves.RemoveAt(1);
            draft.Payload.DieId = "character-die";
            draft.Payload.MovementCost = DieFace.Army;
            draft.Payload.AbilityId = "mouth-command";
            view.Options[0].EligibleDice.Add("character-die");
            view.Options[0].MovementCosts.Add(new MovementCostOption
            {
                DieId = "character-die", Face = DieFace.Character, MaximumMoves = 1
            });
            Assert.IsTrue(draft.Reconcile(view));
            Assert.IsEmpty(draft.Payload.DieId);
            Assert.IsEmpty(draft.Payload.AbilityId);
            Assert.AreEqual(1, draft.Payload.Moves.Count);
            Assert.AreEqual("target", draft.Payload.Moves[0].To);
        }

        [Test]
        public void EventMarchKeepsItsExactPrefixAndNeverReplacesAnInvalidRoute()
        {
            var view = View(CommandKind.MoveArmies);
            var option = view.Options[0];
            option.ArmyEvent = new ArmyEventOption { RouteMovement = true, SeparateRoutes = true };
            var draft = Armies(view);
            var move = draft.Payload.Moves[0];
            move.To = "";
            move.Route.Add("pass");
            option.Armies[0].Targets[0].Route.AddRange(new[] { "pass", "target" });
            option.Armies[1].Targets[0].Route.Add("target");
            draft.Payload.Moves[1].Route.Add("target");
            ArmyDraft.Edit(draft, 0);
            Assert.IsFalse(draft.Reconcile(view));
            CollectionAssert.AreEqual(new[] { "pass" }, move.Route);

            option.Armies[0].Targets[0].Route = new List<string> { "other-pass", "target" };
            Assert.IsTrue(draft.Reconcile(view));
            Assert.IsEmpty(move.Route);
            Assert.IsEmpty(move.To);
            CollectionAssert.AreEqual(new[] { "target" }, draft.Payload.Moves[1].Route);
            Assert.AreEqual("target", draft.Payload.Moves[1].To);
        }

        [Test]
        public void FellowshipKeepsAnIncompleteRouteThroughAnIllegalDestinationButHonorsNewBlockers()
        {
            var view = View(CommandKind.RevealFellowship);
            var travel = new TravelOption { Origin = "origin", Range = 3, Targets = { "finish" } };
            view.Options[0].Travel.Add(travel);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.RevealFellowship);
            draft.Payload.Route.AddRange(new[] { "origin", "pass" });
            Assert.IsFalse(draft.Reconcile(view));
            Assert.IsFalse(draft.Ready(view.Options[0]));
            CollectionAssert.AreEqual(new[] { "origin", "pass" }, draft.Payload.Route);

            travel.BlockedRegions.Add("pass");
            Assert.IsTrue(draft.Reconcile(view));
            Assert.IsEmpty(draft.Payload.Route);
        }

        [Test]
        public void ANewStopInsideACompletedRouteInvalidatesTheRouteWithoutShorteningItsDestination()
        {
            var view = View(CommandKind.DeclareFellowship);
            var travel = new TravelOption { Origin = "origin", Range = 3, Targets = { "pass", "finish" } };
            view.Options[0].Travel.Add(travel);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.DeclareFellowship);
            draft.Payload.Route.AddRange(new[] { "origin", "pass", "finish" });
            Assert.IsFalse(draft.Reconcile(view));
            travel.StopRegions.Add("pass");
            Assert.IsTrue(draft.Reconcile(view));
            Assert.IsEmpty(draft.Payload.Route);
        }

        [TestCase(CommandKind.MoveCompanions)]
        [TestCase(CommandKind.MoveShadowCharacters)]
        public void CompanionFocusRemainsWithTheSelectedGroupAfterRemovingAnEarlierGroup(CommandKind kind)
        {
            var view = View(kind);
            var draft = new TurnDraft();
            draft.Choose(kind);
            foreach (var id in new[] { "first", "second", "third" })
            {
                view.Options[0].EligibleIds.Add(id);
                view.Options[0].Travel.Add(new TravelOption { Id = id, Origin = "origin", Range = 1, Targets = { "target" } });
                draft.Payload.CompanionMoves.Add(new CompanionMoveChoice { Ids = { id }, Route = { "origin", "target" } });
            }
            TravelDraft.SelectGroup(draft, 1);
            Assert.IsFalse(draft.Reconcile(view));
            view.Options[0].EligibleIds.Remove("first");
            Assert.IsTrue(draft.Reconcile(view));
            Assert.AreEqual(0, draft.CompanionFocus);
            Assert.AreEqual("second", TravelDraft.Group(draft).Ids[0]);
        }

        private static TurnDraft Armies(PlayerView view)
        {
            var draft = new TurnDraft();
            draft.Choose(view.Options[0].Kind);
            foreach (var origin in new[] { "first", "second" })
            {
                var unit = origin + "-unit";
                view.Options[0].Armies.Add(new ArmyOption
                {
                    RegionId = origin, UnitIds = { unit },
                    Targets = { new ArmyTargetOption { RegionId = "target", EligibleIds = { unit } } }
                });
                draft.Payload.Moves.Add(new ArmyMoveChoice { From = origin, To = "target", UnitIds = { unit } });
            }
            return draft;
        }
    }
}
