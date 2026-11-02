using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class LordsFinalCardsTests
    {
        [Test]
        public void MadnessHasBothReadableHalvesAndInitiativeZero()
        {
            var card = CardLibrary.Cards["lords.sh.strategy.26"];
            Assert.IsTrue(card.HasCombat);
            CollectionAssert.AreEqual(new[] { 0 }, card.Initiative);
            StringAssert.Contains("Sauron", card.Event);
            StringAssert.Contains("adjacent to the target army", card.Event);
            var inspector = new CardInspector();
            inspector.ShowCombat(card.Id);
            Assert.AreEqual(card.Combat, inspector.Root.Q<Label>("card-combat-text").text);
            Assert.AreEqual(DisplayStyle.Flex, inspector.Root.Q<VisualElement>("card-combat-section").style.display.value);
        }

        [Test]
        public void SpecialCardShowsOnlyItsEventEvenFromACombatContext()
        {
            const string id = "lords.sh.special.05";
            var card = CardLibrary.Cards[id];
            Assert.IsFalse(card.HasCombat);
            Assert.IsEmpty(card.Initiative);
            var inspector = new CardInspector();
            inspector.Update(new PlayerView { Decision = new DecisionView { Kind = DecisionKind.CombatCard } });
            inspector.ShowCombat(id);
            Assert.AreEqual(card.Event, inspector.Root.Q<Label>("card-text").text);
            Assert.AreEqual("SPECIAL EVENT", inspector.Root.Q<Label>("card-event-heading").text);
            Assert.AreEqual(DisplayStyle.None, inspector.Root.Q<VisualElement>("card-combat-section").style.display.value);
            StringAssert.Contains("automatically", inspector.Root.Q<Label>("card-window").text);
            inspector.Show("lords.sh.strategy.26", true);
            Assert.AreEqual(DisplayStyle.Flex, inspector.Root.Q<VisualElement>("card-combat-section").style.display.value);
        }

        [Test]
        public void SpecialReactionRemainsAnExplicitSelectionWithOneConfirmation()
        {
            var draft = new TurnDraft();
            var option = new ActionOption { Kind = CommandKind.RespondEffect, EligibleIds = { "lords.we-shall-get-it" } };
            draft.Choose(option.Kind);
            Assert.IsFalse(draft.Ready(option));
            draft.Change(p => p.ReactionId = "lords.we-shall-get-it");
            Assert.IsTrue(draft.Ready(option));
            draft.Back();
            Assert.IsFalse(draft.Ready(option));
            Assert.IsEmpty(draft.Payload.DieId);
        }

        [Test]
        public void PublicSpecialTableCardIsInspectableWithoutAnArtificialModifier()
        {
            const string id = "lords.sh.special.05";
            var view = new PlayerView { MatchId = "special-table" };
            view.Players.Add(new PlayerSummary { Side = Side.Shadow, TableCards = { id } });
            var table = new TableEffectsView(new VisualElement(), new MapCanvas(BoardPresentation.Load()));
            table.Apply(view);
            var source = table.Root.Q<Button>("effect-source-" + id);
            Assert.IsNotNull(source);
            Assert.IsTrue(source.ClassListContains("effect-card-tile"), "The table card is a card tile.");
            // UI-446 (owner): the card is read from its tile's unrolled text (LordsFinalCardsNetworkTests), only its part in
            // effect — a table card's Event; neither a click nor Inspect opens details beside or in the panel.
            Assert.IsTrue(ReadOnlyControl.Is(source), "A click on the card does nothing.");
            Assert.AreEqual(TooltipText.Parts(CardLibrary.Title(id), "A card in effect"), source.tooltip);
            Assert.IsFalse(EffectsPresentation.CombatPart(EffectsPresentation.Sources(view).Single()), "Its Event part is in effect.");
            Assert.AreEqual("SPECIAL CARD · SPECIAL EVENT", CardTextFormat.Kind(CardLibrary.Cards[id]));
            table.Inspect(id);
            Assert.IsTrue(table.IsOpen);
            Assert.IsEmpty(table.SelectedSource);
            Assert.AreEqual(DisplayStyle.None, table.Details.style.display.value);
            Assert.IsNull(table.Root.Q("effect-card"), "No card text block opens outside its tile's unrolled text.");
            view.Effects.Add(new ActiveEffectView { Id = "effect", SourceId = id, Owner = Side.Shadow });
            table.Apply(view);
            Assert.AreEqual("Effects · 1", table.Tab.text);
            Assert.AreSame(source, table.Root.Q<Button>("effect-source-" + id));
            view.Effects.Clear();
            view.Players[0].TableCards.Clear();
            table.Apply(view);
            Assert.AreEqual("Effects · 0", table.Tab.text);
            Assert.IsNull(table.Root.Q<Button>("effect-source-" + id));
        }
    }
}
