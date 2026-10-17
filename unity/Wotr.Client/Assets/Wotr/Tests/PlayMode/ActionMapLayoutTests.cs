using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Object = UnityEngine.Object;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class ActionMapLayoutTests
    {
        [UnityTest]
        public IEnumerator RecordedMovementAndAttackFitFourLayoutsAndAllowPan() => Wait(Layouts());

        private static async Task Layouts()
        {
            foreach (var scenario in new[] { "ui42-moves", "ui42-battles" })
            {
                using var room = await Connect(scenario, ScenarioKind.BaseAcceptance);
                var client = room.Fp;
                var board = client.Board;
                board.Battles.Close();
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        texture.Create();
                        client.Panel.targetTexture = texture;
                        client.Panel.scale = scale;
                        board.Windows.Clear();
                        await room.Settle();
                        await ActionContextNetworkTests.OpenLog(room, board);
                        var kind = scenario == "ui42-moves" ? CommandKind.MoveArmies : CommandKind.AttackArmy;
                        var action = board.Log.Model.Rows.Where(r => r.Step == null && r.Group.Action.Command == kind).Last().Group;
                        await ActionContextNetworkTests.OpenEntry(room, board, action.Action.Id);
                        var overlay = board.Map.HistoryOverlay;
                        Assert.IsFalse(overlay.Suppressed);
                        Assert.AreEqual(PickingMode.Ignore, overlay.Root.pickingMode);
                        Assert.IsEmpty(overlay.Root.Query<Label>().ToList(), "Recorded routes use contours, without floating labels.");
                        Assert.IsEmpty(overlay.Root.Query<Button>().ToList(), "Recorded regions never become action controls.");
                        Inside(board.Log.Popup.worldBound, board.Map.worldBound);
                        LayoutTests.Save(texture, $"{scenario}-{width}-{scale:0.00}.png");
                        foreach (var region in overlay.Context.Regions.Where(board.Presentation.Shapes.ContainsKey))
                        {
                            var shape = board.Presentation.Shapes[region];
                            var point = board.Map.LocalToWorld(board.Map.Camera.ToScreen(new Vector2(shape.Anchor[0], shape.Anchor[1])));
                            Assert.IsTrue(board.Map.worldBound.Contains(point), region + " outside framed map");
                            Assert.IsTrue(board.Log.Popup.parent.worldBound.Contains(point), region + " hidden under the fixed HUD");
                            Assert.IsFalse(board.Log.Popup.worldBound.Contains(point), $"{scenario} {width}/{scale} {region} at {point} hidden by {board.Log.Popup.worldBound}");
                            Assert.IsFalse(board.Log.Root.worldBound.Contains(point), region + " hidden by the open log");
                        }
                        var revision = board.View.Revision;
                        var selection = board.Map.Selected;
                        var pan = board.Map.Camera.Pan;
                        var zoom = board.Map.Camera.Zoom;
                        var card = board.Log.Popup.worldBound;
                        var context = overlay.Context;
                        board.Map.Refresh();
                        await room.Settle();
                        Assert.AreEqual(pan, board.Map.Camera.Pan, "Passive layout must not repeat recorded framing.");
                        Assert.AreEqual(zoom, board.Map.Camera.Zoom);
                        Assert.AreEqual(card, board.Log.Popup.worldBound);
                        var origin = ExposedPanPoint(board.Map);
                        using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 1, mousePosition = origin }))
                            board.Map.SendEvent(down);
                        Assert.IsTrue(overlay.Suppressed);
                        using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 1, mousePosition = origin + new Vector2(20, 15) }))
                            board.Map.SendEvent(move);
                        using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 1, mousePosition = origin + new Vector2(20, 15) }))
                            board.Map.SendEvent(up);
                        await room.Settle();
                        Assert.AreNotEqual(pan, board.Map.Camera.Pan);
                        Assert.IsFalse(overlay.Suppressed);
                        Assert.AreEqual(revision, board.View.Revision);
                        Assert.AreEqual(selection, board.Map.Selected);
                        Assert.AreEqual(card, board.Log.Popup.worldBound, "Manual navigation leaves the pinned card in place.");
                        Assert.AreSame(context, overlay.Context);
                        LayoutTests.Save(texture, $"{scenario}-{width}-{scale:0.00}.png");
                        board.Log.CloseCard();
                        board.Log.Close();
                        client.Panel.targetTexture = client.Viewport;
                        texture.Release();
                        Object.Destroy(texture);
                    }
            }
        }

        private static Vector2 ExposedPanPoint(MapCanvas map)
        {
            for (var row = 1; row < 10; row++)
                for (var column = 1; column < 10; column++)
                {
                    var point = map.worldBound.min + Vector2.Scale(map.worldBound.size,
                        new Vector2(column * .1f, row * .1f));
                    if (map.panel.Pick(point) == map)
                        return point;
                }
            throw new AssertionException("No exposed map for the recorded-action pan gesture.");
        }

        private static void Inside(Rect panel, Rect viewport)
        {
            Assert.That(panel.xMin, Is.GreaterThanOrEqualTo(viewport.xMin - 1));
            Assert.That(panel.yMin, Is.GreaterThanOrEqualTo(viewport.yMin - 1));
            Assert.That(panel.xMax, Is.LessThanOrEqualTo(viewport.xMax + 1));
            Assert.That(panel.yMax, Is.LessThanOrEqualTo(viewport.yMax + 1));
        }
    }
}
