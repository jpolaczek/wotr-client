using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public sealed partial class MapViewportTests
    {
        [UnityTest]
        public IEnumerator FixedOuterEdgesCornersAndHudPointsStayContinuousAtFourLayouts() =>
            MatchTestRoom.Wait(EdgeLayouts(CheckFixedEdges));

        [UnityTest]
        public IEnumerator MovingArmyCannotStopNavigationUnderAStationaryEdgePointerAtFourLayouts() =>
            MatchTestRoom.Wait(EdgeLayouts(CheckIncomingArmy));

        [UnityTest]
        public IEnumerator ReenteringTheViewportResumesEdgePanWithoutAnotherMoveAtFourLayouts() =>
            MatchTestRoom.Wait(EdgeLayouts(CheckReentry));

        private static async Task EdgeLayouts(Func<UI10TestRoom, BoardScreen, float, Task> check)
        {
            using var room = await UI10TestRoom.Connect("map15-navigation");
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                var snapshot = JsonConvert.SerializeObject(client.Board.View);
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        target.Create();
                        try
                        {
                            client.Panel.targetTexture = target;
                            client.Panel.scale = scale;
                            await room.Settle();
                            // Measure physical pixels from this rendered target rather
                            // than assuming PanelSettings.scale is its entire transform.
                            var pixelsPerUnit = target.width / client.Board.Map.worldBound.width;
                            await check(room, client.Board, pixelsPerUnit);
                        }
                        catch (Exception)
                        {
                            LayoutTests.Save(target, $"ui81-{check.Method.Name}-{client.Board.View.You}-{width}-{scale:0.00}.png");
                            throw;
                        }
                        finally
                        {
                            client.Board.Map.EdgePan.Stop();
                            client.Panel.targetTexture = client.Viewport;
                            client.Panel.scale = 1;
                            target.Release();
                            Object.Destroy(target);
                        }
                    }
                Assert.AreEqual(snapshot, JsonConvert.SerializeObject(client.Board.View));
            }
            Assert.AreEqual(0, room.Boundary.Commands);
        }

        private static async Task CheckFixedEdges(UI10TestRoom room, BoardScreen board, float scale)
        {
            // Cards touches the right edge. Only the narrow outer strip belongs to
            // navigation: a point farther inside the same real button must still block.
            Center(board.Map);
            await room.Settle();
            var cards = board.Hand.parent;
            var rim = new Vector2(board.Map.worldBound.xMax - 2 / scale, cards.worldBound.center.y);
            Assert.IsTrue(cards.worldBound.Contains(rim), "The regression must exercise the actual Cards edge.");
            var rimPick = board.Map.panel.Pick(rim);
            Assert.IsTrue(rimPick == cards || cards.Contains(rimPick), "The outer probe must hit the real Cards button.");
            await AssertContinuous(room, board, rim, Vector2.left, "outer Cards edge");
            var inside = new Vector2(board.Map.worldBound.xMax - 15, cards.worldBound.center.y);
            var picked = board.Map.panel.Pick(inside);
            Assert.IsTrue(picked == cards || cards.Contains(picked), "The inner probe must hit Cards.");
            Assert.Less(MapEdgePan.VelocityAt(inside, board.Map.worldBound).x, 0);
            var pan = board.Map.Camera.Pan;
            MapContourProbe.Hover(board.Root, inside);
            await room.Settle();
            Assert.IsFalse(board.Map.EdgePan.Active, "The outer strip must not unblock the whole Cards button.");
            Assert.AreEqual(pan, board.Map.Camera.Pan);
            Assert.IsTrue(board.HandDrawer.IsOpen, "Hover preserves the docked hand.");

            // Fixed points deliberately do not search for a hole in the rendered HUD.
            foreach (var direction in new[] { Vector2.left, Vector2.right, Vector2.up, Vector2.down,
                new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
                foreach (var inset in new[] { 2f / scale, 0f })
                {
                    Center(board.Map);
                    await room.Settle();
                    var area = board.Map.worldBound;
                    var point = FixedEdgePoint(area, direction, inset);
                    await AssertContinuous(room, board, point, -direction,
                        $"edge {direction}, inset {inset}, pick {board.Map.panel.Pick(point)?.name}");
                }
        }

        private static Vector2 FixedEdgePoint(Rect area, Vector2 direction, float inset) =>
            new Vector2(direction.x < 0 ? area.xMin + inset : direction.x > 0 ? area.xMax - inset : area.center.x,
                direction.y < 0 ? area.yMin + inset : direction.y > 0 ? area.yMax - inset : area.center.y);

        private static async Task AssertContinuous(UI10TestRoom room, BoardScreen board, Vector2 point,
            Vector2 direction, string description)
        {
            var map = board.Map;
            var previous = map.Camera.Pan;
            MapContourProbe.Hover(board.Root, point);
            Assert.IsTrue(map.EdgePan.Active, description + " must start navigation.");
            // No further pointer events: every interval must advance independently.
            for (var sample = 0; sample < 3; sample++)
            {
                await EdgeSampleInterval(map, room.Token);
                var delta = map.Camera.Pan - previous;
                Assert.IsTrue(map.EdgePan.Active, description + " stopped under the stationary pointer.");
                if (direction.x != 0)
                    Assert.Greater(delta.x * direction.x, .1f, description + " horizontal sample " + sample);
                if (direction.y != 0)
                    Assert.Greater(delta.y * direction.y, .1f, description + " vertical sample " + sample);
                Covers(map);
                previous = map.Camera.Pan;
            }
            map.EdgePan.Stop();
        }

        // Read after UI Toolkit has processed the same scheduler as the edge-pan timer.
        // A Task.Delay continuation alone can run before that frame's scheduled updates.
        private static async Task EdgeSampleInterval(MapCanvas map, CancellationToken token)
        {
            var sampled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var interval = map.schedule.Execute(() => sampled.TrySetResult(true));
            interval.ExecuteLater(50);
            try
            {
                var completed = await Task.WhenAny(sampled.Task, Task.Delay(2000, token));
                token.ThrowIfCancellationRequested();
                Assert.AreSame(sampled.Task, completed, "The UI scheduler did not complete the edge-pan sample within two seconds.");
                await sampled.Task;
            }
            finally
            {
                interval.Pause();
            }
        }

        private static async Task CheckIncomingArmy(UI10TestRoom room, BoardScreen board, float scale)
        {
            var map = board.Map;
            var marker = map.Q<Button>("army-minas-tirith-FreePeoples");
            Assert.IsNotNull(marker, "Use the real army from the server projection.");
            var anchor = map.Data.Shapes["minas-tirith"].Anchor;
            var exposedY = (board.Hunt.Root.worldBound.yMax + board.Hand.parent.worldBound.yMin) / 2;
            map.EdgePan.Stop();
            map.Camera.Zoom = map.Camera.MaximumZoom;
            // UI-126: the row of forces stands above the settlement symbol, so the pointer waits
            // at the marker's height, measured from the real layout, not at the region's anchor.
            var rowOffset = await MarkerRowOffset(room, map, marker, "minas-tirith", exposedY);
            // Camera preparation leaves the real army just outside the right edge.
            // Only the timer moves it into the viewport; neither the marker nor its
            // authoritative region is moved directly by the test.
            map.Camera.Pan = new Vector2(map.contentRect.xMax + 35 - anchor[0] * map.Camera.Zoom,
                map.WorldToLocal(new Vector2(map.worldBound.xMax, exposedY)).y - rowOffset - anchor[1] * map.Camera.Zoom);
            map.Refresh();
            await room.Settle();
            Assert.Greater(MapContourProbe.Anchor(map, "minas-tirith").x, map.worldBound.xMax);
            var point = new Vector2(map.worldBound.xMax - 15, exposedY);
            Assert.AreSame(map, map.panel.Pick(point), "The stationary pointer starts on actual exposed terrain.");
            MapContourProbe.Hover(board.Root, point);
            Assert.IsTrue(map.EdgePan.Active);
            var deadline = Time.realtimeSinceStartup + 2;
            var reachedPointer = false;
            while (Time.realtimeSinceStartup < deadline)
            {
                var picked = map.panel.Pick(point);
                if (picked == marker || marker.Contains(picked))
                {
                    reachedPointer = true;
                    break;
                }
                await Task.Delay(8, room.Token);
            }
            Assert.IsTrue(reachedPointer, "The rendered army must physically arrive under the unchanged pointer " +
                $"(scale {scale}, pointer {point}, marker {marker.worldBound}, anchor {MapContourProbe.Anchor(map, "minas-tirith")}).");
            var previous = map.Camera.Pan;
            for (var sample = 0; sample < 3; sample++)
            {
                await EdgeSampleInterval(map, room.Token);
                Assert.IsTrue(map.EdgePan.Active, "An incoming map figure must not stop its own navigation.");
                Assert.Less(map.Camera.Pan.x, previous.x - .1f);
                previous = map.Camera.Pan;
                Covers(map);
            }
            map.EdgePan.Stop();
        }

        // The vertical distance from a region's anchor to the centre of its marker row, with the anchor
        // just inside the right edge at the pointer's height.
        private static async Task<float> MarkerRowOffset(UI10TestRoom room, MapCanvas map, Button marker, string region, float y)
        {
            var anchor = map.Data.Shapes[region].Anchor;
            map.Camera.Pan = new Vector2(map.contentRect.xMax - 60 - anchor[0] * map.Camera.Zoom,
                map.WorldToLocal(new Vector2(map.worldBound.xMax, y)).y - anchor[1] * map.Camera.Zoom);
            map.Refresh();
            await room.Settle();
            Assert.AreEqual(DisplayStyle.Flex, marker.resolvedStyle.display, "The measured marker is on the map.");
            return marker.worldBound.center.y - MapContourProbe.Anchor(map, region).y;
        }

        private static async Task CheckReentry(UI10TestRoom room, BoardScreen board, float scale)
        {
            Center(board.Map);
            await room.Settle();
            var map = board.Map;
            var point = FixedEdgePoint(map.worldBound, Vector2.left, 2 / scale);
            var input = map.panel.visualTree;
            MapContourProbe.Hover(board.Root, point);
            Assert.IsTrue(map.EdgePan.Active);
            LeaveViewport(input, point - Vector2.right * 30);
            Assert.IsFalse(map.EdgePan.Active);
            var pan = map.Camera.Pan;
            await room.Settle();
            Assert.AreEqual(pan, map.Camera.Pan, "Leaving the panel must stop its timer.");
            EnterViewport(input, point);
            Assert.IsTrue(map.EdgePan.Active, "A fresh panel entry must not require an extra PointerMove.");
            await room.Settle();
            Assert.Greater(map.Camera.Pan.x, pan.x + 1);
            Covers(map);
            map.EdgePan.Stop();

            Center(map);
            await room.Settle();
            var boundary = FixedEdgePoint(map.worldBound, Vector2.right, 0);
            MapContourProbe.Hover(board.Root, boundary);
            Assert.IsTrue(map.EdgePan.Active);
            // A host window exit may retain the last on-screen coordinate. It must
            // stop even when that coordinate is the exact navigation boundary.
            using (var leaveWindow = MouseLeaveWindowEvent.GetPooled(new Event
                { type = EventType.MouseLeaveWindow, mousePosition = boundary }))
                input.SendEvent(leaveWindow);
            Assert.IsFalse(map.EdgePan.Active);
            pan = map.Camera.Pan;
            await room.Settle();
            Assert.AreEqual(pan, map.Camera.Pan, "Window exit at the last known boundary stops navigation.");
            EnterViewport(input, point);
            Assert.IsTrue(map.EdgePan.Active, "A fresh entry must recover after a host window exit.");
            map.EdgePan.Stop();
        }

        private static void LeaveViewport(VisualElement input, Vector2 point)
        {
            using var trigger = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = point });
            using var leave = PointerLeaveEvent.GetPooled(trigger);
            // Enter/Leave dispatch to an assigned target, unlike a picked move.
            leave.target = input;
            input.SendEvent(leave);
        }

        private static void EnterViewport(VisualElement input, Vector2 point)
        {
            using var trigger = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = point });
            using var enter = PointerEnterEvent.GetPooled(trigger);
            enter.target = input;
            input.SendEvent(enter);
        }
    }
}
