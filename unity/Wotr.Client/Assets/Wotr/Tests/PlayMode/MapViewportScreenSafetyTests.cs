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
        public IEnumerator ScreenSamplesRespectOutsideFocusButtonsCaptureAndModalUiAtFourLayouts() =>
            MatchTestRoom.Wait(EdgeLayouts(CheckScreenSafety));

        private static async Task CheckScreenSafety(UI10TestRoom room, BoardScreen board, float pixelsPerUnit)
        {
            var client = room.Fp.Board == board ? room.Fp : room.Sh;
            var previousOrder = client.Panel.sortingOrder;
            client.Panel.sortingOrder = 10000;
            try
            {
                using var input = new EngineScreenPointer(client.Panel, board.Map);
                Center(board.Map);
                await room.Settle();
                var surface = new Rect(Vector2.zero, input.ScreenSize);
                await CheckNativeAvailability(room, board.Map, input, surface);
                await CheckNativeControl(room, board, input, pixelsPerUnit);
                await CheckNativeModals(room, board, client, input, surface);
                await CheckNativeCapture(room, board, input, surface);
                await CheckNativeLimits(room, board.Map, input);
            }
            finally
            {
                client.Panel.sortingOrder = previousOrder;
                board.Map.EdgePan.Stop();
            }
        }

        private static async Task CheckNativeAvailability(UI10TestRoom room, MapCanvas map,
            EngineScreenPointer input, Rect surface)
        {
            foreach (var direction in new[] { Vector2.left, Vector2.right, Vector2.up, Vector2.down })
            {
                var edge = FixedEdgePoint(surface, direction, 0);
                input.Sample(edge);
                Assert.IsTrue(map.EdgePan.Active, "A focused native sample must reach the exact edge.");
                input.Sample(edge + direction);
                await AssertNativeIdle(room, map, "One physical pixel outside must stop navigation.");
                input.Sample(edge);
                Assert.IsTrue(map.EdgePan.Active, "Returning from outside requires no routed Move.");
                input.Sample(edge, hasFocus: false);
                await AssertNativeIdle(room, map, "An inactive client must not pan.");
                input.Sample(edge);
                Assert.IsTrue(map.EdgePan.Active, "Regaining focus must resume an idle edge pointer.");
                input.Sample(edge, mousePresent: false);
                await AssertNativeIdle(room, map, "An absent mouse must stop navigation.");
                foreach (var buttons in new[] { 1, 2, 4 })
                {
                    input.Sample(edge);
                    Assert.IsTrue(map.EdgePan.Active);
                    input.Sample(edge, pressedButtons: buttons);
                    await AssertNativeIdle(room, map, $"Physical button mask {buttons} must prevent edge pan.");
                }
                await CheckFullScreenEdge(room, map, input, edge, direction);
            }
            input.Pause();
        }

        // UI-174: in a full-screen mode a cursor in a black bar or past the screen's edge pans as at the edge itself,
        // and losing focus still stops it.
        private static async Task CheckFullScreenEdge(UI10TestRoom room, MapCanvas map, EngineScreenPointer input,
            Vector2 edge, Vector2 direction)
        {
            input.FillsScreen = true;
            try
            {
                foreach (var distance in new[] { 1f, 40f, 400f })
                {
                    input.Sample(edge + direction * distance);
                    Assert.IsTrue(map.EdgePan.Active, $"A full-screen cursor {distance} px beyond {direction} keeps panning.");
                }
                var pan = map.Camera.Pan;
                await room.Settle();
                Assert.Greater(Vector2.Dot(map.Camera.Pan - pan, -direction), 0, "The camera moves toward the held edge.");
                input.Sample(edge + direction * 40, hasFocus: false);
                await AssertNativeIdle(room, map, "An inactive full-screen client must not pan.");
            }
            finally
            {
                input.FillsScreen = false;
                Center(map);
            }
        }

        private static async Task CheckNativeControl(UI10TestRoom room, BoardScreen board,
            EngineScreenPointer input, float pixelsPerUnit)
        {
            var map = board.Map;
            var cards = board.Hand.parent;
            var inside = new Vector2(map.worldBound.xMax - 15, cards.worldBound.center.y);
            var picked = map.panel.Pick(inside);
            Assert.IsTrue(picked == cards || cards.Contains(picked), "The sample must hit the real Cards interior.");
            Assert.Less(MapEdgePan.VelocityAt(inside, map.worldBound).x, 0);
            input.Sample(inside * pixelsPerUnit);
            await AssertNativeIdle(room, map, "Raw sampling cannot unblock a control interior.");
            var edge = new Vector2(input.ScreenSize.x, cards.worldBound.center.y * pixelsPerUnit);
            input.Move(edge);
            Assert.IsTrue(map.EdgePan.Active, "The same Cards tab reserves its physical outer edge for navigation.");
            Assert.IsTrue(board.HandDrawer.IsOpen, "Raw sampling preserves the docked hand.");
            input.Pause();
        }

        private static async Task CheckNativeModals(UI10TestRoom room, BoardScreen board,
            MountedMatchClient client, EngineScreenPointer input, Rect surface)
        {
            CardNetworkTests.Click(board.Menu.Toggle);
            await room.Settle();
            Assert.IsTrue(board.Menu.IsOpen);
            await SampleBehindModal(room, board.Map, input, surface, board.Menu.Root);
            CardNetworkTests.Click(board.Menu.Root.Q<Button>("open-interface"));
            await room.Until(() => client.Interface.Settings.IsOpen);
            await room.Settle();
            await SampleBehindModal(room, board.Map, input, surface, client.Interface.Settings.Root);
            input.Pause();
            CardNetworkTests.Click(client.Interface.Settings.Root.Q<Button>("close-interface"));
            await room.Settle();
        }

        private static async Task SampleBehindModal(UI10TestRoom room, MapCanvas map,
            EngineScreenPointer input, Rect surface, VisualElement modal)
        {
            foreach (var direction in new[] { Vector2.left, Vector2.right, Vector2.up, Vector2.down })
            {
                var interior = FixedEdgePoint(surface, direction, 1);
                var panelPoint = RuntimePanelUtils.ScreenToPanel(map.panel, interior);
                var picked = map.panel.Pick(panelPoint);
                Assert.IsTrue(picked == modal || modal.Contains(picked), "The modal must cover this physical edge.");
                input.Move(FixedEdgePoint(surface, direction, 0));
                await AssertNativeIdle(room, map, "Exact native edges cannot pan behind a full-screen modal.");
            }
        }

        private static async Task CheckNativeCapture(UI10TestRoom room, BoardScreen board,
            EngineScreenPointer input, Rect surface)
        {
            board.Information.Region("minas-tirith");
            await room.Settle();
            var header = board.Information.Root.Q(className: FloatingWindow.HandleClass);
            Assert.IsNotNull(header);
            var point = new Vector2(header.worldBound.xMin + 8, header.worldBound.center.y);
            await SampleDuringCapture(room, board.Map, input, header, point, surface);
            board.Information.Close();
            await room.Settle();
            var marker = board.Map.Q<Button>("army-minas-tirith-FreePeoples");
            await MapContourProbe.ReachMarker(board.Map, marker, "minas-tirith");
            await room.Settle();
            await SampleDuringCapture(room, board.Map, input, marker, marker.worldBound.center, surface);
            board.Hover.Close();
        }

        private static async Task SampleDuringCapture(UI10TestRoom room, MapCanvas map,
            EngineScreenPointer input, VisualElement source, Vector2 point, Rect surface)
        {
            var picked = map.panel.Pick(point);
            Assert.IsTrue(picked == source || source.Contains(picked), "The window/figure must receive its real press.");
            using (var down = PointerDownEvent.GetPooled(new Event
                { type = EventType.MouseDown, button = 0, mousePosition = point, clickCount = 1 }))
                picked.SendEvent(down);
            var capture = map.panel.GetCapturingElement(PointerId.mousePointerId) as VisualElement;
            try
            {
                Assert.IsNotNull(capture, "A real press must capture before sampling the raw edge.");
                // Even a release sample received before UI processes PointerUp cannot
                // bypass capture owned by the window or the actual army gesture.
                input.Sample(FixedEdgePoint(surface, Vector2.right, 0), pressedButtons: 0);
                await AssertNativeIdle(room, map, "A native sample must respect an existing UI capture.");
            }
            finally
            {
                input.Pause();
                using var up = PointerUpEvent.GetPooled(new Event
                    { type = EventType.MouseUp, button = 0, mousePosition = point, clickCount = 1 });
                (capture ?? picked).SendEvent(up);
            }
            Assert.IsNull(map.panel.GetCapturingElement(PointerId.mousePointerId));
        }

        private static async Task CheckNativeLimits(UI10TestRoom room, MapCanvas map, EngineScreenPointer input)
        {
            Center(map);
            map.Camera.Pan = new Vector2(100000, map.Camera.Pan.y);
            map.Refresh();
            await room.Settle();
            var before = map.Camera.Pan;
            input.Sample(Vector2.zero);
            Assert.IsTrue(map.EdgePan.Active, "A corner may continue along its unconstrained axis.");
            await Task.Delay(80, room.Token);
            Assert.AreEqual(before.x, map.Camera.Pan.x);
            Assert.Greater(map.Camera.Pan.y, before.y + .1f);
            Covers(map);
            input.Pause();
            map.Camera.Pan = new Vector2(100000, 100000);
            map.Refresh();
            input.Sample(Vector2.zero);
            await AssertNativeIdle(room, map, "Repeated native samples at both camera limits must stay idle.");
            Covers(map);
            input.Pause();
        }

        private static async Task AssertNativeIdle(UI10TestRoom room, MapCanvas map, string message)
        {
            var pan = map.Camera.Pan;
            Assert.IsFalse(map.EdgePan.Active, message);
            await room.Settle();
            Assert.IsFalse(map.EdgePan.Active, message);
            Assert.AreEqual(pan, map.Camera.Pan, message);
            Covers(map);
        }
    }
}
