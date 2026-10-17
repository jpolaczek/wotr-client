using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    public sealed partial class MapViewportTests
    {
        [UnityTest]
        public IEnumerator ModalWindowsCaptureAndOneAxisLimitsKeepEdgeNavigationSafeAtFourLayouts() =>
            MatchTestRoom.Wait(EdgeLayouts(CheckEdgeSafety));

        private static async Task CheckEdgeSafety(UI10TestRoom room, BoardScreen board, float pixelsPerUnit)
        {
            Center(board.Map);
            await room.Settle();
            var client = room.Fp.Board == board ? room.Fp : room.Sh;
            CardNetworkTests.Click(board.Menu.Toggle);
            await room.Settle();
            Assert.IsTrue(board.Menu.IsOpen);
            await CheckModalEdges(room, board, board.Menu.Root, pixelsPerUnit);
            CardNetworkTests.Click(board.Menu.Root.Q<Button>("open-interface"));
            await room.Until(() => client.Interface.Settings.IsOpen);
            await room.Settle();
            await CheckModalEdges(room, board, client.Interface.Settings.Root, pixelsPerUnit);
            CardNetworkTests.Click(client.Interface.Settings.Root.Q<Button>("close-interface"));
            await room.Settle();

            board.Information.Region("minas-tirith");
            await room.Settle();
            var header = board.Information.Root.Q(className: FloatingWindow.HandleClass);
            Assert.IsNotNull(header);
            var headerPoint = new Vector2(header.worldBound.xMin + 8, header.worldBound.center.y);
            var pickedHeader = board.Map.panel.Pick(headerPoint);
            Assert.IsTrue(pickedHeader == header || header.Contains(pickedHeader));
            await CapturedPressBlocksReentry(room, board, pickedHeader, headerPoint, pixelsPerUnit);
            board.Information.Close();
            await room.Settle();

            var marker = board.Map.Q<Button>("army-minas-tirith-FreePeoples");
            await MapContourProbe.ReachMarker(board.Map, marker, "minas-tirith");
            await room.Settle();
            var markerPoint = marker.worldBound.center;
            var pickedMarker = board.Map.panel.Pick(markerPoint);
            Assert.IsTrue(pickedMarker == marker || marker.Contains(pickedMarker));
            await CapturedPressBlocksReentry(room, board, pickedMarker, markerPoint, pixelsPerUnit);
            board.Hover.Close();

            await CornerContinuesAlongUnblockedAxis(room, board, pixelsPerUnit);
        }

        private static async Task CheckModalEdges(UI10TestRoom room, BoardScreen board,
            VisualElement modal, float pixelsPerUnit)
        {
            var map = board.Map;
            foreach (var direction in new[] { Vector2.left, Vector2.right, Vector2.up, Vector2.down })
            {
                var point = FixedEdgePoint(map.worldBound, direction, 2 / pixelsPerUnit);
                var picked = map.panel.Pick(point);
                Assert.IsTrue(picked == modal || modal.Contains(picked), "The modal must really cover the edge.");
                var pan = map.Camera.Pan;
                MapContourProbe.Hover(board.Root, point);
                await room.Settle();
                Assert.IsFalse(map.EdgePan.Active, "The outer strip cannot pan behind a modal.");
                Assert.AreEqual(pan, map.Camera.Pan);
            }
        }

        private static async Task CapturedPressBlocksReentry(UI10TestRoom room, BoardScreen board,
            VisualElement source, Vector2 point, float pixelsPerUnit)
        {
            var map = board.Map;
            var pan = map.Camera.Pan;
            using (var down = PointerDownEvent.GetPooled(new Event
                { type = EventType.MouseDown, button = 0, mousePosition = point, clickCount = 1 }))
                source.SendEvent(down);
            var capture = map.panel.GetCapturingElement(PointerId.mousePointerId) as VisualElement;
            Assert.IsNotNull(capture, "The real window/figure press must capture before testing reentry.");
            try
            {
                var edge = FixedEdgePoint(map.worldBound, Vector2.right, 2 / pixelsPerUnit);
                LeaveViewport(map.panel.visualTree, edge + Vector2.right * 30);
                EnterViewport(map.panel.visualTree, edge);
                await room.Settle();
                Assert.IsFalse(map.EdgePan.Active, "Reentry with a captured press must stay idle.");
                Assert.AreEqual(pan, map.Camera.Pan);
            }
            finally
            {
                // Complete the physical gesture without moving the window or figure.
                using var up = PointerUpEvent.GetPooled(new Event
                    { type = EventType.MouseUp, button = 0, mousePosition = point, clickCount = 1 });
                (capture ?? source).SendEvent(up);
            }
            Assert.IsNull(map.panel.GetCapturingElement(PointerId.mousePointerId));
            Assert.IsFalse(map.EdgePan.Active, "Release alone must not restart navigation.");
            Assert.AreEqual(pan, map.Camera.Pan);
        }

        private static async Task CornerContinuesAlongUnblockedAxis(UI10TestRoom room, BoardScreen board,
            float pixelsPerUnit)
        {
            var map = board.Map;
            Center(map);
            map.Camera.Pan = new Vector2(100000, map.Camera.Pan.y);
            map.Refresh();
            await room.Settle();
            var fixedX = map.Camera.Pan.x;
            var previousY = map.Camera.Pan.y;
            var point = FixedEdgePoint(map.worldBound, new Vector2(-1, -1), 2 / pixelsPerUnit);
            MapContourProbe.Hover(board.Root, point);
            for (var sample = 0; sample < 3; sample++)
            {
                await Task.Delay(50, room.Token);
                Assert.IsTrue(map.EdgePan.Active, "One constrained axis must not stop the other corner direction.");
                Assert.AreEqual(fixedX, map.Camera.Pan.x);
                Assert.Greater(map.Camera.Pan.y, previousY + .1f);
                previousY = map.Camera.Pan.y;
                Covers(map);
            }
            map.EdgePan.Stop();
        }
    }
}
