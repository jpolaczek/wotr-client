using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class KingsCards03Tests
    {
        private static PlayerView DrawView()
        {
            var option = new ActionOption { Kind = CommandKind.DrawCard, EligibleDice = new List<string> { "muster", "event" },
                Targets = new List<string> { "character", "strategy" }, DrawCosts = new List<EventCostOption>
                {
                    new EventCostOption { DieId = "muster", Face = DieFace.Event, AbilityId = CardDrawDraft.Palantir },
                    new EventCostOption { DieId = "event", Face = DieFace.Event }
                } };
            return new PlayerView { You = Side.Shadow, Decision = new DecisionView { Kind = DecisionKind.Action },
                Options = new List<ActionOption> { option }, Dice = new List<DieView>
                {
                    new DieView { Id = "muster", Owner = Side.Shadow, Location = DieLocation.Available, Face = DieFace.Muster },
                    new DieView { Id = "event", Owner = Side.Shadow, Location = DieLocation.Available, Face = DieFace.Event }
                } };
        }

        [Test]
        public void DrawClickAndDragCarryTheSameExplicitConversionAndDeck()
        {
            var view = DrawView();
            var option = view.Options.Single();
            var click = new TurnDraft();
            DraftGesture.Choose(click, view, CommandKind.DrawCard);
            Assert.IsEmpty(click.Payload.DieId);
            click.Change(p => { p.DieId = "muster"; p.Deck = "strategy"; });
            CardDrawDraft.MatchCost(click, option);
            var drag = new TurnDraft();
            Assert.IsTrue(DraftGesture.Drop(drag, view, new DraftToken(DraftObject.Die, "muster"),
                new DraftToken(DraftObject.Deck, "strategy", CommandKind.DrawCard)));
            Assert.AreEqual(click.Payload.AbilityId, drag.Payload.AbilityId);
            Assert.AreEqual(CardDrawDraft.Palantir, drag.Payload.AbilityId);
            Assert.IsTrue(click.Ready(option));
            Assert.IsTrue(drag.Ready(option));
            click.Change(p => p.DieId = "event");
            CardDrawDraft.MatchCost(click, option);
            Assert.IsEmpty(click.Payload.AbilityId);
        }

        [Test]
        public void ReconnectRemovesSpentConversionWithoutSilentlySpendingAnotherDie()
        {
            var view = DrawView();
            var draft = new TurnDraft();
            DraftGesture.Drop(draft, view, new DraftToken(DraftObject.Die, "muster"),
                new DraftToken(DraftObject.Deck, "strategy", CommandKind.DrawCard));
            var option = view.Options.Single();
            option.DrawCosts.RemoveAt(0);
            option.EligibleDice.Remove("muster");
            Assert.IsTrue(draft.Reconcile(view));
            Assert.IsEmpty(draft.Payload.AbilityId);
            Assert.IsEmpty(draft.Payload.DieId);
            Assert.AreEqual("strategy", draft.Payload.Deck);
            Assert.IsFalse(draft.Ready(option));
        }

        [Test]
        public void NativeMusterAndPalantirEventRemainSeparateChoices()
        {
            var option = new ActionOption { Kind = CommandKind.PlayEvent, Events = new List<EventPlayOption>
            {
                new EventPlayOption { CardId = "kings.sh.strategy.05", Costs = new List<EventCostOption>
                {
                    new EventCostOption { DieId = "muster", Face = DieFace.Muster },
                    new EventCostOption { DieId = "muster", Face = DieFace.Event, AbilityId = CardDrawDraft.Palantir }
                } }
            } };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.PlayEvent);
            draft.Change(p => p.CardId = "kings.sh.strategy.05");
            CardDraft.SelectDie(draft, option, "muster");
            Assert.IsFalse(draft.Ready(option));
            draft.Change(p => { p.Face = DieFace.Event; p.CardCost = DieFace.Event; p.AbilityId = CardDrawDraft.Palantir; });
            Assert.IsTrue(draft.Ready(option));
            Assert.AreEqual("Event · Palantír", CardDrawDraft.Label(option.Events[0].Costs[1]));
        }

        [Test]
        public void BaradPalantirRemovalRequiresWillOrChosenRing()
        {
            var option = new ActionOption { Kind = CommandKind.DiscardBaradPalantir,
                EligibleDice = new List<string> { "will", "army" }, EligibleIds = new List<string> { "ring" },
                DiceWithoutRing = new List<string> { "will" } };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.DiscardBaradPalantir, "army");
            Assert.IsFalse(draft.Ready(option));
            draft.Change(p => p.RingId = "ring");
            Assert.IsTrue(draft.Ready(option));
            draft.Change(p => { p.RingId = ""; p.DieId = "will"; });
            Assert.IsTrue(draft.Ready(option));
        }

        [Test]
        public void SixShadowCardsHaveBothHalvesAndSourceInitiatives()
        {
            foreach (var number in new[] { 1, 3, 5, 6, 18, 25 })
            {
                var card = CardLibrary.Cards["kings.sh.strategy." + number.ToString("00")];
                Assert.IsNotEmpty(card.Event);
                Assert.IsNotEmpty(card.Combat);
                Assert.AreEqual(number == 6 ? 7 : 3, card.Initiative.Single());
            }
        }
    }
}
