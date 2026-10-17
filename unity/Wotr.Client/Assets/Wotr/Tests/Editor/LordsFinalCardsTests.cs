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
            table.Inspect(id);
            Assert.AreEqual("SPECIAL CARD · SPECIAL EVENT", table.Root.Q<Label>("effect-card-kind").text);
            Assert.AreEqual(CardLibrary.Cards[id].Event, table.Root.Q<Label>("effect-card-event").text);
            Assert.AreEqual(DisplayStyle.None, table.Root.Q("effect-card-combat").style.display.value);
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
