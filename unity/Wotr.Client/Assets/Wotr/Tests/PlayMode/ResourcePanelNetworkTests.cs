using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;
using static Wotr.PlayTests.KingsCardNetworkChecks;

namespace Wotr.PlayTests
{
    public sealed class ResourcePanelNetworkTests
    {
        [UnityTest]
        public IEnumerator SpecialChoicesAndBothDiscardsAreMadeOnThePoolDiceWithoutAPanel() => Wait(Choices());

        [UnityTest]
        public IEnumerator DismayTargetsTheOpponentsRetainedRulerAfterClosingAndReconnect() => Wait(Dismay());

        [UnityTest]
        public IEnumerator RulerTokenDropKeepsItsIdentityThroughBothDenethorDraws() => Wait(Ruler());

        [UnityTest]
        public IEnumerator RingCommitsOnlyTheNewFaceAndLeavesTheChangedDieUnused() => Wait(Ring());

        [UnityTest]
        public IEnumerator DismayCanFinishBeforeLoreWithoutLosingTheExactDieOrClosingTheNewDecision() => Wait(DismayThenLore());

        private static async Task DismayThenLore()
        {
            using var room = await Connect("ui58-dismay-before-lore", ScenarioKind.CardPack);
            var actor = room.Sh;
            var option = actor.Board.View.Options.Single(o => o.Kind == CommandKind.UseDismay);
            var token = option.EligibleIds.Single();
            var ruler = actor.Board.View.Dice.Single(d => d.Owner == Side.Shadow && option.EligibleDice.Contains(d.Id));
            var revision = actor.Board.View.Revision;
            PointerGestureEvents.ClickAt(actor.Board.OwnDice.Q<Button>("ruler-token-" + token));
            await room.Settle();
            PointerGestureEvents.ClickAt(actor.Board.OwnDice.Q<Button>(ruler.Id));
            await room.At(revision + 1);
            Assert.AreEqual("removed", actor.Board.View.RulerTokens.Single(t => t.Id == token).Location);
            Assert.AreEqual("DieAndResult", actor.Board.View.Decision.Presentation);
            Assert.IsTrue(actor.Board.ResourceDecision.IsOpen);
            Assert.AreSame(actor.Board.ResourceDecision.DecisionHost, actor.Board.Tray.parent);
            PointerGestureEvents.ClickAt(actor.Board.Tray.Q<Button>("effect-choice-" + ruler.Id));
            await room.Settle();
            var face = actor.Board.View.Dice.Single(d => d.Id == ruler.Id).Face == DieFace.Muster
                ? DieFace.Character : DieFace.Muster;
            PointerGestureEvents.ClickAt(actor.Board.Tray.Q<Button>("effect-choice-lore." + face + ".keep"));
            await room.Settle();
            actor.Board.ResourceDecision.Close();
            await Reconnect(room, actor);
            Assert.IsFalse(actor.Board.ResourceDecision.IsOpen);
            PointerGestureEvents.ClickAt(actor.Board.ResourceDecision.Tab);
            await room.Settle();
            await room.Confirm(actor);
            Assert.AreEqual(revision + 2, actor.Board.View.Revision);
            Assert.AreEqual(face, room.Fp.Board.View.Dice.Single(d => d.Id == ruler.Id).Face);
            Assert.AreEqual(DieLocation.Available, room.Fp.Board.View.Dice.Single(d => d.Id == ruler.Id).Location);
            Assert.AreEqual("removed", room.Fp.Board.View.RulerTokens.Single(t => t.Id == token).Location);
            Assert.AreEqual(DecisionKind.Action, room.Fp.Board.View.Decision.Kind);
        }

        // UI-264: the special-die pick and the Ruler discard are made on the lit dice in the pool; neither the
        // 'Dice & tokens' panel nor the decision window opens, and the console says what the click does.
        private static async Task Choices()
        {
            using var room = await Connect("ui09-dice", ScenarioKind.CardPack);
            foreach (var kind in new[] { DecisionKind.SpecialDieChoice, DecisionKind.RulerDiscard })
                foreach (var actor in new[] { room.Sh, room.Fp })
                {
                    Assert.AreEqual(kind, actor.Board.View.Decision.Kind);
                    Assert.IsTrue(RequiredDecisions.Waits(actor.Board.View));
                    var before = actor.Board.View.Revision;
                    var id = actor.Board.View.Options.Single().EligibleDice.First();
                    await Reconnect(room, actor);
                    Assert.IsFalse(actor.Board.ResourceDecision.IsOpen, "The pool's decision opens no panel.");
                    Assert.AreNotSame(actor.Board.ResourceDecision.DecisionHost, actor.Board.Tray.parent);
                    Assert.IsFalse(actor.Board.DecisionWindow.IsOpen, "The pool's decision opens no window.");
                    var opponent = actor == room.Fp ? room.Sh : room.Fp;
                    Assert.IsFalse(opponent.Board.ResourceDecision.IsOpen);
                    Assert.IsTrue(actor.Board.OwnDice.Q<Button>(id).ClassListContains("resource-candidate"));
                    Assert.IsNull(actor.Board.Tray.Q<Button>("cost-" + id), "Choose the actual die in its pool, without a duplicate picker.");
                    Assert.IsNull(actor.Board.Tray.Q<Button>("confirm-decision"));
                    Assert.IsNull(actor.Board.Tray.Q<Button>("decision-back"), "A single pool choice has nothing to go back to.");
                    StringAssert.StartsWith("Click a highlighted die", actor.Board.Tray.Q<Label>(className: "draft-status").text);
                    await Layout(room, actor, kind + "-" + actor.Board.View.You, "ui58");
                    PointerGestureEvents.ClickAt(actor.Board.OwnDice.Q<Button>(id));
                    await room.At(before + 1);
                    Assert.AreEqual(kind == DecisionKind.SpecialDieChoice ? DieLocation.Hunt : DieLocation.Discarded,
                        actor.Board.View.Dice.Single(d => d.Id == id).Location);
                }
        }

