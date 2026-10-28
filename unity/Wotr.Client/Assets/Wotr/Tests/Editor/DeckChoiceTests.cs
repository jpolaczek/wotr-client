using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-390 (owner's report, 2026-10-28): the Palantír of Anárion's extra card after Denethor's token paid for Horn of
    // Gondor is made on the decks. It leads to the hand, never to Denethor's region, names its source and step, and his
    // window keeps no Open decision link that leads nowhere.
    public sealed class DeckChoiceTests
    {
        private const string Denethor = "kings.sovereign.denethor";

        [Test]
        public void TheDeckChoiceLeadsToTheHandNotToItsSourcesRegion()
        {
            var view = View(false);
            var attention = DecisionAttention.For(view);
            Assert.AreEqual(DecisionContext.Hand, attention.Context);
            Assert.IsEmpty(attention.Region, "Minas Tirith, where Denethor stands, is not where the step is made.");
            Assert.IsFalse(CharacterRosterActions.HasReaction(view, Denethor), "His window links to no decision.");
        }

        [Test]
        public void TheConsoleNamesTheSourceAndTheDecksClick()
        {
            var view = View(false);
            var state = TurnConsoleState.For(view, new TurnDraft(), new ActionDieContext(), id => id);
            Assert.AreEqual("YOUR DECISION · Denethor · Choose a deck", state.Headline);
            Assert.AreEqual("Click a glowing deck beside your hand to draw its top card.", state.Guidance);
            Assert.AreEqual("Denethor · Choose a deck", DecisionNoticeText.Describe(view));
            Assert.IsTrue(DeckChoice.Offers(view, "character"));
            Assert.IsTrue(DeckChoice.Offers(view, "strategy"));
        }

        [Test]
        public void AnOptionalDrawNamesThePass()
        {
            var view = View(true);
            StringAssert.EndsWith(", or Pass to complete the action without an extra card.", DeckChoice.Hint(view));
            view.DecisionOwner = Side.Shadow;
            Assert.IsFalse(DeckChoice.Pending(view), "The opponent's deck choice is not this player's.");
        }

        private static PlayerView View(bool optional)
        {
            var view = new PlayerView
            {
                MatchId = "deck", You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "anarion", Kind = DecisionKind.EffectChoice, Presentation = DeckChoice.Presentation, SourceId = Denethor }
            };
            view.Sovereigns.Add(new SovereignView { Id = Denethor, Title = "Denethor", RegionId = "minas-tirith" });
            view.Characters.Add(new CharacterView { Id = Denethor, Location = "minas-tirith", Side = Side.FreePeoples });
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, EligibleIds = { "strategy", "character" }, Minimum = 1, Maximum = 1 };
            if (optional)
                option.EligibleIds.Add("decline");
            view.Options.Add(option);
            return view;
        }
    }
}
