using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.Tests
{
    // UI-288: the Hunt's responses and the cards among the effects are pictures, not glyphs or title rectangles.
    public sealed class HuntAndEffectPicturesTests
    {
        private const string Strider = "base.character.strider";
        private const string Gandalf = "base.character.gandalf-grey";
        private const string AxeAndBow = "base.fp.character.06";

        [Test]
        public void ResponsesShowTheRingTheCurrentGuideAndTheCompanionsStack()
        {
            var view = DamageView();
            var responses = new HuntResponsesView();
            responses.Context(view, new TurnDraft(), true);

            Assert.IsTrue(Art(responses, HuntResponse.Ring).ClassListContains("hunt-response-ring"));
            Assert.IsTrue(Art(responses, HuntResponse.RandomCompanion).ClassListContains("hunt-response-random"));
            Assert.IsEmpty(responses.Root.Query<Label>("hunt-response-symbol").ToList(), "No glyph stands for a response.");
            var guide = Art(responses, HuntResponse.Guide).Q<Image>();
            Assert.AreSame(FigureArtwork.Sprite(FigureAppearances.Character(Strider, Side.FreePeoples)), guide.sprite);

            view.Fellowship.Guide = Gandalf;
            responses.Context(view, new TurnDraft(), true);
            Assert.AreSame(FigureArtwork.Sprite(FigureAppearances.Character(Gandalf, Side.FreePeoples)), guide.sprite,
                "The response follows the Guide of the moment.");
        }

        [Test]
        public void TableCardResponseIsAParchmentCardWithItsCornerSymbol()
        {
            var view = DamageView();
            view.Decision = new DecisionView { Id = "table", Kind = DecisionKind.Reaction, Window = "hunt.table" };
            view.Players.Add(new PlayerSummary { Side = Side.FreePeoples, TableCards = { AxeAndBow } });
            view.Options.Clear();
            view.Options.Add(new ActionOption
            {
                Kind = CommandKind.RespondEffect, Enabled = true, EligibleIds = { "card.axe-and-bow" },
                EffectChoices = { new EffectChoiceView { Id = "card.axe-and-bow", SourceId = AxeAndBow, Title = "Reduce damage", Cost = "DiscardTableCard" } }
            });
            var responses = new HuntResponsesView();
            responses.Context(view, new TurnDraft(), true);

            var card = responses.Root.Q<Button>("effect-choice-card.axe-and-bow").Q("hunt-response-art");
            Assert.IsTrue(card.ClassListContains("hunt-response-card"));
            var symbol = card.Q<ActionSymbol>();
            Assert.IsNotNull(symbol, "The card shows the symbol printed on its corner.");
            Assert.IsTrue(symbol.ClassListContains(ActionSymbol.InkClass));
        }

        [Test]
        public void TableCardsAmongTheEffectsAreParchmentCardsAndAbilitiesStayPlain()
        {
            var view = new PlayerView();
            view.Players.Add(new PlayerSummary { Side = Side.FreePeoples, TableCards = { AxeAndBow } });
            view.Effects.Add(new ActiveEffectView
            {
                Id = "ability", SourceId = "base.character.saruman", SourceKind = "Ability", Title = "Saruman",
                Description = "A public ability.", Duration = "UntilRemoved"
            });
            var table = new TableEffectsView(new VisualElement(), new MapCanvas(BoardPresentation.Load()));
            table.Apply(view);

            var card = table.Root.Q<Button>("effect-source-" + AxeAndBow);
            Assert.IsTrue(card.ClassListContains("effect-card-tile"));
            Assert.IsNotNull(card.Q<ActionSymbol>());
            Assert.AreEqual(CardLibrary.Title(AxeAndBow), card.text);
            var ability = table.Root.Q<Button>("effect-source-base.character.saruman");
            Assert.IsFalse(ability.ClassListContains("effect-card-tile"), "An ability is not a card.");
            Assert.IsNull(ability.Q<ActionSymbol>());
        }

        private static VisualElement Art(HuntResponsesView responses, HuntResponse response) =>
            responses.Root.Q<Button>("hunt-response-" + response).Q("hunt-response-art");

        private static PlayerView DamageView() => new PlayerView
        {
            You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = "damage", Kind = DecisionKind.HuntDamage },
            Hunt = new HuntView { Damage = 3 },
            Fellowship = new FellowshipView { Corruption = 4, Guide = Strider },
            OwnCompanions =
            {
                new CharacterView { Id = Strider, Level = 3 },
                new CharacterView { Id = Gandalf, Level = 3 }
            },
            Options = { new ActionOption { Kind = CommandKind.ResolveHuntDamage, Enabled = true,
                HuntResponses = { HuntResponse.Ring, HuntResponse.Guide, HuntResponse.RandomCompanion } } }
        };
    }
}