        private static async Task Dismay()
        {
            using var room = await Connect("ui09-dismay", ScenarioKind.CardPack);
            var actor = room.Sh;
            var option = actor.Board.View.Options.Single(o => o.Kind == CommandKind.UseDismay);
            var die = actor.Board.View.Dice.Single(d => d.Owner == Side.FreePeoples && option.EligibleDice.Contains(d.Id));
            var token = option.EligibleIds[0];
            var revision = actor.Board.View.Revision;
            PointerGestureEvents.ClickAt(actor.Board.OwnDice.Q<Button>("ruler-token-" + token));
            await room.Settle();
            actor.Board.ResourceDecision.Close();
            await Reconnect(room, actor);
            Assert.IsFalse(actor.Board.ResourceDecision.IsOpen);
            Assert.IsTrue(actor.Board.OwnDice.Q<Button>("ruler-token-" + token).ClassListContains("selected"));
            Assert.IsTrue(actor.Board.OpponentDice.Q<Button>(die.Id).ClassListContains("resource-candidate"));
            PointerGestureEvents.ClickAt(actor.Board.ResourceDecision.Tab);
            await room.Settle();
            PointerGestureEvents.ClickAt(actor.Board.OpponentDice.Q<Button>(die.Id));
            await room.At(revision + 1);
            Assert.AreEqual("removed", actor.Board.View.RulerTokens.Single(t => t.Id == token).Location);
            Assert.AreEqual(Side.FreePeoples, actor.Board.View.Dice.Single(d => d.Id == die.Id).Owner);
        }

        private static async Task Ruler()
        {
            using var room = await Connect("ui09-ruler-draw", ScenarioKind.CardPack);
            var actor = room.Fp;
            var ruler = actor.Board.View.Dice.Single(d => d.Owner == Side.FreePeoples && d.Source == "ruler");
            const string token = "kings.token.denethor";
            var before = actor.Board.View.Revision;
            await CardGestureNetworkTests.Drop(room, actor, actor.Board.OwnDice.Q<Button>("ruler-token-" + token),
                actor.Board.OwnDice.Q<Button>(ruler.Id), false);
            Assert.AreEqual(before, actor.Board.View.Revision);
            Assert.IsTrue(actor.Board.OwnDice.Q<Button>(ruler.Id).ClassListContains("selected"));
            Assert.IsTrue(actor.Board.OwnDice.Q<Button>("ruler-token-" + token).ClassListContains("selected"));
            await Reconnect(room, actor);
            await CardGestures.Draw(room, actor, "character");
            Assert.AreEqual("dismay", actor.Board.View.RulerTokens.Single(t => t.Id == token).Location);
            Assert.AreEqual(DieLocation.Resolving, actor.Board.View.Dice.Single(d => d.Id == ruler.Id).Location);
            await Reconnect(room, actor);
            await CardGestures.Draw(room, actor, "strategy");
            Assert.AreEqual(DieLocation.Spent, actor.Board.View.Dice.Single(d => d.Id == ruler.Id).Location);
            Assert.AreEqual(before + 2, actor.Board.View.Revision);
        }

        private static async Task Ring()
        {
            using var room = await Connect("turn-ring-politics", ScenarioKind.BaseAcceptance);
            var actor = room.Fp;
            var view = actor.Board.View;
            var die = view.Dice.Single(d => d.Owner == Side.FreePeoples && d.Face == DieFace.Army);
            var ring = view.Rings.First(r => r.Owner == Side.FreePeoples).Id;
            var revision = view.Revision;
            // UI-161: the base ring is used at its socket, the die and the result popover, without the resource
            // panel; a reconnect restores the popover from the saved draft.
            PointerGestureEvents.ClickAt(actor.Board.Console.Rings.For(ring));
            await room.Settle();
            Assert.IsFalse(actor.Board.ResourceDecision.IsOpen);
            PointerGestureEvents.ClickAt(actor.Board.OwnDice.Q<Button>(die.Id));
            await room.Settle();
            Assert.IsTrue(actor.Board.RingPicker.IsOpen);
            await Reconnect(room, actor);
            await room.Until(() => actor.Board.RingPicker.IsOpen);
            Assert.IsFalse(actor.Board.ResourceDecision.IsOpen);
            PointerGestureEvents.ClickAt(actor.Board.RingPicker.Root.Q<Button>("ring-face-muster"));
            await room.At(revision + 1);
            Assert.AreEqual(DieFace.Muster, actor.Board.View.Dice.Single(d => d.Id == die.Id).Face);
            Assert.AreEqual(DieLocation.Available, actor.Board.View.Dice.Single(d => d.Id == die.Id).Location);
            Assert.AreEqual(Side.Shadow, actor.Board.View.Rings.Single(r => r.Id == ring).Owner);
            Assert.AreEqual(Side.FreePeoples, actor.Board.View.DecisionOwner);
            Assert.IsFalse(actor.Board.ResourceDecision.IsOpen);
        }
    }
}
