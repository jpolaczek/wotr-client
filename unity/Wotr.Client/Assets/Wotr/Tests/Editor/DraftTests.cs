using NUnit.Framework;
using Newtonsoft.Json;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class DraftTests
    {
        [Test]
        public void ClickAndDragChooseTheSameCostAndIntentionWithoutCommitting()
        {
            var view = CostView();
            var click = new TurnDraft();
            click.Change(p => p.DieId = "event-b");
            DraftGesture.Choose(click, view, CommandKind.DrawCard);
            click.Change(p => p.Deck = "character");
            var drag = new TurnDraft();
            Assert.IsTrue(DraftGesture.Drop(drag, view, new DraftToken(DraftObject.Die, "event-b"),
                new DraftToken(DraftObject.Deck, "character", CommandKind.DrawCard)));
            Assert.AreEqual(click.Kind, drag.Kind);
            Assert.AreEqual(JsonConvert.SerializeObject(click.Payload), JsonConvert.SerializeObject(drag.Payload));
            Assert.AreEqual(DieLocation.Available, view.Dice[1].Location);
        }

        [Test]
        public void IllegalDropLeavesEverySelectionUntouched()
        {
            var view = CostView();
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, CommandKind.DrawCard);
            draft.Change(p => p.Deck = "character");
            var before = JsonConvert.SerializeObject(draft.Payload);
            Assert.IsFalse(DraftGesture.Drop(draft, view, new DraftToken(DraftObject.Die, "opponent"),
                new DraftToken(DraftObject.Deck, "strategy", CommandKind.DrawCard)));
            Assert.AreEqual(before, JsonConvert.SerializeObject(draft.Payload));
        }

        [Test]
        public void DefaultCostRequiresOnlyEquivalentOrdinaryDice()
        {
            var view = CostView();
            Assert.AreEqual("event-a", DraftGesture.EquivalentDie(view, view.Options[0]));
            view.Dice[1].Face = DieFace.WillOfTheWest;
            Assert.IsEmpty(DraftGesture.EquivalentDie(view, view.Options[0]));
            view.Dice[1].Face = DieFace.Event;
            view.Dice[1].Source = "keeper";
            Assert.IsEmpty(DraftGesture.EquivalentDie(view, view.Options[0]));
            view.Dice.RemoveAt(1);
            Assert.IsEmpty(DraftGesture.EquivalentDie(view, view.Options[0]), "An incomplete view must not guess a cost.");
        }

        // UI-405: a carried figure's gesture that ends without a step leaves the draft exactly as it was, undo history too.
        [Test]
        public void ACheckpointPutsBackTheDraftAndItsUndoHistoryExactly()
        {
            var draft = new TurnDraft();
            draft.Choose(CommandKind.MoveArmies, "army");
            draft.Change(p => p.Stepwise = true);
            var before = JsonConvert.SerializeObject(draft.Payload);
            var checkpoint = draft.Checkpoint();
            draft.Gesture(() => draft.Change(p => p.Moves.Add(new ArmyMoveChoice { From = "minas-tirith" })));
            draft.Rewind(checkpoint);
            Assert.AreEqual(before, JsonConvert.SerializeObject(draft.Payload), "The pre-press draft is back.");
            draft.Change(p => p.Moves.Add(new ArmyMoveChoice { From = "dale" }));
            Assert.IsEmpty(checkpoint.Payload.Moves, "The draft never shares the checkpoint's payload.");
            draft.Rewind(checkpoint);
            Assert.AreEqual(before, JsonConvert.SerializeObject(draft.Payload), "A checkpoint can be put back again.");
            draft.Back();
            Assert.AreEqual(CommandKind.MoveArmies, draft.Kind);
            Assert.IsFalse(draft.Payload.Stepwise, "Undo steps over what the draft did before the press, nothing more.");
            draft.Back();
            Assert.IsNull(draft.Kind, "The history is the pre-press one: nothing the gesture did is remembered.");
        }

        [Test]
        public void BackUndoesEachStepAndClearsTheIntentAtTheBeginning()
        {
            var draft = new TurnDraft();
            draft.Choose(CommandKind.UseRing);
            draft.Change(p => p.RingId = "ring");
            draft.Change(p => p.DieId = "event");
            draft.Change(p => p.Face = DieFace.Army);
            draft.Back();
            Assert.IsNull(draft.Payload.Face);
            Assert.AreEqual("event", draft.Payload.DieId);
            draft.Back();
            Assert.IsEmpty(draft.Payload.DieId);
            Assert.AreEqual("ring", draft.Payload.RingId);
            draft.Back();
            Assert.IsEmpty(draft.Payload.RingId);
            draft.Back();
            Assert.IsNull(draft.Kind);
        }

        private static PlayerView CostView() => new PlayerView
        {
            You = Side.FreePeoples,
            Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action },
            Options = { new ActionOption { Kind = CommandKind.DrawCard, EligibleDice = { "event-a", "event-b" }, Targets = { "character", "strategy" } } },
            Dice =
            {
                new DieView { Id = "event-a", Source = "standard", Face = DieFace.Event, Location = DieLocation.Available },
                new DieView { Id = "event-b", Source = "standard", Face = DieFace.Event, Location = DieLocation.Available }
            }
        };

        [Test]
        public void ReconnectRetainsLegalChoiceButNeverSubstitutesAnotherDie()
        {
            var draft = new TurnDraft();
            draft.Choose(CommandKind.DrawCard);
            draft.Payload.DieId = "wild";
            draft.Payload.Deck = "character";
            var view = new PlayerView();
            view.Options.Add(new ActionOption { Kind = CommandKind.DrawCard, EligibleDice = { "wild", "event" }, Targets = { "character" } });
            draft.Reconcile(view);
            Assert.AreEqual("wild", draft.Payload.DieId);
            view.Options[0].EligibleDice.Remove("wild");
            draft.Reconcile(view);
            Assert.AreEqual("", draft.Payload.DieId);
            Assert.AreEqual("character", draft.Payload.Deck);
            Assert.IsFalse(draft.Ready(view.Options[0]));
        }

        [Test]
        public void VisualSelectionCanBeUndoneWithoutChangingServerView()
        {
            var view = new PlayerView { Hand = { "one", "two", "three" } };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.DiscardCards);
            draft.Toggle("one");
            draft.Toggle("two");
            draft.Toggle("one");
            CollectionAssert.AreEqual(new[] { "two" }, draft.Payload.Ids);
            CollectionAssert.AreEqual(new[] { "one", "two", "three" }, view.Hand);
            draft.Clear();
            Assert.IsEmpty(draft.Payload.Ids);
        }
    }
}
