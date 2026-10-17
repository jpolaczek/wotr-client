using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-161: the Elven Ring gesture reads its dice and results only from the server's UseRing option.
    public sealed class RingChoiceTests
    {
        private static PlayerView View() => new PlayerView
        {
            You = Side.FreePeoples,
            DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = "d", Kind = DecisionKind.Action },
            Dice = new List<DieView>
            {
                new DieView { Id = "fp-1", Owner = Side.FreePeoples, Face = DieFace.Army, Location = DieLocation.Available },
                new DieView { Id = "fp-2", Owner = Side.FreePeoples, Face = DieFace.WillOfTheWest, Location = DieLocation.Available }
            },
            Options = new List<ActionOption>
            {
                new ActionOption
                {
                    Kind = CommandKind.UseRing, Enabled = true,
                    EligibleIds = new List<string> { "base.ring.0" },
                    EligibleDice = new List<string> { "fp-1" },
                    Faces = new List<DieFace> { DieFace.Character, DieFace.Army, DieFace.Muster }
                }
            }
        };

        private static TurnDraft Armed()
        {
            var draft = new TurnDraft();
            draft.Choose(CommandKind.UseRing);
            draft.Change(p => p.RingId = "base.ring.0");
            return draft;
        }

        [Test]
        public void AnArmedRingOffersOnlyTheServersDice()
        {
            var view = View();
            Assert.AreEqual(RingStep.None, RingChoice.Step(new TurnDraft()));
            Assert.IsEmpty(RingChoice.Dice(view, new TurnDraft()));
            var draft = Armed();
            Assert.AreEqual(RingStep.Die, RingChoice.Step(draft));
            CollectionAssert.AreEqual(new[] { "fp-1" }, RingChoice.Dice(view, draft));
            StringAssert.StartsWith("Click a highlighted die", RingChoice.Hint(draft));
            Assert.IsEmpty(RingChoice.Faces(view, draft), "No results before a die is chosen.");
        }

        [Test]
        public void TheChosenDieOffersEveryResultButItsCurrentOne()
        {
            var view = View();
            var draft = Armed();
            draft.Change(p => p.DieId = "fp-1");
            Assert.AreEqual(RingStep.Face, RingChoice.Step(draft));
            var faces = RingChoice.Faces(view, draft);
            CollectionAssert.AreEqual(new[] { DieFace.Character, DieFace.Army, DieFace.Muster }, faces.Select(f => f.Face));
            CollectionAssert.AreEqual(new[] { true, false, true }, faces.Select(f => f.Enabled), "The current Army result is disabled.");
            StringAssert.StartsWith("Choose the new result", RingChoice.Hint(draft));
        }

        [Test]
        public void ClearingTheDraftEndsTheGesture()
        {
            var draft = Armed();
            draft.Change(p => p.DieId = "fp-1");
            draft.Clear();
            Assert.IsFalse(RingChoice.Armed(draft));
            Assert.AreEqual("", RingChoice.Hint(draft));
        }
    }
}
