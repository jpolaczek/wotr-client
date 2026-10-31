using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-216: a card's army event is planned on the map and in the region panel; the console counts the armies and
    // confirms. No decision window opens. UI-431 (owner's rule, UI-430): its Move and Attack are lit together on the map
    // (UI-389) and its Skip maneuver is the console's ✓; the console has no buttons for them.
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

        // The Black Captain Commands' Witch-king maneuver: Moria's army may move to Hollin or attack Lórien's Elves.
        private static PlayerView Maneuver()
        {
            var view = View();
            view.Decision.SourceId = "base.sh.character.24";
            view.Options.Add(new ActionOption
            {
                Kind = CommandKind.AttackArmy, Enabled = true, Minimum = 1, Maximum = 1, ArmyEvent = new ArmyEventOption(),
                Armies =
                {
                    new ArmyOption
                    {
                        RegionId = "moria", UnitIds = { "moria-2", "moria-1" },
                        Targets = { new ArmyTargetOption { RegionId = "lorien", BattleMode = BattleMode.Field, EligibleIds = { "moria-1", "moria-2" } } }
                    }
                }
            });
            view.Units.Add(new UnitView { Id = "lorien-1", Nation = "elves", Kind = UnitKind.Elite, Side = Side.FreePeoples, Location = "lorien" });
            return view;
        }

        private static TurnDraft Moving()
        {
            var draft = new TurnDraft();
            draft.Choose(CommandKind.MoveArmies);
            return draft;
        }

        [Test]
        public void ALitArmyStartsWithAllItsFiguresWhichTheRegionPanelToggles()
        {
            var view = View();
            var draft = Moving();
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

        // UI-431: no console button; the ✓ is Skip maneuver while no army is chosen and confirms the step once one is.
        [Test]
        public void TheConsoleHasNoArmyButtonsAndItsCheckMarkFinishesUntilAnArmyIsChosen()
        {
            var view = View();
            var draft = Moving();
            Assert.IsEmpty(CardConsoleOptions.For(view, draft));
            var finish = FinishingOption.Option(view, draft);
            Assert.AreEqual(ConsoleOptionKind.Action, finish.Kind);
            Assert.AreEqual(CommandKind.FinishArmyEvent, finish.Command);
            Assert.IsTrue(finish.Enabled);
            Assert.AreEqual("Finish · End " + CardLibrary.Title("base.sh.strategy.15") + " without moving or attacking", finish.Tooltip);
            Assert.AreEqual("Or ✓ to finish without moving or attacking.", FinishingOption.Hint(view, draft));
            BoardDraft.SelectRegion(draft, view, "moria");
            Assert.IsNull(FinishingOption.Option(view, draft), "A chosen army makes the ✓ its confirmation.");
            BoardDraft.SelectRegion(draft, view, "hollin");
            Assert.AreEqual("Armies moved 1 of 2", CardDecisions.Progress(view, draft));
            var tray = new DecisionTray(new VisualElement());
            tray.Render(view, draft, false, false);
            Assert.IsFalse(tray.HasChoices);
        }

        // UI-431 (UI-389, UI-403): the chosen army's attack targets light red with its move destinations; the attack target's
        // click makes the attack, with exactly the step the old Attack button and the same two clicks made.
        [Test]
        public void AnAttackTargetLightsWithTheMovesAndItsClickMakesTheOldAttackButtonsStep()
        {
            var view = Maneuver();
            var draft = Moving();
            CollectionAssert.IsEmpty(CardArmyEvent.OtherTargets(view, draft), "Nothing lights before an army is chosen.");
            Assert.AreEqual("Click a lit army, then where it moves or a red region it attacks.", CardArmyEvent.Hint(view, draft));
            Assert.IsTrue(CardArmyEvent.Start(view, draft, "moria"));
            CollectionAssert.AreEqual(new[] { "lorien" }, CardArmyEvent.OtherTargets(view, draft));
            CollectionAssert.AreEqual(new[] { "lorien" }, CardArmyEvent.AttackTargets(view, draft));
            Assert.AreEqual("Click a lit destination to move or a red region to attack", CardArmyEvent.Hint(view, draft));
            Assert.IsTrue(CardArmyEvent.IsTarget(view, draft, "hollin"), "The move lights with the attack.");

            Assert.IsTrue(CardArmyEvent.Switch(view, draft, "lorien"));
            Assert.AreEqual(CommandKind.AttackArmy, draft.Kind);
            var old = new TurnDraft();
            old.Choose(CommandKind.MoveArmies);
            DraftGesture.Choose(old, view, CommandKind.AttackArmy);
            AttackDraft.SelectRegion(old, view, "moria");
            AttackDraft.SelectRegion(old, view, "lorien");
            Assert.AreEqual(JsonConvert.SerializeObject(old.Payload), JsonConvert.SerializeObject(draft.Payload));
            draft.Back();
            Assert.AreEqual(CommandKind.MoveArmies, draft.Kind, "One Back undoes the switch.");
        }

        // An army the event may only attack with starts the attack; a second planned army leaves the attack out.
        [Test]
        public void AnAttackOnlyArmyStartsTheAttackAndTwoPlannedArmiesLeaveItOut()
        {
            var view = Maneuver();
            view.Options.Single(o => o.Kind == CommandKind.MoveArmies).Armies.RemoveAll(a => a.RegionId == "moria");
            var draft = Moving();
            Assert.IsTrue(CardArmyEvent.IsSource(view, draft, "moria"));
            Assert.IsTrue(CardArmyEvent.Start(view, draft, "moria"));
            Assert.AreEqual(CommandKind.AttackArmy, draft.Kind);
            Assert.IsTrue(CardArmyEvent.IsTarget(view, draft, "lorien"));

            var two = View();
            two.Options.Add(Maneuver().Options.Single(o => o.Kind == CommandKind.AttackArmy));
            var plan = Moving();
            BoardDraft.SelectRegion(plan, two, "dol-guldur");
            BoardDraft.SelectRegion(plan, two, "north-mirkwood");
            BoardDraft.SelectRegion(plan, two, "moria");
            CollectionAssert.IsEmpty(CardArmyEvent.OtherTargets(two, plan), "An attack takes one army.");
        }
    }
}
