using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class ShadowMarchDraftTests
    {
        [Test]
        public void TwoArmyRoutesStaySeparateAndUndoCopiesBothPaths()
        {
            var view = View(2);
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, CommandKind.MoveArmies);
            Assert.IsTrue(BoardDraft.SelectRegion(draft, view, "a"));
            Assert.IsTrue(BoardDraft.SelectRegion(draft, view, "x"));
            Assert.IsFalse(draft.Ready(view.Options[0]));
            Assert.IsTrue(BoardDraft.SelectRegion(draft, view, "b"));
            Assert.IsFalse(draft.Ready(view.Options[0]));
            Assert.IsTrue(BoardDraft.SelectRegion(draft, view, "b"));
            Assert.IsTrue(BoardDraft.SelectRegion(draft, view, "x"));
            Assert.IsTrue(BoardDraft.SelectRegion(draft, view, "a"));
            Assert.IsTrue(draft.Ready(view.Options[0]));
            CollectionAssert.AreEqual(new[] { "x", "b" }, draft.Payload.Moves[0].Route);
            CollectionAssert.AreEqual(new[] { "x", "a" }, draft.Payload.Moves[1].Route);
            Assert.IsEmpty(draft.Payload.Route);
            ArmyDraft.Edit(draft, 0);
            EventArmyDraft.ResetRoute(draft);
            Assert.IsFalse(draft.Ready(view.Options[0]));
            draft.Back();
            Assert.IsTrue(draft.Ready(view.Options[0]));
            CollectionAssert.AreEqual(new[] { "x", "b" }, draft.Payload.Moves[0].Route);
            CollectionAssert.AreEqual(new[] { "x", "a" }, draft.Payload.Moves[1].Route);
        }

        [Test]
        public void FourArmyPlanCanEditOneGroupAndReconcileWithoutLosingOthers()
        {
            var view = View(4);
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, CommandKind.MoveArmies);
            foreach (var army in view.Options[0].Armies)
            {
                Assert.IsTrue(BoardDraft.SelectRegion(draft, view, army.RegionId));
                Assert.IsTrue(BoardDraft.SelectRegion(draft, view, "x"));
            }
            Assert.IsTrue(draft.Ready(view.Options[0]));
            Assert.AreEqual(4, draft.Payload.Moves.Count);
            ArmyDraft.Edit(draft, 1);
            EventArmyDraft.Toggle(draft, new[] { "b-unit" });
            Assert.IsFalse(draft.Ready(view.Options[0]));
            Assert.IsEmpty(draft.Payload.Moves[1].Route);
            Assert.AreEqual(1, draft.Payload.Moves[0].Route.Count);
            draft.Back();
            draft.Reconcile(view);
            Assert.IsTrue(draft.Ready(view.Options[0]));
            Assert.AreEqual(4, draft.Payload.Moves.Count);
            Assert.IsFalse(ArmyDraft.Start(draft, view, "a"));
        }

        [Test]
        public void PrefixIsHighlightedButCannotBeCommittedBeforeAValidFinalRegion()
        {
            var view = View(2);
            view.Options[0].Minimum = 1;
            var draft = new TurnDraft();
            draft.Choose(CommandKind.MoveArmies);
            Assert.IsTrue(ArmyDraft.Start(draft, view, "a"));
            Assert.IsTrue(ArmyDraft.Target(draft, view, "x"));
            Assert.IsFalse(draft.Ready(view.Options[0]));
            CollectionAssert.AreEqual(new[] { "b" }, ArmyDraft.Targets(draft, view));
            Assert.IsTrue(ArmyDraft.Target(draft, view, "b"));
            Assert.IsTrue(draft.Ready(view.Options[0]));
        }

        private static PlayerView View(int count)
        {
            var option = new ActionOption
            {
                Kind = CommandKind.MoveArmies,
                Minimum = count == 2 ? 2 : 1,
                Maximum = count,
                ArmyEvent = new ArmyEventOption { RouteMovement = true, SeparateRoutes = true, MaximumSteps = count == 2 ? 2 : 1 }
            };
            var view = new PlayerView { You = Side.Shadow, Decision = new DecisionView { Kind = DecisionKind.ArmyEvent }, Options = { option } };
            foreach (var region in new[] { "a", "b", "c", "d" }.Take(count))
            {
                var target = count == 2 ? region == "a" ? "b" : "a" : "x";
                var route = count == 2 ? new[] { "x", target } : new[] { target };
                var army = new ArmyOption
                {
                    RegionId = region,
                    UnitIds = { region + "-unit" },
                    Targets = { new ArmyTargetOption
                    { RegionId = target, Route = route.ToList(), EligibleIds = { region + "-unit" } } }
                };
                option.Armies.Add(army);
                option.Targets.Add(target);
                view.Units.Add(new UnitView { Id = region + "-unit", Location = region, Side = Side.Shadow, Kind = UnitKind.Regular });
            }
            return view;
        }
    }
}
