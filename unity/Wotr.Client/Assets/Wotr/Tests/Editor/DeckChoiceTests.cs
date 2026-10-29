using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-390 (owner's report on the Kings branch): a deck choice — here the Palantír of Orthanc's draw — is made on the
    // decks. It leads to the hand, names its source and step, and lights each offered deck.
    public sealed class DeckChoiceTests
    {
        private const string Palantir = "base.sh.character.21";

        [Test]
        public void TheDeckChoiceLeadsToTheHand()
        {
            var attention = DecisionAttention.For(View());
            Assert.AreEqual(DecisionContext.Hand, attention.Context);
            Assert.IsEmpty(attention.Region);
        }

        [Test]
        public void TheConsoleNamesTheCardAndTheDecksClick()
        {
            var view = View();
            var state = TurnConsoleState.For(view, new TurnDraft(), new ActionDieContext(), id => id);
            Assert.AreEqual("YOUR DECISION · The Palantír of Orthanc · Choose a deck", state.Headline);
            Assert.AreEqual("Click a glowing deck beside your hand to draw its top card.", state.Guidance);
            Assert.AreEqual("The Palantír of Orthanc · Choose a deck", DecisionNoticeText.Describe(view));
            Assert.IsTrue(DeckChoice.Offers(view, "character"));
            Assert.IsTrue(DeckChoice.Offers(view, "strategy"));
            view.DecisionOwner = Side.FreePeoples;
            Assert.IsFalse(DeckChoice.Pending(view), "The opponent's deck choice is not this player's.");
        }

        private static PlayerView View()
        {
            var view = new PlayerView
            {
                MatchId = "deck", You = Side.Shadow, DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Id = "palantir", Kind = DecisionKind.EffectChoice, Presentation = DeckChoice.Presentation, SourceId = Palantir }
            };
            view.Options.Add(new ActionOption { Kind = CommandKind.ChooseEffect, EligibleIds = { "strategy", "character" }, Minimum = 1, Maximum = 1 });
            return view;
        }
    }
}
