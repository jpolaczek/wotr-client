using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    // UI-167 (SIGNALS_UX B, SG05–SG07): the ribbon marks the moment the action passes to the player.
    public sealed class TurnSignalTests
    {
        [Test]
        public void TheActionPassingToThePlayerGivesTheRibbonOnceButNotOnTheFirstView()
        {
            var signal = new TurnSignal();
            Assert.AreEqual("", signal.Observe(View("a1", Side.FreePeoples, DecisionKind.Action)), "The first view only lights the console.");
            Assert.AreEqual("", signal.Observe(View("s1", Side.Shadow, DecisionKind.Action)), "The opponent's turn gives nothing.");
            Assert.AreEqual("YOUR ACTION · Turn 3", signal.Observe(View("a2", Side.FreePeoples, DecisionKind.Action)));
            Assert.AreEqual("", signal.Observe(View("a2", Side.FreePeoples, DecisionKind.Action)), "The same decision again is not a new moment.");
            Assert.AreEqual("", signal.Observe(View("a3", Side.FreePeoples, DecisionKind.Action)), "A second own action in a row already has the turn.");
        }

        [Test]
        public void FellowshipPhaseAndHuntAllocationHaveTheirRibbonsAndInnerDecisionsHaveNone()
        {
            var shadow = new TurnSignal();
            shadow.Observe(View("f", Side.FreePeoples, DecisionKind.FellowshipPhase, Side.Shadow));
            Assert.AreEqual("ALLOCATE HUNT DICE", shadow.Observe(View("h", Side.Shadow, DecisionKind.AllocateHunt, Side.Shadow)));
            var free = new TurnSignal();
            free.Observe(View("s", Side.Shadow, DecisionKind.Action));
            Assert.AreEqual("FELLOWSHIP PHASE", free.Observe(View("f", Side.FreePeoples, DecisionKind.FellowshipPhase)));
            foreach (var kind in new[] { DecisionKind.CombatCard, DecisionKind.Losses, DecisionKind.HuntDamage, DecisionKind.EffectChoice,
                DecisionKind.ActionSequence, DecisionKind.Reaction })
            {
                free.Observe(View("o-" + kind, Side.Shadow, DecisionKind.Action));
                Assert.AreEqual("", free.Observe(View("p-" + kind, Side.FreePeoples, kind)), kind.ToString());
            }
        }

        [Test]
        public void AReconnectionAndTheEndOfTheGameGiveNoRibbon()
        {
            var signal = new TurnSignal();
            signal.Observe(View("s1", Side.Shadow, DecisionKind.Action));
            signal.Rebase();
            Assert.AreEqual("", signal.Observe(View("a1", Side.FreePeoples, DecisionKind.Action)), "The view after a reconnection catches up.");
            signal.Observe(View("s2", Side.Shadow, DecisionKind.Action));
            var won = View("a2", Side.FreePeoples, DecisionKind.Action);
            won.Winner = Side.FreePeoples;
            Assert.AreEqual("", signal.Observe(won));
        }

        [Test]
        public void TheConsoleGlowsUntilTheFirstStepOfAStartingDecision()
        {
            var view = View("a", Side.FreePeoples, DecisionKind.Action);
            var draft = new TurnDraft();
            Assert.IsTrue(TurnSignal.Waiting(view, draft));
            draft.Change(payload => payload.DieId = "die-1");
            Assert.IsFalse(TurnSignal.Waiting(view, draft), "Choosing a die is the first step.");
            var hunt = View("h", Side.Shadow, DecisionKind.AllocateHunt, Side.Shadow);
            var allocation = new TurnDraft();
            allocation.Choose(CommandKind.AllocateHunt);
            Assert.IsTrue(TurnSignal.Waiting(hunt, allocation), "The allocation the client opens itself is not a step.");
            allocation.Toggle("shadow-die");
            Assert.IsFalse(TurnSignal.Waiting(hunt, allocation));
            Assert.IsFalse(TurnSignal.Waiting(View("s", Side.Shadow, DecisionKind.Action), new TurnDraft()), "Not while the opponent acts.");
            Assert.IsFalse(TurnSignal.Waiting(View("c", Side.FreePeoples, DecisionKind.CombatCard), new TurnDraft()));
        }

        [Test]
        public void TheRibbonNeverTakesThePointerAndCarriesTheSidesEnamel()
        {
            var ribbon = new TurnRibbon();
            Assert.IsFalse(ribbon.IsShowing);
            ribbon.Show("YOUR ACTION · Turn 2", Side.Shadow);
            Assert.IsTrue(ribbon.IsShowing);
            Assert.AreEqual(1, ribbon.Shown);
            Assert.AreEqual("YOUR ACTION · Turn 2", ribbon.Text.text);
            Assert.IsTrue(ribbon.Root.Q("turn-ribbon-band").ClassListContains("side-shadow"));
            foreach (var element in ribbon.Root.Query<VisualElement>().ToList())
                Assert.AreEqual(PickingMode.Ignore, element.pickingMode, element.name);
            ribbon.Hide();
            Assert.IsFalse(ribbon.IsShowing);

            var status = new ConsoleStatus();
            status.SetYourTurn(true);
            Assert.IsTrue(status.YourTurn);
            Assert.AreEqual(PickingMode.Ignore, status.Plaque.Q("console-status-glow").pickingMode);
            status.SetYourTurn(false);
            status.Flash();
            Assert.IsTrue(status.Plaque.ClassListContains("flash"));
            Assert.AreEqual(1, status.Flashes);
        }

        // A view of the player `you` (the Free Peoples unless said) with a decision of `owner`.
        private static PlayerView View(string decision, Side owner, DecisionKind kind, Side you = Side.FreePeoples) => new PlayerView
        {
            You = you, Turn = 3, DecisionOwner = owner,
            Decision = new DecisionView { Id = decision, Kind = kind }
        };
    }
}
