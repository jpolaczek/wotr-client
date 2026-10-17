using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class TravelGestureNetworkTests
    {
        [UnityTest]
        public IEnumerator FigureAndDestinationClicksSaveOneGroupAndFinishKeepsTheExactPaidDie() => Wait(Travel());

        private static async Task Travel()
        {
            using var room = await Connect("travel-drag", ScenarioKind.Character);
            FellowshipGestures.Die(room.Fp);
            var die = room.Fp.Board.ActionDie.DieId;
            room.Fp.Board.Map.Focus("hollin");
            FellowshipGestures.Figure(room.Fp, "hollin", "base.character.gimli");
            FellowshipGestures.Figure(room.Fp, "dale", "base.character.legolas");
            Assert.IsFalse(room.Fp.Board.Map.Q<Button>("army-hollin-FreePeoples").ClassListContains("selected-force"));
            Assert.IsTrue(room.Fp.Board.Map.Q<Button>("army-dale-FreePeoples").ClassListContains("selected-force"));
            FellowshipGestures.Figure(room.Fp, "hollin", "base.character.gimli");
            await room.Settle();
            // UI-105: the destination click moves the selected group at once; a figure is never dragged.
            Assert.IsTrue(room.Fp.Board.Map.DraftOverlay.Targets.Contains("moria"), "Reachable destinations light up.");
            var revision = room.Fp.Board.View.Revision;
            FellowshipGestures.FinishRoute(room.Fp, "moria");
            await room.At(revision + 1);
            Assert.AreEqual(1, room.Fp.Board.View.ActionSequence.Remaining);
            Assert.AreEqual(die, room.Fp.Board.View.ActionSequence.DieId);
            Assert.AreEqual("moria", room.Sh.Board.View.Characters.Single(c => c.Id == "base.character.gimli").Location);
            var connection = room.Fp.Connected;
            room.Fp.Connection.Reconnect();
            await room.Until(() => room.Fp.Connected > connection);
            await room.Settle();
            room.Fp.Board.Hover.Open("moria");
            await room.Settle();
            var saved = room.Fp.Board.Hover.Panel.Root.Q<Button>("hover-figure-base.character.gimli");
            Assert.IsFalse(saved.ClassListContains("eligible-figure"));
            CardNetworkTests.Click(saved);
            Assert.IsFalse(saved.ClassListContains("selected"));
            Assert.AreEqual(1, room.Fp.Board.View.Revision);
            await ArmyGestures.Finish(room, room.Fp);
            T22NetworkActions.Spent(room, Side.FreePeoples, die);
            Assert.AreEqual("dale", room.Sh.Board.View.Characters.Single(c => c.Id == "base.character.legolas").Location);
        }

        [UnityTest]
        public IEnumerator FellowshipPortraitAndDestinationClicksUseTheSelectedCostAndKeepTheGuideWindow() => Wait(Separate());

        private static async Task Separate()
        {
            using var room = await Connect("separate-drag", ScenarioKind.Character);
            FellowshipGestures.Die(room.Fp);
            var die = room.Fp.Board.ActionDie.DieId;
            room.Fp.Board.Map.Focus(room.Fp.Board.View.Fellowship.RegionId);
            FellowshipGestures.Portrait(room.Fp, "base.character.gimli");
            MapContourProbe.ReachRegion(room.Fp.Board.Map, "fords-of-bruinen");
            await room.Settle();
            var revision = room.Fp.Board.View.Revision;
            FellowshipGestures.FinishRoute(room.Fp, "fords-of-bruinen");
            await room.At(revision + 1);
            Assert.AreEqual(DecisionKind.Guide, room.Fp.Board.View.Decision.Kind);
            Assert.AreEqual(DieLocation.Resolving, room.Fp.Board.View.Dice.Single(d => d.Id == die).Location);
            Assert.AreEqual("fords-of-bruinen", room.Sh.Board.View.Characters.Single(c => c.Id == "base.character.gimli").Location);
            FellowshipGestures.Portrait(room.Fp, "base.character.gandalf-grey");
            await room.At(2);
            T22NetworkActions.Spent(room, Side.FreePeoples, die);
        }

        [UnityTest]
        public IEnumerator FellowshipRequiresClickAndStaysOpenAfterExitWithoutMovingTheCamera()
        {
            using var client = new MountedMatchClient("http://localhost", "fellowship-hover");
            var view = new BoardFixture().View;
            client.Apply(view);
            for (var frame = 0; frame < 6; frame++)
                yield return null;
            var board = client.Board;
            var pan = board.Map.Camera.Pan;
            var zoom = board.Map.Camera.Zoom;
            Assert.IsFalse(board.Fellowship.Pinned);
            CardGestures.Move(board.Edges.Corner);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.IsTrue(board.Edges.CornerPanels.ClassListContains(LeftRailPreview.PreviewClass));
            Assert.Less(Vector2.Distance(pan, board.Map.Camera.Pan), .001f, "Hover changed camera: " + pan.ToString("R") +
                " -> " + board.Map.Camera.Pan.ToString("R"));
            Assert.IsTrue(board.Fellowship.IsOpen);
            Assert.Less(Vector2.Distance(pan, board.Map.Camera.Pan), .001f, "Click changed camera: " + pan.ToString("R") +
                " -> " + board.Map.Camera.Pan.ToString("R"));
            Assert.AreEqual(zoom, board.Map.Camera.Zoom);
            Assert.AreEqual(0, board.View.Revision);
            Move(board.Fellowship.Root.Q<Button>("focus-fellowship"));
            yield return new WaitForSecondsRealtime(.2f);
            Assert.IsTrue(board.Fellowship.IsOpen, "Moving from the status into its panel must keep the inspection open.");
            Move(board.Phase);
            yield return new WaitForSecondsRealtime(.42f);
            Assert.IsFalse(board.Fellowship.IsOpen, "Leaving the hover preview rolls it back.");
            Assert.Less(Vector2.Distance(pan, board.Map.Camera.Pan), .001f);
            client.Apply(view);
            yield return null;
            Assert.Less(Vector2.Distance(pan, board.Map.Camera.Pan), .001f);
            board.Fellowship.Close();
            for (var frame = 0; frame < 3; frame++)
                yield return null;
            var selectedRegion = board.Map.Selected;
            Assert.AreEqual(DisplayStyle.None, board.Map.FellowshipMarker.Root.resolvedStyle.display,
                "At fit zoom, closing inspection hides figures (UI-30).");
            yield return Wait(MapContourProbe.ReachMarker(board.Map, board.Map.FellowshipMarker.Root,
                view.Fellowship.RegionId));
            Assert.AreEqual(DisplayStyle.Flex, board.Map.FellowshipMarker.Root.resolvedStyle.display);
            var markerPan = board.Map.Camera.Pan;
            var markerZoom = board.Map.Camera.Zoom;
            Move(board.Map.FellowshipMarker.Root);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.IsFalse(board.Fellowship.Pinned);
            Assert.AreEqual(DisplayStyle.None, board.Hover.Panel.Root.resolvedStyle.display);
            PointerGestureEvents.ClickAt(board.Map.FellowshipMarker.Root);
            Assert.IsTrue(board.Fellowship.IsOpen);
            Assert.AreEqual(selectedRegion, board.Map.Selected, "Opening the Fellowship marker must not select the region below it.");
            Assert.Less(Vector2.Distance(markerPan, board.Map.Camera.Pan), .001f);
            Assert.AreEqual(markerZoom, board.Map.Camera.Zoom);
        }

        private static void Move(VisualElement target)
        {
            using var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = target.worldBound.center });
            target.SendEvent(move);
        }
    }
}
