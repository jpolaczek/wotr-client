using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    // UI-199 (user report): scrolled to the view's edge, a force keeps its place beside its region and the map's edge
    // cuts it; it never moves inward or vanishes while its region is within reach, and far away it is hidden.
    public sealed class MapMarkerEdgeTests
    {
        [UnityTest]
        public IEnumerator AForceAtTheEdgeKeepsItsPlaceAndIsCutByTheMap()
        {
            using var client = new MountedMatchClient("http://localhost", "marker-edge");
            client.Apply(new BoardFixture().View);
            yield return Settle();
            var map = client.Board.Map;
            client.Board.Hover.Close();
            var shape = map.Data.Shapes["minas-tirith"];
            var point = new Vector2(shape.Anchor[0], shape.Anchor[1]);
            map.FocusOn(point, map.FitZoom * 2);
            map.Refresh();
            yield return Settle();
            var army = map.Q<Button>("army-minas-tirith-FreePeoples");
            Assert.AreEqual(DisplayStyle.Flex, army.resolvedStyle.display);
            var offset = Offset(map, army, point);

            // The region's point 20 points below the top edge: the row above the settlement reaches past the edge.
            map.Camera.Pan += new Vector2(0, map.contentRect.yMin + 20 - map.Camera.ToScreen(point).y);
            map.Refresh();
            yield return Settle();
            Assert.AreEqual(DisplayStyle.Flex, army.resolvedStyle.display, "Within reach the force stays shown.");
            Assert.Less(Vector2.Distance(offset, Offset(map, army, point)), 1f, "The force keeps its place beside its region.");
            Assert.Less(army.worldBound.yMin, map.worldBound.yMin, "The map's edge cuts the force; it is not pushed inward.");

            map.Camera.Pan += new Vector2(0, -(MapCanvas.MarkerReach + 200));
            map.Refresh();
            yield return Settle();
            Assert.AreEqual(DisplayStyle.None, army.resolvedStyle.display, "Far beyond the edge the force is hidden.");
        }

        private static Vector2 Offset(MapCanvas map, VisualElement army, Vector2 point) =>
            army.worldBound.position - map.LocalToWorld(map.Camera.ToScreen(point));

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 6; i++)
                yield return null;
        }
    }
}
