using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-482 (owner, 2026-11-04): EXP-19 stops offering the declaration at the start of turn 1 when it can change nothing;
    // Declare keeps its place, greyed, says why, and nothing offers it. The pinned test backend still offers it, so this
    // view without the option is a fixture.
    public sealed class FellowshipDeclareUnavailableTests
    {
        [Test]
        public void AtTheStartOfTheGameDeclareIsGreyedAndSaysWhy()
        {
            var view = View();
            var declare = Declare(view, out var controls);
            Assert.AreEqual(DisplayStyle.Flex, declare.style.display.value, "Disabled, not hidden.");
            Assert.IsFalse(declare.enabledSelf);
            Assert.AreEqual("Declare · The Fellowship has not moved yet: there is nothing to declare at the start of the game", declare.tooltip);
            Assert.IsFalse(declare.ClassListContains("legal-target"));
            Assert.IsFalse(declare.ClassListContains(ChoiceGlow.ClassName));
            Assert.IsTrue(controls.Offers, "Finish phase alone lights the panel.");
            Assert.IsTrue(controls.Root.Q<Button>("fellowship-continue").enabledSelf);
        }

        [Test]
        public void NoHintOrGlowOffersIt()
        {
            var view = View();
            Assert.IsNull(FellowshipActions.Option(view, new ActionDieContext(), CommandKind.DeclareFellowship));
            var guidance = TurnConsoleState.For(view, new TurnDraft(), new ActionDieContext(), id => id).Guidance;
            Assert.AreEqual("Continue to the Hunt.", guidance);
            StringAssert.DoesNotContain("Declare", FellowshipActions.ActionHint(view, new TurnDraft(), new ActionDieContext()));
        }

        [Test]
        public void TheReasonIsForTheStartOfTheGameAlone()
        {
            var moved = View();
            moved.Fellowship.Progress = 1;
            Assert.AreEqual(FellowshipControls.DeclareUnavailable, Declare(moved, out _).tooltip);
            var later = View();
            later.Turn = 2;
            Assert.AreEqual(FellowshipControls.DeclareUnavailable, Declare(later, out _).tooltip);
            var actions = View();
            actions.Phase = GamePhase.ActionResolution;
            actions.Decision.Kind = DecisionKind.Action;
            Assert.AreEqual(FellowshipControls.DeclareUnavailable, Declare(actions, out _).tooltip);
            // The Council's homelands come before the phase's own choices, which may yet offer a declaration.
            var homelands = View();
            homelands.Decision.Kind = DecisionKind.CouncilHomelands;
            Assert.AreEqual(FellowshipControls.DeclareUnavailable, Declare(homelands, out _).tooltip);
        }

        [Test]
        public void AnOfferedDeclarationStaysLitAtTheStartOfTheGame()
        {
            var view = View();
            view.Options.Add(new ActionOption { Kind = CommandKind.DeclareFellowship, Enabled = true });
            var declare = Declare(view, out _);
            Assert.IsTrue(declare.enabledSelf);
            Assert.AreEqual(FellowshipControls.DeclareStart, declare.tooltip);
            Assert.IsFalse(FellowshipActions.NothingToDeclare(view));
        }

        private static Button Declare(PlayerView view, out FellowshipControls controls)
        {
            controls = new FellowshipControls();
            controls.Refresh(view, new TurnDraft(), new ActionDieContext(), true, false);
            return controls.Root.Q<Button>("fellowship-declare");
        }

        private static PlayerView View() => new PlayerView
        {
            You = Side.FreePeoples, DecisionOwner = Side.FreePeoples, Turn = 1, Phase = GamePhase.Fellowship,
            Decision = new DecisionView { Id = "fellowship", Kind = DecisionKind.FellowshipPhase },
            Fellowship = new FellowshipView { Hidden = true, Progress = 0, Guide = "base.character.gandalf-grey" },
            Options = { new ActionOption { Kind = CommandKind.ContinueFellowship, Enabled = true } }
        };
    }
}
