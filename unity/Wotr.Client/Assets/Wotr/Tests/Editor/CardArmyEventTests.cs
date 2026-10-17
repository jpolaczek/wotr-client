using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-216: a card's army event is planned on the map and in the region panel; the console counts the armies,
    // offers Skip maneuver and confirms. No decision window opens.
    public sealed class CardArmyEventTests
    {
        private static PlayerView View()
        {
            var move = new ActionOption { Kind = CommandKind.MoveArmies, Enabled = true, Minimum = 1, Maximum = 2, ArmyEvent = new ArmyEventOption() };
            foreach (var (region, target) in new[] { ("moria", "hollin"), ("dol-guldur", "north-mirkwood") })
                move.Armies.Add(new ArmyOption
                {
                    RegionId = region,
                    UnitIds = { region + "-1", region + "-2" },
                    Targets = { new ArmyTargetOption { RegionId = target, EligibleIds = { region + "-1", region + "-2" } } }
                });
            var view = new PlayerView
            {
                You = Side.Shadow,
                DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Id = "gather", Kind = DecisionKind.ArmyEvent, SourceId = "base.sh.strategy.15" },
                Options = { move, new ActionOption { Kind = CommandKind.FinishArmyEvent, Enabled = true } }
            };
            foreach (var army in move.Armies)
                foreach (var id in army.UnitIds)
                    view.Units.Add(new UnitView { Id = id, Nation = "sauron", Kind = UnitKind.Regular, Side = Side.Shadow, Location = army.RegionId });
            return view;
        }

        [Test]
        public void ALitArmyStartsWithAllItsFiguresWhichTheRegionPanelToggles()
        {
            var view = View();
            var draft = new TurnDraft();
            draft.Choose(CommandKind.MoveArmies);
            Assert.IsTrue(CardArmyEvent.Active(view, draft));
            CollectionAssert.AreEquivalent(new[] { "moria", "dol-guldur" }, CardArmyEvent.Sources(view, draft).Select(a => a.RegionId));
            Assert.IsTrue(BoardDraft.SelectRegion(draft, view, "moria"));
            Assert.AreEqual("moria", CardArmyEvent.ArmyRegion(draft));
            CollectionAssert.AreEquivalent(new[] { "moria-1", "moria-2" }, CardArmyEvent.Moving(draft));
            Assert.IsTrue(CardArmyEvent.Toggle(view, draft, "moria-2"));
            CollectionAssert.AreEqual(new[] { "moria-1" }, CardArmyEvent.Moving(draft));
            Assert.IsFalse(CardArmyEvent.Toggle(view, draft, "dol-guldur-1"), "Only the planned army's figures toggle.");
            Assert.IsTrue(CardArmyEvent.IsTarget(view, draft, "hollin"));
        }

        [Test]
        public void TheConsoleCountsTheArmiesAndOffersSkipManeuverWithoutAWindow()
        {
            var view = View();
            var draft = new TurnDraft();
            draft.Choose(CommandKind.MoveArmies);
            var skip = CardConsoleOptions.For(view, draft).Single();
            Assert.AreEqual("action-FinishArmyEvent", skip.Name);
            Assert.AreEqual(ConsoleOptionKind.Action, skip.Kind);
            Assert.AreEqual(CommandKind.FinishArmyEvent, skip.Command);
            BoardDraft.SelectRegion(draft, view, "moria");
            BoardDraft.SelectRegion(draft, view, "hollin");
            Assert.AreEqual("Armies moved 1 of 2", CardDecisions.Progress(view, draft));
            var tray = new DecisionTray(new VisualElement());
            tray.Render(view, draft, false, false);
            Assert.IsFalse(tray.HasChoices);
        }
    }
}
