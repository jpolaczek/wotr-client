using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-390 (owner's report, 2026-10-28): a deck choice is made on the decks. The Palantír of Anárion's extra card after
    // Denethor's token paid for Horn of Gondor (Kings) and the base game's Palantír of Orthanc draw both lead to the hand,
    // never to their source's region, name their source and step, and Denethor's window keeps no Open decision link that
    // leads nowhere.
    public sealed class DeckChoiceTests
    {
        private const string Denethor = "kings.sovereign.denethor";
        private const string Palantir = "base.sh.character.21";

        [Test]
        public void TheDeckChoiceLeadsToTheHandNotToItsSourcesRegion()
        {
            var view = DenethorView(false);
            var attention = DecisionAttention.For(view);
            Assert.AreEqual(DecisionContext.Hand, attention.Context);
            Assert.IsEmpty(attention.Region, "Minas Tirith, where Denethor stands, is not where the step is made.");
            Assert.IsFalse(CharacterRosterActions.HasReaction(view, Denethor), "His window links to no decision.");
        }

        [Test]
        public void TheConsoleNamesTheSourceAndTheDecksClick()
        {
            var view = DenethorView(false);
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
            var view = DenethorView(true);
            StringAssert.EndsWith(", or Pass to complete the action without an extra card.", DeckChoice.Hint(view));
            view.DecisionOwner = Side.Shadow;
            Assert.IsFalse(DeckChoice.Pending(view), "The opponent's deck choice is not this player's.");
        }

        // The base game's case (EXP-17 carries it from the base-game line): the Palantír of Orthanc's draw.
        [Test]
        public void ThePalantirOfOrthancsDrawLeadsToTheHand()
        {
            var attention = DecisionAttention.For(PalantirView());
            Assert.AreEqual(DecisionContext.Hand, attention.Context);
            Assert.IsEmpty(attention.Region);
        }

        [Test]
        public void TheConsoleNamesThePalantirOfOrthancAndTheDecksClick()
        {
            var view = PalantirView();
            var state = TurnConsoleState.For(view, new TurnDraft(), new ActionDieContext(), id => id);
            Assert.AreEqual("YOUR DECISION · The Palantír of Orthanc · Choose a deck", state.Headline);
            Assert.AreEqual("Click a glowing deck beside your hand to draw its top card.", state.Guidance);
            Assert.AreEqual("The Palantír of Orthanc · Choose a deck", DecisionNoticeText.Describe(view));
            Assert.IsTrue(DeckChoice.Offers(view, "character"));
            Assert.IsTrue(DeckChoice.Offers(view, "strategy"));
            view.DecisionOwner = Side.FreePeoples;
            Assert.IsFalse(DeckChoice.Pending(view), "The opponent's deck choice is not this player's.");
        }

        private static PlayerView DenethorView(bool optional)
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

        private static PlayerView PalantirView()
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
