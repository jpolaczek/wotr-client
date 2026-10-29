using System.Collections;
using System.Collections.Generic;
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
    // UI-401 (TABLETOP_FIGURES_UX, "Stand places, scale, depth and hit shape"): every force stands on its region's place
    // at every zoom, sized in board units within the readable clamp, and keeps that place while the map pans; nearer
    // forces are drawn in front; the Helm's Deep siege shows both sides; markers keep their design size and move by their
    // transform only.
    public sealed class MapStandLayoutTests
    {
        private static readonly float[] Zooms = { 1f, 1.4f, 2.2f, 4f };

        [UnityTest]
        public IEnumerator ForcesKeepTheirPlacesAtEveryZoomAndWhilePanningWithNearerOnesInFront()
        {
            var fixture = Siege();
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
                    client.Board.Hover.Close();
                    var map = client.Board.Map;
                    foreach (var relative in Zooms)
                    {
                        var label = $"{width} × {scale:0.00} at {relative:0.0} × fit";
                        var zoom = Mathf.Min(map.FitZoom * relative, map.Camera.MaximumZoom);
                        map.FocusOn(map.Stands.Stands("helms-deep").Field, zoom);
                        yield return Settle();
                        AssertOnPlaces(map, label);
                        AssertNearerInFront(map, label);
                        AssertSiege(map, label);
                        LayoutTests.Save(texture, $"ui401-siege-{width}-{scale:0.00}-{relative:0.0}.png");
                        var before = Shown(map);
                        var pan = map.Camera.Pan;
                        map.Camera.Pan += new Vector2(37, -23);
                        map.Refresh();
                        var moved = map.Camera.Pan - pan;
                        foreach (var pair in Shown(map))
                            if (before.TryGetValue(pair.Key, out var was))
                                Assert.That(Vector2.Distance(pair.Value.position - was.position, moved), Is.LessThan(.01f),
                                    "A force moves with the map and never jumps: " + pair.Key + " · " + label);
                    }
                    client.Panel.targetTexture = client.Viewport;
                    texture.Release();
                    Object.Destroy(texture);
                }
        }

        // A siege at Helm's Deep with neighbours around it, and a tight region (Edoras) that shows tokens until there is
        // room for its figures.
        private static BoardFixture Siege()
        {
            var fixture = new BoardFixture();
            fixture.Put("rohan", UnitKind.Regular, "helms-deep", 2);
            fixture.Put("rohan", UnitKind.Elite, "helms-deep", 1);
            foreach (var unit in fixture.View.Units.Where(u => u.Location == "helms-deep"))
                unit.Garrison = true;
            fixture.Put("isengard", UnitKind.Regular, "helms-deep", 4);
            fixture.Put("isengard", UnitKind.Elite, "helms-deep", 2);
            fixture.Put("rohan", UnitKind.Regular, "westemnet", 2);
            fixture.Put("rohan", UnitKind.Elite, "fords-of-isen", 1);
            fixture.Put("rohan", UnitKind.Regular, "edoras", 1);
            fixture.Put("isengard", UnitKind.Regular, "gap-of-rohan", 2);
            fixture.Put("isengard", UnitKind.Elite, "orthanc", 1);
            fixture.Refresh();
            return fixture;
        }

        // Each shown force stands with its feet on its place (the garrison at the gate, a field force on the field), at
        // its level's size: figures at the zoom clamped to the readable range, a token within its own.
        private static void AssertOnPlaces(MapCanvas map, string label)
        {
            foreach (var army in map.Data.Armies)
            {
                var button = map.Q<Button>(Name(army));
                var bounds = map.MarkerBounds(army.RegionId, army.Side, army.Garrison);
                Assert.AreEqual(bounds.HasValue, button.resolvedStyle.display == DisplayStyle.Flex, button.name + " · " + label);
                if (bounds == null)
                    continue;
                var stands = map.Stands.Stands(army.RegionId);
                var place = army.Garrison ? stands.Garrison.Value : stands.Field;
                var figures = button.ClassListContains(MapArmyArtwork.FiguresClass);
                var feet = button.LocalToWorld(MapArmyArtwork.Feet(army.Garrison, figures));
                var expected = map.LocalToWorld(map.Camera.ToScreen(place));
                Assert.That(Vector2.Distance(feet, expected), Is.LessThan(.5f), "Feet on the place: " + button.name + " · " + label);
                var size = MapArmyArtwork.Size(army.Garrison, figures);
                // At 125 % the panel snaps the design size to whole pixels.
                Assert.That(button.resolvedStyle.width, Is.EqualTo(size.x).Within(.5f), "The design size is kept: " + label);
                var drawn = button.worldBound.width / button.resolvedStyle.width;
                var zoom = map.Camera.Zoom;
                var wanted = figures
                    ? Mathf.Clamp(zoom, MapStandSlots.WorstCaseZoom(map.Data.Geometry), MapArmyArtwork.DesignUnit) / MapArmyArtwork.DesignUnit
                    : Mathf.Clamp(MapStandScale.TokenUnits * zoom, MapStandScale.TokenMinimum, MapStandScale.TokenDesign) / MapStandScale.TokenDesign;
                Assert.That(drawn, Is.EqualTo(wanted).Within(.002f), "Sized in board units: " + button.name + " · " + label);
            }
        }

        // UI-401: the stand layer is sorted by the feet on the board, so a nearer (lower) figure is drawn in front.
        private static void AssertNearerInFront(MapCanvas map, string label)
        {
            var feet = map.Stands.Layer.Children().Select(child => ((MapStand)child.userData).Feet.y).ToList();
            for (var i = 1; i < feet.Count; i++)
                Assert.LessOrEqual(feet[i - 1], feet[i], "Nearer stands later in the layer · " + label);
        }

        // At the Detail zoom the siege shows both sides as figures, the nearer garrison in front, sharing no more than a
        // quarter of either; Edoras, too tight for figures at scale 1, shows its token until the zoom gives it room.
        private static void AssertSiege(MapCanvas map, string label)
        {
            var garrison = map.Q<Button>("army-helms-deep-FreePeoples-garrison");
            var besieger = map.Q<Button>("army-helms-deep-Shadow");
            var detail = map.MarkerVisibility.Detail;
            Assert.AreEqual(map.Camera.Zoom / map.FitZoom >= MapMarkerVisibility.FiguresFrom, detail, label);
            Assert.AreEqual(detail, garrison.ClassListContains(MapArmyArtwork.FiguresClass), label);
            Assert.AreEqual(detail, besieger.ClassListContains(MapArmyArtwork.FiguresClass), label);
            var edoras = map.Q<Button>("army-edoras-FreePeoples");
            var capacity = map.Stands.Stands("edoras").Capacity;
            if (edoras.resolvedStyle.display == DisplayStyle.Flex)
                Assert.AreEqual(detail && map.Stands.Scale.Footprint <= capacity, edoras.ClassListContains(MapArmyArtwork.FiguresClass),
                    "Edoras shows figures only with room for them · " + label);
            if (!detail)
                return;
            var front = map.MarkerBounds("helms-deep", Side.FreePeoples, true).Value;
            var back = map.MarkerBounds("helms-deep", Side.Shadow, false).Value;
            Assert.LessOrEqual(Overlap(front, back), .25f * Mathf.Min(Area(front), Area(back)), "A siege shows both · " + label);
            var layer = map.Stands.Layer;
            Assert.Greater(layer.IndexOf(garrison.parent), layer.IndexOf(besieger.parent), "The nearer garrison in front · " + label);
        }

        private static Dictionary<string, Rect> Shown(MapCanvas map)
        {
            var result = new Dictionary<string, Rect>();
            foreach (var army in map.Data.Armies)
                if (map.MarkerBounds(army.RegionId, army.Side, army.Garrison) is Rect bounds)
                    result[Name(army)] = bounds;
            return result;
        }

        private static string Name(ArmyMarker army) => "army-" + army.RegionId + "-" + army.Side + (army.Garrison ? "-garrison" : "");

        private static float Overlap(Rect a, Rect b) =>
            Mathf.Max(0, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin)) *
            Mathf.Max(0, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));

        private static float Area(Rect rect) => rect.width * rect.height;

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 6; i++)
                yield return null;
        }
    }
}
