using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Wotr.Client;

namespace Wotr.PlayTests
{
    public sealed partial class MapViewportTests
    {
        [UnityTest]
        public IEnumerator ScreenRoutingKeepsTheLastPixelAndExactEdgesMovingAtFourLayouts() =>
            MatchTestRoom.Wait(EdgeLayouts(CheckScreenEdges));

        private static async Task CheckScreenEdges(UI10TestRoom room, BoardScreen board, float pixelsPerUnit)
        {
            var client = room.Fp.Board == board ? room.Fp : room.Sh;
            var previousOrder = client.Panel.sortingOrder;
            client.Panel.sortingOrder = 10000;
            await room.Settle();
            try
            {
                using var input = new EngineScreenPointer(client.Panel, board.Map);
                var surface = new Rect(0, 0, client.Panel.targetTexture.width, client.Panel.targetTexture.height);
                // Begin with the exclusive maximum, so the routing regression is
                // observed before timer scheduling at otherwise reachable edges.
                foreach (var direction in new[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down,
                    new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
                    foreach (var inset in new[] { 0f, 1f })
                    {
                        Center(board.Map);
                        await room.Settle();
                        var approach = FixedEdgePoint(surface, direction, 2);
                        input.Move(approach);
                        Assert.AreSame(board.Map.panel, input.RoutedPanel,
                            "The approach must use the real selected screen panel.");
                        Assert.IsTrue(board.Map.EdgePan.Active, "The screen approach must already pan.");
                        var delivered = input.DeliveredMoves;
                        var target = FixedEdgePoint(surface, direction, inset);
                        input.Move(target);
                        var detail = $"screen={target}, inset={inset}, physical={surface.size}, " +
                            $"pixelsPerUnit={pixelsPerUnit}, routed={input.RoutedPanel == board.Map.panel}, " +
                            $"panelPoint={input.RoutedPosition}, newMoves={input.DeliveredMoves - delivered}";
                        Assert.IsTrue(board.Map.EdgePan.Active,
                            "Touching the physical game boundary must keep navigation active: " + detail +
                            $", externalMoves={input.ExternalMoves}, lastEvent={input.LastPointerEvent}");
                        var previous = board.Map.Camera.Pan;
                        for (var sample = 0; sample < 3; sample++)
                        {
                            var began = Time.realtimeSinceStartupAsDouble;
                            var frame = Time.frameCount;
                            await EdgeSampleInterval(board.Map, room.Token);
                            var delta = board.Map.Camera.Pan - previous;
                            var sampleDetail = detail + $", sample={sample}, previous={previous}, " +
                                $"current={board.Map.Camera.Pan}, elapsed={Time.realtimeSinceStartupAsDouble - began:F4}, " +
                                $"frames={frame}->{Time.frameCount}, hover={board.Map.Hovered}, " +
                                $"appFocus={Application.isFocused}, externalMoves={input.ExternalMoves}, " +
                                $"lastEvent={input.LastPointerEvent}";
                            Assert.IsTrue(board.Map.EdgePan.Active, sampleDetail);
                            if (direction.x != 0)
                                Assert.Greater(-direction.x * delta.x, .1f, sampleDetail);
                            if (direction.y != 0)
                                Assert.Greater(-direction.y * delta.y, .1f, sampleDetail);
                            Covers(board.Map);
                            previous = board.Map.Camera.Pan;
                        }
                        input.Pause();
                    }
            }
            finally
            {
                client.Panel.sortingOrder = previousOrder;
                board.Map.EdgePan.Stop();
            }
        }
    }
}
