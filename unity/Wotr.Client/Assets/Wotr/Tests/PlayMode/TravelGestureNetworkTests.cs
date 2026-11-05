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
            // UI-402: the selection lights each character's own miniature, never its region's army.
            Assert.IsFalse(Selected(room, "base.character.gimli"));
            Assert.IsTrue(Selected(room, "base.character.legolas"));
            Assert.IsFalse(room.Fp.Board.Map.Q<Button>("army-dale-FreePeoples").ClassListContains("selected-force"));
            // UI-402: Gimli's own miniature on the map chooses him again exactly as his chip did; it is lit as the chip is.
            await Miniature(room, "hollin", "base.character.gimli");
            await room.Settle();
            // UI-105: the destination click moves the selected group at once. UI-405: so does carrying Gimli's own figure
            // onto the destination and dropping it there — the drop is the destination's click.
            Assert.IsTrue(room.Fp.Board.Map.DraftOverlay.Targets.Contains("moria"), "Reachable destinations light up.");
            var revision = room.Fp.Board.View.Revision;
            var map = room.Fp.Board.Map;
            var moria = MapContourProbe.ReachRegion(map, "moria");
            await room.Settle();
            MapContourProbe.DragFigure(room.Fp.Board, map.Q<Button>("character-base.character.gimli"), moria);
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

        // UI-402: a character's miniature on the map, clicked with a Character die, chooses it as its panel chip does: it is
        // lit as the chip is, the click opens no region panel and the figure shows the selection. (One test per seeded
        // scenario: a second one would find Gimli moved.)
        private static async Task Miniature(MatchTestRoom room, string region, string id)
        {
            var board = room.Fp.Board;
            board.Hover.Close();
            await room.Settle();
            var figure = board.Map.Q<Button>("character-" + id);
            await MapContourProbe.ReachMarker(board.Map, figure, region);
            Assert.IsTrue(figure.ClassListContains(MapCharacterArtwork.EligibleClass), "The die lights the figure as it lights its chip.");
            LayoutTests.Save(room.Fp.Viewport, "ui402-character-travel-lit.png");
            PointerGestureEvents.ClickAt(figure);
            await room.Settle();
            Assert.IsTrue(Selected(room, id), "Its figure shows the selection.");
            Assert.AreEqual(DisplayStyle.None, board.Hover.Panel.Root.resolvedStyle.display, "A character's click opens no region panel.");
        }

        private static bool Selected(MatchTestRoom room, string id) =>
            room.Fp.Board.Map.Q<Button>("character-" + id).ClassListContains(MapCharacterArtwork.SelectedClass);

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
