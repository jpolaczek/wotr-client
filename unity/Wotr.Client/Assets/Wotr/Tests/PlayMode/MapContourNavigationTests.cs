using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public sealed class MapContourNavigationTests
    {
        [UnityTest]
        public IEnumerator BothRolesInspectContoursAtFourLayoutsWithoutChangingProjectedState() =>
            MatchTestRoom.Wait(Layouts());

        [UnityTest]
        public IEnumerator BattleNoticeAndRecordedActionUseCurrentAnchorsWithoutAutomaticCameraMotion() =>
            MatchTestRoom.Wait(Navigation());

        private static async Task Layouts()
        {
            using var room = await UI10TestRoom.Connect("map15-navigation");
            foreach (var client in new[] { room.Fp, room.Sh })
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        texture.Create();
                        try
                        {
                            client.Panel.targetTexture = texture;
                            client.Panel.scale = scale;
                            client.Board.Windows.Clear();
                            await room.Settle();
                            await InspectLayout(room, client, texture, width, scale);
                        }
                        finally
                        {
                            client.Board.Cards.Close();
                            client.Board.Hover.Close();
                            client.Panel.targetTexture = client.Viewport;
                            client.Panel.scale = 1;
                            texture.Release();
                            Object.Destroy(texture);
                        }
                    }
            Assert.AreEqual(0, room.Boundary.Commands);
        }

        private static async Task InspectLayout(UI10TestRoom room, MountedMatchClient client,
            RenderTexture texture, int width, float scale)
        {
            var board = client.Board;
            var state = JsonConvert.SerializeObject(board.View);
            board.Map.Fit();
            foreach (var factor in new[] { 1f, 1.8f, 1.4f })
            {
                var cursor = board.Map.contentRect.center + new Vector2(23, -17);
                var underCursor = board.Map.Camera.ToBoard(cursor);
                board.Map.Camera.ZoomAt(cursor, factor);
                board.Map.Refresh();
                Assert.Less(Vector2.Distance(underCursor, board.Map.Camera.ToBoard(cursor)), .001f);
                MapContourProbe.Pan(board.Map, new Vector2(31, -19));
                foreach (var shape in board.Presentation.Shapes.Values)
                {
                    var anchor = new Vector2(shape.Anchor[0], shape.Anchor[1]);
                    Assert.AreEqual(shape.Id, board.Map.HitTest(board.Map.Camera.ToScreen(anchor)), shape.Id);
                }
                CheckEdgesAndWater(board.Map);
            }

            // Concave woods, a small battlefield, and coastal terrain all share the
            // same coordinate path as the renderer. The pinned card stays live.
            board.Cards.Show(board.View.Hand.First(), true);
            foreach (var region in new[] { "old-forest", "osgiliath", "dol-amroth" })
            {
                board.Hover.Close();
                board.Map.Focus(region);
                await room.Settle();
                LayoutTests.Save(texture, $"map15-before-pan-{board.View.You}-{region}-{width}-{scale:0.00}.png");
                await NavigateBesidePinnedCard(room, board, $"{board.View.You}/{region}/{width}/{scale:0.00}",
                    trace => MapContourProbe.ReachRegion(board.Map, region, trace));
                var point = MapContourProbe.Terrain(board.Map, region);
                var pan = board.Map.Camera.Pan;
                MapContourProbe.Hover(board.Root, point);
                await Task.Delay(280, room.Token);
                Assert.AreEqual(DisplayStyle.None, board.Hover.Panel.Root.resolvedStyle.display,
                    "An empty contour must not open from hover: " + region);
                MapContourProbe.Click(board.Map, point);
                await room.Settle();
                Assert.AreEqual(region, board.Hover.Panel.Region);
                Assert.AreEqual(DisplayStyle.Flex, board.Hover.Panel.Root.resolvedStyle.display);
                Assert.AreEqual(pan, board.Map.Camera.Pan);
                Assert.IsTrue(board.Cards.Pinned);
                LayoutTests.Save(texture, $"map15-hit-{client.Board.View.You}-{region}-{width}-{scale:0.00}.png");
            }
            await VisibleBoundary(room, board, texture, width, scale);
            board.Cards.Close();
            await EmptyKeyboardFocus(room, board);
            await OccupiedInspection(room, board);
            await Locate(room, board);
            Assert.AreEqual(state, JsonConvert.SerializeObject(board.View));
        }

        private static void CheckEdgesAndWater(MapCanvas map)
        {
            foreach (var pair in new[] { ("buckland", "old-forest"), ("minas-tirith", "osgiliath"),
                ("dagorlad", "morannon"), ("dol-amroth", (string)null) })
            {
                var boundary = map.Data.Geometry.Boundaries.First(edge =>
                    edge.Owners.Contains(pair.Item1) && edge.Owners.Contains(pair.Item2));
                var segment = Enumerable.Range(1, boundary.Points.Length - 1)
                    .OrderByDescending(i => Vector2.Distance(Point(boundary.Points[i - 1]), Point(boundary.Points[i])))
                    .First();
                var from = Point(boundary.Points[segment - 1]);
                var to = Point(boundary.Points[segment]);
                var center = (from + to) / 2;
                var normal = new Vector2(-(to - from).y, (to - from).x).normalized;
                foreach (var direction in new[] { -1f, 1f })
                {
                    var inside = center + normal * direction;
                    var expected = map.Data.Contours.HitTest(inside);
                    Assert.Contains(expected, boundary.Owners);
                    var local = map.WorldToLocal(map.LocalToWorld(map.Camera.ToScreen(inside)));
                    Assert.AreEqual(expected, map.HitTest(local), boundary.Id + " boundary side " + direction);
                }
                // Exact source coordinates retain the documented ordinal tie rule.
                var expectedBoundary = boundary.Owners.Where(owner => owner != null).OrderBy(id => id, StringComparer.Ordinal).First();
                Assert.AreEqual(expectedBoundary, map.Data.Contours.HitTest(center));
            }
            foreach (var water in new[] { new Vector2(-20, 900), new Vector2(1504.875f, 988.5f) })
                Assert.IsNull(map.HitTest(map.Camera.ToScreen(water)), "Ocean and the Gorgoroth interior hole remain unselectable.");
        }

        private static Vector2 Point(float[] coordinates) => new Vector2(coordinates[0], coordinates[1]);

        private static async Task VisibleBoundary(UI10TestRoom room, BoardScreen board,
            RenderTexture texture, int width, float scale)
        {
            // The source's horizontal Old Forest/Buckland edge at y=328 is long
            // enough to probe each side away from its vertices and white center.
            var center = new Vector2(416, 328);
            board.Hover.Close();
            board.Map.Camera.Zoom = 2.6f;
            board.Map.Camera.Pan = board.Map.contentRect.center - center * board.Map.Camera.Zoom;
            board.Map.Refresh();
            await room.Settle();
            var points = new[] { center + Vector2.up, center + Vector2.down };
            await NavigateBesidePinnedCard(room, board, $"{board.View.You}/old-forest-boundary/{width}/{scale:0.00}",
                trace => MapContourProbe.ReachPoints(board.Map, points, trace));
            foreach (var offset in new[] { -1f, 1f })
            {
                var coordinate = center + new Vector2(0, offset);
                var expected = board.Presentation.Contours.HitTest(coordinate);
                Assert.Contains(expected, new[] { "old-forest", "buckland" });
                var point = board.Map.LocalToWorld(board.Map.Camera.ToScreen(coordinate));
                MapContourProbe.Click(board.Map, point);
                await room.Settle();
                Assert.AreEqual(expected, board.Hover.Panel.Region);
                Assert.AreEqual(expected, board.Map.Selected);
                LayoutTests.Save(texture, $"map15-edge-{board.View.You}-{expected}-{width}-{scale:0.00}.png");
                board.Hover.Close();
                await room.Settle();
            }
        }

        private static async Task NavigateBesidePinnedCard(UI10TestRoom room, BoardScreen board, string context,
            Action<Action<string>> navigate)
        {
            var card = board.Cards.Root.worldBound;
            var map = board.Map;
            var details = new List<string>
            {
                context + "; initial pan=" + map.Camera.Pan.ToString("F3") +
                    "; zoom=" + map.Camera.Zoom.ToString("R") + "; card=" + card
            };
            try
            {
                navigate(details.Add);
                await room.Settle();
                Assert.AreEqual(card, board.Cards.Root.worldBound, "Panning moves terrain, not the pinned card.");
                Assert.IsTrue(board.Cards.Pinned);
            }
            catch (AssertionException error)
            {
                throw new AssertionException(error.Message + "\n" + string.Join("\n", details));
            }
            TestContext.WriteLine(string.Join(" | ", details));
        }

        private static async Task EmptyKeyboardFocus(UI10TestRoom room, BoardScreen board)
        {
            board.Hover.Close();
            board.Map.Focus("old-forest");
            await room.Settle();
            board.Root.Focus();
            using (var key = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Tab }))
                board.Root.SendEvent(key);
            ((VisualElement)board.Map).Focus();
            await room.Settle();
            Assert.AreEqual(DisplayStyle.None, board.Hover.Panel.Root.resolvedStyle.display);
            using (var activate = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Return }))
                board.Map.SendEvent(activate);
            await room.Settle();
            Assert.AreEqual("old-forest", board.Hover.Panel.Region);
            Assert.AreEqual(DisplayStyle.Flex, board.Hover.Panel.Root.resolvedStyle.display);
            board.Hover.Close();
        }

        private static async Task OccupiedInspection(UI10TestRoom room, BoardScreen board)
        {
            board.Hover.Close();
            board.Map.Focus("minas-tirith");
            await room.Settle();
            var pan = board.Map.Camera.Pan;
            var selected = board.Map.Selected;
            var point = MapContourProbe.Terrain(board.Map, "minas-tirith");
            MapContourProbe.Hover(board.Root, point);
            await Task.Delay(280, room.Token);
            Assert.AreEqual(DisplayStyle.None, board.Hover.Panel.Root.resolvedStyle.display);
            MapContourProbe.Click(board.Map, point);
            await room.Settle();
            Assert.AreEqual("minas-tirith", board.Hover.Panel.Region);
            Assert.AreEqual(DisplayStyle.Flex, board.Hover.Panel.Root.resolvedStyle.display);
            Assert.AreEqual(pan, board.Map.Camera.Pan);
            Assert.AreEqual(selected, board.Map.Selected);
            Assert.IsEmpty(board.Hover.Panel.Root.Query<Button>(className: "eligible-figure").ToList(),
                "Inspection with no chosen die must not invent controls for either side.");
            MapContourProbe.Hover(board.Root, board.Phase.worldBound.center);
            await Task.Delay(200, room.Token);
            Assert.AreEqual(DisplayStyle.Flex, board.Hover.Panel.Root.resolvedStyle.display);
            board.Hover.Close();
        }

        private static async Task Locate(UI10TestRoom room, BoardScreen board)
        {
            // UI-100: Locate is the position row of the Fellowship panel, unrolled from the corner tab.
            CardGestures.Move(board.Edges.Corner);
            await Task.Delay(300, room.Token);
            var locate = board.Fellowship.Root.Q<Button>("focus-fellowship");
            Assert.IsTrue(locate.enabledInHierarchy && locate.worldBound.width > 0, "The corner tab unrolls the Fellowship panel.");
            PointerGestureEvents.ClickAt(locate);
            await room.Settle();
            var region = board.View.Fellowship.RegionId;
            Assert.AreEqual(region, board.Map.Selected);
            var anchor = MapContourProbe.Anchor(board.Map, region);
            Assert.IsTrue(board.Map.worldBound.Contains(anchor), "Locate keeps the region inside the constrained raster viewport.");
            Assert.AreEqual(region, board.Map.HitTest(board.Map.WorldToLocal(anchor)));
            var before = board.Map.Camera.Pan;
            PointerGestureEvents.ClickAt(board.Map.FellowshipMarker.Root);
            await room.Settle();
            Assert.AreEqual(before, board.Map.Camera.Pan);
            Assert.IsTrue(board.Fellowship.IsOpen);
            var controls = board.Fellowship.Root.Q("fellowship-controls");
            Assert.AreEqual(board.View.You == Side.FreePeoples ? DisplayStyle.Flex : DisplayStyle.None,
                controls.resolvedStyle.display);
            board.Fellowship.Close();
        }

        private static async Task Navigation()
        {
            using var room = await MatchTestRoom.Connect("map15-notice", ScenarioKind.Interaction, openPanels: false);
            var board = room.Fp.Board;
            board.Map.Focus("minas-tirith");
            room.Sh.Board.Map.Focus("old-forest");
            var shadowPan = room.Sh.Board.Map.Camera.Pan;
            ArmyGestures.Die(room.Fp, ArmyGestures.ArmyDie(room.Fp));
            await ArmyGestures.All(room.Fp, "minas-tirith");
            board.Hover.Close();
            await room.Settle();
            var pan = board.Map.Camera.Pan;
            MapContourProbe.Click(board.Map, MapContourProbe.Terrain(board.Map, "lossarnach"));
            await room.At(1);
            Assert.AreEqual(pan, board.Map.Camera.Pan);
            Assert.AreEqual(shadowPan, room.Sh.Board.Map.Camera.Pan);
            Assert.IsTrue(board.Battles.IsOpen, "UI-149: the battle opens its window without moving the camera.");
            PointerGestureEvents.ClickAt(board.Notice.Button);
            await room.Settle();
            Assert.AreEqual("lossarnach", board.Map.Selected);
            Assert.AreEqual("lossarnach", board.Map.HitTest(board.Map.WorldToLocal(
                MapContourProbe.Anchor(board.Map, "lossarnach"))));
            CardNetworkTests.Click(board.Tray.Q<Button>("battle-no-card"));
            await room.At(2);
            Assert.AreEqual(shadowPan, room.Sh.Board.Map.Camera.Pan);
            Assert.IsTrue(room.Sh.Board.Battles.IsOpen, "UI-149: the defender's window opened with the battle.");
            PointerGestureEvents.ClickAt(room.Sh.Board.Notice.Button);
            await room.Settle();
            Assert.AreEqual("lossarnach", room.Sh.Board.Map.Selected);
            Assert.IsNull(room.Sh.Board.View.Battle.Attacker.CardId);
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                client.Board.Battles.Close();
                await ActionContextNetworkTests.OpenLog(room, client.Board);
                var action = client.Board.Log.Model.Rows.First(row => row.Step == null &&
                    row.Group.Action.Command == CommandKind.AttackArmy).Group.Action;
                await ActionContextNetworkTests.OpenEntry(room, client.Board, action.Id);
                var overlay = client.Board.Map.HistoryOverlay;
                Assert.AreEqual(PickingMode.Ignore, overlay.Root.pickingMode);
                Assert.Contains("lossarnach", overlay.Context.Regions.ToArray());
                Assert.Contains("minas-tirith", overlay.Context.Regions.ToArray());
                var savedPan = client.Board.Map.Camera.Pan;
                var connected = client.Connected;
                client.Connection.Reconnect();
                await room.Until(() => client.Connected > connected);
                await room.Settle();
                Assert.AreEqual(savedPan, client.Board.Map.Camera.Pan);
                Assert.AreEqual(2, client.Board.View.Revision);
                Assert.AreEqual(action.Id, client.Board.Log.Card.Displayed.Action.Id);
                client.Board.Log.CloseCard();
                client.Board.Log.Close();
            }
        }
    }
}
