using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-455 (owner, 2026-11-03): the console has a small Cancel of its own under its four actions, always in its place and
    // greyed while there is nothing to cancel; it replaces UI-448's long Cancel option in the console's line. Its click drops
    // everything chosen since the last sent step, and a forced step starts again.
    public sealed class DraftCancelTests
    {
        private const string Legolas = "base.character.legolas";

        [Test]
        public void TheConsoleHasASmallCancelUnderItsFourActions()
        {
            var console = new TurnConsole();
            var actions = console.Actions;
            CollectionAssert.AreEqual(new[] { "console-back", "console-confirm", "console-pass", "console-discard", ConsoleActions.CancelName },
                actions.Root.Children().Select(child => child.name).ToArray(), "Cancel stands last, on its own line.");
            Assert.AreSame(actions.Cancel, actions.Root.Q<Button>(ConsoleActions.CancelName));
            Assert.AreEqual("Cancel", actions.Cancel.text);
            Assert.IsTrue(actions.Cancel.ClassListContains("console-cancel"));
            Assert.IsFalse(actions.Cancel.enabledSelf, "Greyed until something can be cancelled.");
            Assert.AreEqual(DraftCancel.NothingTooltip, actions.Cancel.tooltip);
            Assert.IsNull(console.Status.Options.Q<Button>("console-cancel-route"), "No Cancel option in the console's line.");
        }

        [Test]
        public void CancelDropsTheWholeUnsentDraft()
        {
            var view = ActionView(new ActionOption { Kind = CommandKind.MoveArmies, Enabled = true, EligibleDice = { "a" } });
            var draft = new TurnDraft();
            Assert.IsFalse(DraftCancel.Offered(view, draft), "Nothing chosen, nothing to cancel.");
            Assert.AreEqual(DraftCancel.NothingTooltip, DraftCancel.Tooltip(view, draft));
            draft.Choose(CommandKind.MoveArmies, "a");
            draft.Change(p => p.Moves.Add(new ArmyMoveChoice { From = "rohan", To = "fords-of-isen" }));
            Assert.IsTrue(DraftCancel.Offered(view, draft));
            Assert.AreEqual("Cancel · Drop everything chosen since your last sent step", DraftCancel.Tooltip(view, draft));
            DraftCancel.Apply(draft, view);
            Assert.IsNull(draft.Kind, "The action is dropped…");
            Assert.AreEqual("", draft.Payload.DieId, "…with its die…");
            CollectionAssert.IsEmpty(draft.Payload.Moves, "…and its moves, at once (Back undoes one step).");

            view.DecisionOwner = Side.Shadow;
            draft.Choose(CommandKind.MoveArmies, "a");
            Assert.IsFalse(DraftCancel.Offered(view, draft), "Nothing of the opponent's decision is cancelled.");
        }

        [Test]
        public void ASeparationsCancelClearsItsCompanions()
        {
            var view = ActionView(new ActionOption
            {
                Kind = CommandKind.SeparateCompanions, Enabled = true, Minimum = 1, Maximum = 1, EligibleIds = { Legolas }, EligibleDice = { "a" },
                Travel = { new TravelOption { Id = Legolas, Origin = "rivendell", Range = 1, Targets = { "rivendell", "eregion" } } }
            });
            var draft = new TurnDraft();
            draft.Choose(CommandKind.SeparateCompanions, "a");
            draft.Change(p => p.Ids.Add(Legolas));
            Assert.IsTrue(DraftCancel.Offered(view, draft));
            Assert.AreEqual("Cancel · Clear the chosen companions and their route", DraftCancel.Tooltip(view, draft));
            DraftCancel.Apply(draft, view);
            Assert.IsNull(draft.Kind);
            CollectionAssert.IsEmpty(draft.Payload.Ids);
        }

        // A reveal is forced: its Cancel clears the drawn route and the reveal starts again, so with no route it waits.
        [Test]
        public void AForcedStepStartsAgainSoItsCancelWaitsForAChange()
        {
            var view = ActionView(new ActionOption
            {
                Kind = CommandKind.RevealFellowship, Enabled = true,
                Travel = { new TravelOption { Id = "fellowship", Origin = "rivendell", Range = 2, Targets = { "eregion", "hollin" } } }
            });
            view.Decision = new DecisionView { Id = "reveal", Kind = DecisionKind.FellowshipRoute };
            var draft = new TurnDraft();
            MandatoryDraft.Ensure(view, draft);
            Assert.AreEqual(CommandKind.RevealFellowship, draft.Kind, "The reveal is the decision's forced step.");
            Assert.IsFalse(DraftCancel.Offered(view, draft), "Nothing drawn yet.");
            draft.Change(p => p.Route.AddRange(new[] { "rivendell", "eregion" }));
            Assert.IsTrue(DraftCancel.Offered(view, draft));
            Assert.AreEqual("Cancel · Clear the drawn route", DraftCancel.Tooltip(view, draft));
            DraftCancel.Apply(draft, view);
            Assert.AreEqual(CommandKind.RevealFellowship, draft.Kind, "The reveal starts again.");
            CollectionAssert.IsEmpty(draft.Payload.Route);
        }

        // An action sequence's die is already spent: its next step is where Cancel returns, so the step alone has nothing to
        // cancel.
        [Test]
        public void AnActionSequencesStepIsNoCancelOfItsSpentDie()
        {
            var view = ActionView(new ActionOption { Kind = CommandKind.MoveArmies, Enabled = true });
            view.Decision = new DecisionView { Id = "sequence", Kind = DecisionKind.ActionSequence };
            view.ActionSequence = new ActionSequenceView { Owner = Side.FreePeoples, Kind = CommandKind.MoveArmies, DieId = "a", Face = DieFace.Army };
            var draft = new TurnDraft();
            MandatoryDraft.Ensure(view, draft);
            Assert.AreEqual(CommandKind.MoveArmies, draft.Kind);
            Assert.IsFalse(DraftCancel.Offered(view, draft), "The sequence's own step is not cancelled.");
            draft.Change(p => p.Moves.Add(new ArmyMoveChoice { From = "rohan", To = "fords-of-isen" }));
            Assert.IsTrue(DraftCancel.Offered(view, draft));
            DraftCancel.Apply(draft, view);
            Assert.AreEqual(CommandKind.MoveArmies, draft.Kind, "The sequence's step starts again…");
            Assert.AreEqual("a", draft.Payload.DieId, "…with its spent die.");
            CollectionAssert.IsEmpty(draft.Payload.Moves);
        }

        private static PlayerView ActionView(ActionOption option) => new PlayerView
        {
            You = Side.FreePeoples,
            DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action },
            Options = { option },
            Dice = { new DieView { Id = "a", Face = DieFace.Army, Source = "standard", Owner = Side.FreePeoples, Location = DieLocation.Available } }
        };
    }
}
