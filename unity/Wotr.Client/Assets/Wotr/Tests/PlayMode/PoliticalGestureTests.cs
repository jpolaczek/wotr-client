using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    public sealed class PoliticalGestureTests
    {
        [UnityTest]
        public IEnumerator UnavailableTokenClicksStayInTheDrawerWithoutChoosingACost()
        {
            using var client = new MountedMatchClient("http://localhost", "politics-unavailable");
            client.Apply(Fixture().View);
            yield return Settle();
            var board = client.Board;
            board.Nations.Open();
            yield return Settle();
            var camera = board.Map.Camera.Pan;
            foreach (var id in new[] { "north", "sauron", "gondor" })
            {
                CardNetworkTests.Click(Token(board, id));
                yield return Settle();
                Assert.AreEqual(DisplayStyle.Flex, board.Nations.Drawer.resolvedStyle.display);
                Assert.AreEqual(DisplayStyle.None, board.Information.Root.resolvedStyle.display);
                Assert.IsEmpty(board.ActionDie.DieId);
                Assert.IsEmpty(board.Nations.Drawer.Query<Button>(className: "selected").ToList());
                AssertArrows(board);
            }
            ArmyGestures.Die(client, "army");
            board.Nations.Open();
            yield return Settle();
            CardNetworkTests.Click(Token(board, "north"));
            yield return Settle();
            Assert.AreEqual("army", board.ActionDie.DieId);
            Assert.AreEqual(DisplayStyle.None, board.Information.Root.resolvedStyle.display);
            AssertArrows(board);
            Assert.AreEqual(0, board.View.Revision);
            Assert.AreEqual(camera, board.Map.Camera.Pan);
        }

        [UnityTest]
        public IEnumerator DragHighlightsItsOwnDestinationAndCancellationKeepsEveryArrow()
        {
            using var client = new MountedMatchClient("http://localhost", "politics-drag");
            client.Apply(Fixture().View);
            yield return Settle();
            var board = client.Board;
            ArmyGestures.Die(client, "muster", DieFace.Muster);
            board.Nations.Open();
            yield return Settle();
            // UI-144: the die's legal steps show at once; no token needs a selecting click.
            AssertArrows(board, "political-slot-elves-1", "political-slot-north-2");
            AssertDestination(board, null);
            var elves = Token(board, "elves");
            var target = board.Nations.Drawer.Q<Button>("political-slot-elves-1");
            yield return StartDrag(elves, target);
            AssertDestination(board, "political-slot-elves-1");
            AssertArrows(board, "political-slot-elves-1", "political-slot-north-2");
            Assert.AreEqual(DisplayStyle.None, board.Information.Root.resolvedStyle.display);
            LayoutTests.Save(client.Viewport, "ui26-politics-drag.png");
            Cancel(board.Root);
            yield return Settle();
            Assert.AreEqual("muster", board.ActionDie.DieId, "Cancelling a drag must retain its chosen cost.");
            AssertDestination(board, null);
            AssertArrows(board, "political-slot-elves-1", "political-slot-north-2");
            Assert.IsFalse(elves.ClassListContains("selected"), "An uncommitted drag must not change the draft.");
            Assert.AreEqual(0, board.View.Revision);
        }

        private static BoardFixture Fixture()
        {
            var fixture = new BoardFixture();
            var north = fixture.View.Nations.Single(n => n.Id == "north");
            north.Active = false;
            north.StepsToWar = 3;
            fixture.Refresh();
            return fixture;
        }

        // UI-410 (owner): no station on the track glows; the next station stays a drop target and the nation's token glows.
        private static void AssertArrows(BoardScreen board, params string[] expected)
        {
            var slots = board.Nations.Drawer.Query<Button>(className: "nation-slot").ToList();
            Assert.IsFalse(slots.Any(b => b.ClassListContains("legal-target")), "No station on the track glows.");
            var targets = slots.Where(b => b.enabledSelf).ToList();
            CollectionAssert.AreEquivalent(expected, targets.Select(b => b.name));
            Assert.IsTrue(targets.All(b => b.enabledInHierarchy));
            foreach (var name in expected)
            {
                var nation = name.Split('-')[2];
                Assert.IsTrue(board.Nations.Drawer.Q<Button>("nation-status-" + nation).ClassListContains("legal-target"), nation + "'s token glows.");
            }
        }

        private static void AssertDestination(BoardScreen board, string expected)
        {
            var highlighted = board.Nations.Drawer.Query<Button>(className: "destination-selected").ToList();
            if (expected == null)
                Assert.IsEmpty(highlighted);
            else
                Assert.AreEqual(expected, highlighted.Single().name);
        }

        private static IEnumerator StartDrag(VisualElement source, VisualElement target)
        {
            PointerGestureEvents.Press(source);
            yield return Settle();
            using var move = PointerMoveEvent.GetPooled(new Event
            {
                type = EventType.MouseDrag, button = 0, mousePosition = target.worldBound.center
            });
            source.SendEvent(move);
            yield return Settle();
        }

        private static void Cancel(VisualElement root)
        {
            using (var down = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape }))
                root.SendEvent(down);
            using (var up = KeyUpEvent.GetPooled(new Event { type = EventType.KeyUp, keyCode = KeyCode.Escape }))
                root.SendEvent(up);
            PointerGestureEvents.Release(root);
        }

        private static Button Token(BoardScreen board, string id) => board.Nations.Drawer.Q<Button>("nation-status-" + id);

        private static IEnumerator Settle()
        {
            yield return null;
            yield return new WaitForSecondsRealtime(.06f);
        }
    }
}
