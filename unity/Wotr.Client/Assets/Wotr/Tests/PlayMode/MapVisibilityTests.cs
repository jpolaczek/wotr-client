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
    public sealed class MapVisibilityTests
    {
        [UnityTest]
        public IEnumerator FellowshipMarkerStaysUnderPointerWhenItsOwnPanelReplacesRegionInspection()
        {
            var fixture = new BoardFixture();
            fixture.View.Fellowship.RegionId = "minas-tirith";
            using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
            client.Apply(fixture.View);
            yield return Settle();
            client.Board.Map.Fit();
            client.Board.Hover.Open("minas-tirith");
            yield return Settle();
            var marker = client.Board.Map.FellowshipMarker.Root;
            yield return MatchTestRoom.Wait(MapContourProbe.ReachMarker(client.Board.Map, marker, "minas-tirith"));
            var camera = client.Board.Map.Camera.Pan;
            var picked = marker.panel.Pick(marker.worldBound.center);
            LayoutTests.Save(client.Viewport, "ui47-fellowship-marker.png");
            Assert.IsTrue(picked == marker || marker.Contains(picked), "Fellowship marker covered by " + picked?.name);
            PointerGestureEvents.ClickAt(marker);
            yield return Settle();
            Assert.IsTrue(client.Board.Fellowship.IsOpen);
            Assert.AreEqual(camera, client.Board.Map.Camera.Pan);
            Assert.AreEqual(DisplayStyle.Flex, client.Board.Map.FellowshipMarker.Root.resolvedStyle.display);
            Assert.AreEqual(0, client.Board.View.Revision);
        }

        // UI-126: armies are always shown as tokens on the whole map; a click on the region, a selected force or
        // the zoom shows their figures on a base. The row stands beside the settlement symbol, never on it.
        [UnityTest]
        public IEnumerator ArmiesStayVisibleAsTokensAndClickSelectionOrZoomShowTheirFigures()
        {
            var fixture = new BoardFixture(true);
            fixture.Put("sauron", UnitKind.Nazgul, "minas-tirith", 2);
            fixture.View.Units.First(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Leader).Garrison = true;
            fixture.Refresh();
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
                    var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    texture.Create();
                    client.Panel.targetTexture = texture;
                    client.Panel.scale = scale;
                    client.Apply(fixture.View);
                    yield return Settle();
                    var board = client.Board;
                    board.Hover.Close();
                    board.Map.Fit();
                    yield return Settle();
                    var label = $"{width} × {scale:0.00}";
                    var marker = board.Map.Q<Button>("army-minas-tirith-FreePeoples");
                    var shadow = board.Map.Q<Button>("army-minas-tirith-Shadow");
                    Assert.IsFalse(board.Map.MarkerVisibility.Detail, label);
                    Assert.AreEqual(DisplayStyle.Flex, marker.resolvedStyle.display, "The whole map shows every army · " + label);
                    Assert.IsTrue(marker.ClassListContains(MapArmyArtwork.TokenClass), label);
                    LayoutTests.Save(texture, $"ui126-map-fit-{width}-{scale:0.00}.png");
                    var point = Anchor(board.Map, "minas-tirith");
                    using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = point }))
                        board.Root.panel.visualTree.SendEvent(move);
                    yield return new WaitForSecondsRealtime(.3f);
                    Assert.IsTrue(marker.ClassListContains(MapArmyArtwork.TokenClass),
                        "Terrain hover only highlights the contour; figures need a click · " + label);
                    MapContourProbe.ClickRegion(board.Map, "minas-tirith");
                    yield return Settle();
                    point = Anchor(board.Map, "minas-tirith");
                    Assert.IsTrue(marker.ClassListContains(MapArmyArtwork.FiguresClass), "A click shows the region's figures · " + label);
                    var dale = board.Map.Q<Button>("army-dale-FreePeoples");
                    if (dale.resolvedStyle.display == DisplayStyle.Flex)
                        Assert.IsTrue(dale.ClassListContains(MapArmyArtwork.TokenClass), "Other regions keep their tokens · " + label);
                    var visible = board.Map.MarkerBounds("minas-tirith").ToArray();
                    Assert.AreEqual(3, visible.Length, "Field forces and garrison stay separate · " + label);
                    var anchor = board.Map.WorldToLocal(point);
                    Assert.Less(Distance(Union(visible), anchor), 40, "The row stands beside its settlement · " + label);
                    for (var i = 0; i < visible.Length; i++)
                    {
                        Assert.IsFalse(visible[i].Contains(anchor), "A marker must not cover the settlement symbol · " + label);
                        for (var j = i + 1; j < visible.Length; j++)
                            Assert.IsFalse(visible[i].Overlaps(visible[j]), "Forces of one region never overlap · " + label);
                    }
                    CardNetworkTests.Click(board.OwnDice.Q<Button>("army"));
                    yield return Settle();
                    MapContourProbe.ClickRegion(board.Map, "minas-tirith");
                    yield return Settle();
                    var figure = board.Hover.Panel.Root.Query<Button>(className: "eligible-figure").ToList()
                        .First(b => ((DraftToken)b.userData).Id == fixture.View.Units.First(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Regular).Id);
                    // UI-201: the figure stands in its own soft glow, inside its tile, which takes no pointer.
                    var glow = figure.Q("figure-glow");
                    Assert.IsNotNull(glow, "A panel figure stands in a glow · " + label);
                    Assert.AreEqual(PickingMode.Ignore, glow.pickingMode);
                    Assert.IsTrue(figure.worldBound.Contains(glow.worldBound.center));
                    CardGestures.Move(figure);
                    yield return new WaitForSecondsRealtime(.22f);
                    PointerGestureEvents.ClickAt(figure);
                    yield return Settle();
                    CardGestures.Move(board.Phase);
                    yield return new WaitForSecondsRealtime(.25f);
                    board.Hover.Close();
                    yield return Settle();
                    Assert.AreEqual(DisplayStyle.Flex, marker.resolvedStyle.display);
                    Assert.IsTrue(marker.ClassListContains("selected-force"));
                    Assert.IsTrue(marker.ClassListContains(MapArmyArtwork.FiguresClass), "The selected force keeps its figures · " + label);
                    Assert.AreEqual(DisplayStyle.Flex, shadow.resolvedStyle.display, "Every army stays on the map · " + label);
                    board.Map.Camera.ZoomAt(board.Map.WorldToLocal(point), 1.65f);
                    board.Map.Refresh();
                    yield return Settle();
                    Assert.IsTrue(board.Map.MarkerVisibility.Detail);
                    Assert.IsTrue(shadow.ClassListContains(MapArmyArtwork.FiguresClass), label);
                    LayoutTests.Save(texture, $"ui126-map-zoom-{width}-{scale:0.00}.png");
                    // Cancel before the next viewport; inspection itself has not sent a command.
                    using (var escape = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape }))
                        board.Root.SendEvent(escape);
                    using (var release = KeyUpEvent.GetPooled(new Event { type = EventType.KeyUp, keyCode = KeyCode.Escape }))
                        board.Root.SendEvent(release);
                    client.Panel.targetTexture = client.Viewport;
                    texture.Release();
                    Object.Destroy(texture);
                }
        }

        private static Rect Union(Rect[] rects) => Rect.MinMaxRect(rects.Min(r => r.xMin), rects.Min(r => r.yMin),
            rects.Max(r => r.xMax), rects.Max(r => r.yMax));

        private static float Distance(Rect rect, Vector2 point) => Vector2.Distance(point,
            new Vector2(Mathf.Clamp(point.x, rect.xMin, rect.xMax), Mathf.Clamp(point.y, rect.yMin, rect.yMax)));

        private static Vector2 Anchor(MapCanvas map, string region)
        {
            var anchor = map.Data.Shapes[region].Anchor;
            return map.LocalToWorld(map.Camera.ToScreen(new Vector2(anchor[0], anchor[1])));
        }

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 6; i++)
                yield return null;
        }
    }
}
