using System.Collections;
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
    public sealed class MapContourAcceptanceTests
    {
        [UnityTest]
        public IEnumerator BothRolesLoadAllEightExpansionCombinationsWithIdenticalPublicContours() =>
            MatchTestRoom.Wait(Combinations());

        [UnityTest]
        public IEnumerator DenseFieldAndGarrisonRemainDistinctDuringMouseAndFractionalWheelAtFourLayouts() =>
            MatchTestRoom.Wait(DenseLayouts());

        private static async Task Combinations()
        {
            for (var flags = 0; flags < 8; flags++)
            {
                using var room = await UI10TestRoom.Connect("map17-" + flags);
                foreach (var client in new[] { room.Fp, room.Sh })
                {
                    var board = client.Board;
                    Assert.AreEqual((flags & 1) != 0, board.View.Configuration.Lords);
                    Assert.AreEqual((flags & 2) != 0, board.View.Configuration.Kings);
                    Assert.AreEqual((flags & 4) != 0, board.View.Configuration.Fate);
                    Assert.AreEqual((flags & 4) != 0 ? "sauron" : "dwarves", board.Presentation.Regions["erebor"].Nation);
                    Assert.AreEqual(105, board.Presentation.Regions.Count);
                    Assert.AreEqual(323, board.Presentation.Geometry.Boundaries.Count);
                    Assert.AreEqual(232, board.Presentation.Regions.Values.SelectMany(r => r.Neighbors.Select(n =>
                        string.CompareOrdinal(r.Id, n) < 0 ? r.Id + ":" + n : n + ":" + r.Id)).Distinct().Count());
                    foreach (var shape in board.Presentation.Shapes.Values)
                        Assert.AreEqual(shape.Id, board.Map.HitTest(board.Map.Camera.ToScreen(
                            new Vector2(shape.Anchor[0], shape.Anchor[1]))));
                    board.Map.Fit();
                    await room.Settle();
                    LayoutTests.Save(client.Viewport, $"map17-combination-{flags}-{board.View.You}.png");
                }
                Assert.AreSame(room.Fp.Board.Presentation.Geometry, room.Sh.Board.Presentation.Geometry);
                Assert.AreEqual(JsonConvert.SerializeObject(room.Fp.Board.Presentation.Regions),
                    JsonConvert.SerializeObject(room.Sh.Board.Presentation.Regions));
                Assert.AreEqual(JsonConvert.SerializeObject(room.Fp.Board.View.Units),
                    JsonConvert.SerializeObject(room.Sh.Board.View.Units));
                Assert.AreEqual(0, room.Boundary.Commands);
            }
        }

        private static async Task DenseLayouts()
        {
            using var room = await UI10TestRoom.Connect("map17-dense");
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
                            await room.Settle();
                            await InspectDense(room, client, texture, width, scale);
                        }
                        finally
                        {
                            client.Board.Hover.Close();
                            client.Board.Cards.Close();
                            client.Panel.targetTexture = client.Viewport;
                            client.Panel.scale = 1;
                            texture.Release();
                            Object.Destroy(texture);
                        }
                    }
            Assert.AreEqual(0, room.Boundary.Commands);
        }

        private static async Task ExposeMarker(UI10TestRoom room, BoardScreen board, VisualElement marker)
        {
            var card = board.Cards.Root.worldBound;
            var area = board.Map.worldBound;
            var left = Mathf.Max(0, card.xMin - area.xMin - 16);
            var right = Mathf.Max(0, area.xMax - card.xMax - 16);
            Assert.Greater(Mathf.Max(left, right), 160, "A pinned card must leave reachable map space.");
            var destination = new Vector2(left > right ? area.xMin + left / 2 : area.xMax - right / 2, area.center.y);
            var picked = board.Map.panel.Pick(destination);
            Assert.IsTrue(picked == board.Map || board.Map.Contains(picked), "The exposed map is covered by " + picked?.name);
            var delta = destination - marker.worldBound.center;
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown,
                button = 1, mousePosition = destination }))
                picked.SendEvent(down);
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag,
                button = 1, mousePosition = destination + delta }))
                board.Map.SendEvent(move);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp,
                button = 1, mousePosition = destination + delta }))
                board.Map.SendEvent(up);
            await room.Settle();
            Assert.AreEqual(card, board.Cards.Root.worldBound, "Pan must move terrain, not the pinned card.");
        }

        private static async Task InspectDense(UI10TestRoom room, MountedMatchClient client,
            RenderTexture texture, int width, float scale)
        {
            var board = client.Board;
            var before = JsonConvert.SerializeObject(board.View);
            var map = board.Map;
            map.Focus("helms-deep");
            map.Camera.ZoomAt(map.contentRect.center, 1.8f);
            map.Refresh();
            foreach (var delta in new[] { 3f, -3f, .25f, -.25f })
            {
                var position = map.contentRect.center;
                var original = map.Camera.ToBoard(position);
                var zoom = map.Camera.Zoom;
                using var wheel = WheelEvent.GetPooled(new Event { type = EventType.ScrollWheel,
                    mousePosition = map.LocalToWorld(position), delta = new Vector2(0, delta) });
                map.SendEvent(wheel);
                Assert.AreNotEqual(zoom, map.Camera.Zoom);
                Assert.Less(Vector2.Distance(original, map.Camera.ToBoard(position)), .001f);
            }
            MapContourProbe.Pan(map, new Vector2(-120, 20));
            board.Cards.Show(board.View.Hand.First(), true);
            await room.Settle();
            var source = map.Q<Button>("army-helms-deep-Shadow");
            Assert.IsNotNull(source);
            Assert.AreEqual(DisplayStyle.Flex, source.resolvedStyle.display);
            LayoutTests.Save(texture, $"map17-dense-before-pan-{board.View.You}-{width}-{scale:0.00}.png");
            await ExposeMarker(room, board, source);
            var picked = source.panel.Pick(source.worldBound.center);
            LayoutTests.Save(texture, $"map17-dense-after-pan-{board.View.You}-{width}-{scale:0.00}.png");
            Assert.IsTrue(picked == source || source.Contains(picked),
                $"Dense marker {source.worldBound} is occluded by {picked?.name}; map={map.worldBound}, card={board.Cards.Root.worldBound}.");
            MapContourProbe.Hover(board.Root, source.worldBound.center);
            await Task.Delay(300, room.Token);
            var hover = board.Hover.Panel.Root;
            Assert.AreEqual(DisplayStyle.None, hover.resolvedStyle.display);
            PointerGestureEvents.ClickAt(source);
            await room.Settle();
            Assert.AreEqual(DisplayStyle.Flex, hover.resolvedStyle.display);
            Assert.AreEqual("helms-deep", board.Hover.Panel.Region);
            var forces = hover.Query<VisualElement>(className: "hover-force").ToList();
            Assert.AreEqual(2, forces.Count);
            Assert.IsTrue(forces.Any(force => force.Q<Label>().text == "FP · Garrison"));
            Assert.IsTrue(forces.Any(force => force.Q<Label>().text == "SH · Field"));
            var figures = hover.Query<Button>().ToList().Where(button => button.name.StartsWith("hover-figure-")).ToArray();
            Assert.AreEqual(23, figures.Length); // 7 FP units + 3 companions, 13 SH units.
            foreach (var figure in figures)
            {
                Assert.IsTrue(hover.worldBound.Contains(figure.worldBound.min + Vector2.one));
                Assert.IsTrue(hover.worldBound.Contains(figure.worldBound.max - Vector2.one));
            }
            Assert.IsEmpty(hover.Query<ScrollView>().ToList());
            Assert.IsTrue(board.Cards.Pinned);
            CardViewportLayoutTests.AssertFits(board);
            LayoutTests.Save(texture, $"map17-dense-{board.View.You}-{width}-{scale:0.00}.png");
            Assert.AreEqual(before, JsonConvert.SerializeObject(board.View));
        }
    }
}
