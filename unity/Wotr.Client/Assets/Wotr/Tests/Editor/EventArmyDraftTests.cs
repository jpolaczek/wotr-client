using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class EventArmyDraftTests
    {
        [Test]
        public void RouteIsExplicitAndUndoKeepsArmyGroupWithoutExtraDie()
        {
            var view = View();
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, CommandKind.MoveArmies);
            Assert.IsTrue(BoardDraft.SelectRegion(draft, view, "a"));
            Assert.IsFalse(draft.Ready(view.Options[0]));
            CollectionAssert.AreEqual(new[] { "b" }, ArmyDraft.Targets(draft, view));
            Assert.IsTrue(BoardDraft.SelectRegion(draft, view, "b"));
            Assert.IsTrue(draft.Ready(view.Options[0]));
            Assert.IsTrue(BoardDraft.SelectRegion(draft, view, "c"));
            CollectionAssert.AreEqual(new[] { "b", "c" }, draft.Payload.Route);
            draft.Back();
            CollectionAssert.AreEqual(new[] { "b" }, draft.Payload.Route);
            Assert.IsTrue(draft.Ready(view.Options[0]));
            draft.Reconcile(view);
            Assert.IsTrue(draft.Ready(view.Options[0]));
            Assert.IsEmpty(draft.Payload.DieId);
        }

        [Test]
        public void GroupEditInvalidatesRouteAndReconcileDropsLostRoutes()
        {
            var view = View();
            var draft = new TurnDraft();
            draft.Choose(CommandKind.MoveArmies);
            ArmyDraft.Start(draft, view, "a");
            ArmyDraft.Target(draft, view, "b");
            EventArmyDraft.Toggle(draft, new[] { "companion" }, true);
            Assert.IsEmpty(draft.Payload.Route);
            Assert.IsFalse(draft.Ready(view.Options[0]));
            Assert.IsEmpty(ArmyDraft.Targets(draft, view));
            draft.Back();
            Assert.IsTrue(draft.Ready(view.Options[0]));
            view.Options[0].Armies[0].Targets.Clear();
            draft.Reconcile(view);
            Assert.IsEmpty(draft.Payload.Route);
            Assert.IsFalse(draft.Ready(view.Options[0]));
        }

        private static PlayerView View()
        {
            var army = new ArmyOption { RegionId = "a", UnitIds = { "unit" }, CharacterIds = { "companion" } };
            army.Targets.Add(new ArmyTargetOption { RegionId = "b", Route = { "b" }, EligibleIds = { "unit", "companion" } });
            army.Targets.Add(new ArmyTargetOption { RegionId = "c", Route = { "b", "c" }, EligibleIds = { "unit", "companion" } });
            return new PlayerView
            {
                You = Side.FreePeoples,
                Decision = new DecisionView { Kind = DecisionKind.ArmyEvent },
                Units = { new UnitView { Id = "unit", Location = "a", Side = Side.FreePeoples, Kind = UnitKind.Regular } },
                Characters = { new CharacterView { Id = "companion", Location = "a", Side = Side.FreePeoples } },
                Options = { new ActionOption { Kind = CommandKind.MoveArmies, Minimum = 1, Maximum = 1, ArmyEvent = new ArmyEventOption { RouteMovement = true, MaximumSteps = 2, RequiresCompanion = true }, Armies = { army }, Targets = { "b", "c" } } }
            };
        }
    }
}
