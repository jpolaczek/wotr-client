using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-273: the Ruler discard in the pool and the numbered steps before the first action.
    public sealed class RulerDiceTests
    {
        [Test]
        public void TheDiscardsCandidatesSayWhatTheirClickDoesAndTheDiscardedDieStaysDimmed()
        {
            var view = View();
            var standard = view.Dice[0];
            var special = view.Dice[1];
            Assert.IsTrue(RulerDice.Candidate(view, standard));
            Assert.IsFalse(RulerDice.Candidate(view, special), "A Lords special die is never discarded.");
            StringAssert.StartsWith("Click to discard this die", DieHints.Describe(view, standard));
            Assert.AreEqual("Click a glowing die to discard it · It returns at the next Recover", RulerDice.Hint(view));
            view.Options[0].EligibleDice.Remove("ruler");
            StringAssert.StartsWith("Your only eligible die", RulerDice.Hint(view));

            standard.Location = DieLocation.Discarded;
            Assert.IsTrue(RulerDice.Discarded(view, standard));
            StringAssert.EndsWith("Discarded · Returns at the next Recover", DieHints.Describe(view, standard));
            view.Configuration.Kings = false;
            Assert.IsFalse(RulerDice.Discarded(view, standard), "Outside Kings a discarded die is only counted Out.");
        }

        [Test]
        public void ThePoolCrownsTheRulerDieAndKeepsTheDiscardedDieInItsSlot()
        {
            var view = View();
            view.Dice[0].Location = DieLocation.Discarded;
            var root = new VisualElement();
            new ActionPoolView(_ => { }, _ => { }).Apply(root, view, Side.Shadow, true);
            Assert.IsNotNull(root.Q<Button>("ruler").Q("ruler-crown"));
            var discarded = root.Q<Button>("standard");
            Assert.IsTrue(discarded.ClassListContains(ActionPoolView.DiscardedClass));
            Assert.IsFalse(discarded.enabledSelf);
            Assert.AreEqual("0", root.Q<Button>("pool-Discarded").text, "A die dimmed in its slot is not counted Out.");
        }

        [Test]
        public void WithLordsAndKingsTheStepsBeforeTheFirstActionAreNumbered()
        {
            var view = View();
            Assert.AreEqual("Step 2 of 4 · Click", PreActionSteps.Label(view, "Click"));
            Assert.AreEqual("Step 2 of 4 · Ruler discards", PreActionSteps.Label(view, ""));
            view.Decision.Kind = DecisionKind.EffectChoice;
            view.Decision.Presentation = LoreChoice.Presentation;
            Assert.AreEqual(4, PreActionSteps.Step(view));
            view.Configuration.Lords = false;
            Assert.AreEqual("Click", PreActionSteps.Label(view, "Click"), "Kings alone has no Lords steps to number.");
        }

        private static PlayerView View() => new PlayerView
        {
            You = Side.Shadow,
            DecisionOwner = Side.Shadow,
            Configuration = new MatchConfiguration { Lords = true, Kings = true },
            Decision = new DecisionView { Id = "discard", Kind = DecisionKind.RulerDiscard },
            Dice =
            {
                new DieView { Id = "standard", Owner = Side.Shadow, Source = "standard", Face = DieFace.Army, Location = DieLocation.Available },
                new DieView { Id = "balrog", Owner = Side.Shadow, Source = "lords.die.balrog", Face = DieFace.UseBalrog, Location = DieLocation.Available },
                new DieView { Id = "ruler", Owner = Side.Shadow, Source = RulerDice.Source, Face = DieFace.Muster, Location = DieLocation.Available }
            },
            Options = { new ActionOption { Kind = CommandKind.DiscardRulerDie, Enabled = true, EligibleDice = { "standard", "ruler" } } }
        };
    }
}
