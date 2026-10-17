using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-142: the game has no double-click. Real engine presses show that every click is delivered as a click, and that a
    // fast click continuing a series after the decision changed is suppressed.
    public sealed class EngineClickTests
    {
        [UnityTest]
        public IEnumerator EngineCountsConsecutiveMousePressesAcrossDifferentControls()
        {
            using var client = new MountedMatchClient("http://localhost", "engine-clicks");
            var surface = Surface(client);
            var clicks = 0;
            var first = new Button(() => clicks++) { text = "First" };
            var second = new Button(() => clicks++) { text = "Second" };
            surface.Add(first);
            surface.Add(second);
            yield return null;
            yield return null;
            using var mouse = new EngineMouseInput(surface);
            mouse.Click(first);
            mouse.Click(second);
            mouse.Click(second, Vector2.right * 2);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, mouse.PressCounts,
                "The engine's mouse counter belongs to the device, not the button.");
            Assert.AreEqual(3, clicks);
            Assert.AreEqual(3, mouse.GeneratedClickCounts.Count, "UITK must generate actual ClickEvents from these presses.");
            surface.RemoveFromHierarchy();
        }

        [UnityTest]
        public IEnumerator RapidClicksOnOneCardAreEachDeliveredAsAClick()
        {
            using var client = new MountedMatchClient("http://localhost", "engine-draft-clicks");
            var surface = Surface(client);
            var clicks = 0;
            var neutral = new Button { text = "Cards" };
            var card = new Button(() => clicks++)
            {
                text = "Card",
                userData = new DraftToken(DraftObject.Card, "own-card")
            };
            surface.Add(neutral);
            surface.Add(card);
            using var input = new DraftInput(surface, () => { }, () => { }, (_, _) => false, () => "match/revision/decision/connection");
            yield return null;
            yield return null;
            using var mouse = new EngineMouseInput(surface);
            mouse.Click(neutral);
            mouse.Click(card);
            mouse.Click(card, Vector2.right * 2);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, mouse.PressCounts);
            Assert.AreEqual(2, clicks, "Both presses on the card reach it as clicks; no hidden double-click gesture takes the second.");
            surface.RemoveFromHierarchy();
        }

        [UnityTest]
        public IEnumerator RapidClicksCannotActivateANewDecisionEvenWithoutAToken()
        {
            using var client = new MountedMatchClient("http://localhost", "engine-scope-clicks");
            var surface = Surface(client);
            var scope = "decision-a";
            var commands = 0;
            var first = new Button(() => { commands++; scope = "decision-b"; }) { text = "Complete decision" };
            var next = new Button(() => commands++) { text = "Deliberate next target" };
            surface.Add(first);
            surface.Add(next);
            using var input = new DraftInput(surface, () => { }, () => { }, (_, _) => false, () => scope);
            yield return null;
            yield return null;
            using var mouse = new EngineMouseInput(surface);
            mouse.Click(first);
            mouse.Click(first, Vector2.right);
            mouse.Click(first, Vector2.right * 2);
            Assert.AreEqual(1, commands, "A fast second or third click cannot submit the next decision under the same pointer.");
            mouse.Click(next);
            Assert.AreEqual(2, commands, "A deliberate move to a different control starts a fresh gesture.");
            surface.RemoveFromHierarchy();
        }

        [UnityTest]
        public IEnumerator ADiscardedCardCannotTurnTheNextClicksIntoTheReplacementsDiscard()
        {
            using var client = new MountedMatchClient("http://localhost", "engine-replacement-clicks");
            var surface = Surface(client);
            var scope = "discard-eight";
            var discarded = 0;
            var other = 0;
            Button slot = null;
            slot = new Button(() =>
            {
                discarded++;
                scope = "discard-seven";
                slot.userData = new DraftToken(DraftObject.Card, "b");
            }) { text = "Card A", userData = new DraftToken(DraftObject.Card, "a") };
            var next = new Button(() => other++) { text = "Card C", userData = new DraftToken(DraftObject.Card, "c") };
            surface.Add(slot);
            surface.Add(next);
            using var input = new DraftInput(surface, () => { }, () => { }, (_, _) => false, () => scope);
            yield return null;
            yield return null;
            using var mouse = new EngineMouseInput(surface);
            mouse.Click(slot);
            mouse.Click(slot);
            mouse.Click(slot);
            Assert.AreEqual(1, discarded, "The replacement card under the following fast clicks is not a new user choice.");
            mouse.Click(next);
            Assert.AreEqual(1, other, "Moving to another card is a fresh click at once.");
            surface.RemoveFromHierarchy();
        }

        [UnityTest]
        public IEnumerator RepeatedFigureClicksKeepLocalCyclesAndChangedScopeCancelsAPressedPair()
        {
            using var client = new MountedMatchClient("http://localhost", "engine-local-cycles");
            var surface = Surface(client);
            var scope = "losses";
            var cycles = 0;
            var figure = new Button(() => cycles++) { text = "Elite", userData = new DraftToken(DraftObject.Figure, "elite") };
            surface.Add(figure);
            using var input = new DraftInput(surface, () => { }, () => { }, (_, _) => false, () => scope);
            yield return null;
            yield return null;
            using var mouse = new EngineMouseInput(surface);
            mouse.Click(figure);
            figure.userData = new DraftToken(DraftObject.Figure, "elite");
            mouse.Click(figure, Vector2.right);
            mouse.Click(figure, Vector2.right * 2);
            Assert.AreEqual(3, cycles, "Repeated clicks still cycle a local elite loss or reroll selection.");
            mouse.Press(figure);
            scope = "new-connection";
            mouse.Release();
            Assert.AreEqual(3, cycles, "A reconnect between press and release invalidates the gesture.");
            Assert.IsFalse(figure.HasPointerCapture(PointerId.mousePointerId));
            mouse.Click(figure);
            Assert.AreEqual(4, cycles, "The canceled press must not suppress a new first click in the updated scope.");
            mouse.Click(figure, Vector2.right);
            Assert.AreEqual(5, cycles, "The next fast click counts without waiting for the device click counter to reset.");
            surface.RemoveFromHierarchy();
        }

        [UnityTest]
        public IEnumerator MovingOutAndBackDuringAPressCancelsThatClickOnly()
        {
            using var client = new MountedMatchClient("http://localhost", "engine-cancel-repeat");
            var surface = Surface(client);
            var clicks = 0;
            var card = new Button(() => clicks++) { text = "Card", userData = new DraftToken(DraftObject.Card, "own-card") };
            surface.Add(card);
            using var input = new DraftInput(surface, () => { }, () => { }, (_, _) => false, () => "discard");
            yield return null;
            yield return null;
            using var mouse = new EngineMouseInput(surface);
            mouse.Click(card);
            mouse.Press(card);
            var origin = card.worldBound.center;
            mouse.Move(origin + Vector2.right * 20);
            mouse.Move(origin);
            mouse.Release();
            Assert.AreEqual(1, clicks, "Crossing the drag threshold turns the press into a drag, even after returning to its origin.");
            mouse.Click(card, Vector2.right);
            Assert.AreEqual(2, clicks, "The next click works without recreating the UI.");
            surface.RemoveFromHierarchy();
        }

        [UnityTest]
        public IEnumerator ASelectedFigureCanStartADragOnTheNextNativePress()
        {
            using var client = new MountedMatchClient("http://localhost", "engine-click-then-drag");
            var surface = Surface(client);
            var selected = 0;
            var drops = 0;
            // UI-105: figures and armies are never dragged, so a reserve stands for the draggable object.
            var figure = new Button(() => selected++)
            {
                text = "Reserve",
                userData = new DraftToken(DraftObject.Reserve, "traveler")
            };
            var destination = new Button { text = "Region", userData = new DraftToken(DraftObject.Region, "destination") };
            surface.Add(figure);
            surface.Add(destination);
            using var input = new DraftInput(surface, () => { }, () => { }, (source, target) =>
            {
                Assert.AreEqual("traveler", source.Id);
                Assert.AreEqual("destination", target.Id);
                drops++;
                return true;
            }, () => "unchanged-decision");
            yield return null;
            yield return null;
            using var mouse = new EngineMouseInput(surface);
            mouse.Click(figure);
            mouse.Press(figure);
            CollectionAssert.AreEqual(new[] { 1, 2 }, mouse.PressCounts);
            mouse.Move(figure.worldBound.center + Vector2.right * 9);
            Assert.IsTrue(surface.HasPointerCapture(PointerId.mousePointerId),
                "A native second press crossing the movement threshold must start a real drag.");
            mouse.Move(destination.worldBound.center);
            mouse.Release();
            Assert.AreEqual(1, selected, "Starting the drag keeps the selection from the first click.");
            Assert.AreEqual(1, drops);
            Assert.IsFalse(surface.HasPointerCapture(PointerId.mousePointerId));
            surface.RemoveFromHierarchy();
        }

        [UnityTest]
        public IEnumerator AChangedActionUnderTheSamePointerIsAnotherClick()
        {
            using var client = new MountedMatchClient("http://localhost", "engine-action-identity");
            var surface = Surface(client);
            var selected = 0;
            var button = new Button(() => selected++)
            {
                text = "Action",
                userData = new DraftToken(DraftObject.Action, action: CommandKind.MoveArmies)
            };
            surface.Add(button);
            using var input = new DraftInput(surface, () => { }, () => { }, (_, _) => false, () => "same-decision");
            yield return null;
            yield return null;
            using var mouse = new EngineMouseInput(surface);
            mouse.Click(button);
            button.userData = new DraftToken(DraftObject.Action, action: CommandKind.Recruit);
            mouse.Click(button);
            Assert.AreEqual(2, selected, "Each click within one decision reaches its control.");
            surface.RemoveFromHierarchy();
        }

        private static VisualElement Surface(MountedMatchClient client)
        {
            var surface = new VisualElement { name = "engine-click-surface" };
            surface.style.position = Position.Absolute;
            surface.style.left = 100;
            surface.style.top = 100;
            surface.style.width = 260;
            surface.style.height = 180;
            client.Board.Root.parent.Add(surface);
            return surface;
        }
    }
}
