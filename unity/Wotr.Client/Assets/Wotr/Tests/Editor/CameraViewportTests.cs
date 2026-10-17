using NUnit.Framework;
using UnityEngine;
using Wotr.Client;

namespace Wotr.EditorTests
{
    public sealed class CameraViewportTests
    {
        private static readonly Rect Board = new Rect(37, 19, 1632, 1338);

        [TestCase(1366, 768, 1f)]
        [TestCase(1366, 768, 1.25f)]
        [TestCase(1920, 1080, 1f)]
        [TestCase(1920, 1080, 1.25f)]
        [TestCase(800, 1500, 1f)]
        public void EveryZoomAndPanKeepsTheWholeViewportInsideTheRaster(int width, int height, float scale)
        {
            var viewport = new Rect(11, 23, width / scale, height / scale);
            var camera = new CameraState { Zoom = .01f, Pan = Vector2.one * 9000 };
            camera.Constrain(Board, viewport);
            Assert.That(camera.MinimumZoom, Is.EqualTo(Mathf.Max(viewport.width / Board.width,
                viewport.height / Board.height)).Within(.0001f));
            Covers(camera, viewport);
            foreach (var factor in new[] { .001f, 10000f, .1f, 1.25f, .8f })
                foreach (var direction in new[] { Vector2.one, -Vector2.one, new Vector2(-1, 1), new Vector2(1, -1) })
                {
                    camera.ZoomAt(viewport.center, factor);
                    camera.Pan += direction * 100000;
                    camera.Constrain(Board, viewport);
                    Covers(camera, viewport);
                    Assert.That(camera.Zoom, Is.InRange(camera.MinimumZoom, camera.MaximumZoom));
                }
        }

        [Test]
        public void ZoomKeepsThePointUnderTheCursorUntilTheRasterEdgeConstrainsIt()
        {
            var viewport = new Rect(0, 0, 1000, 650);
            var camera = new CameraState { Zoom = 1.4f };
            camera.Pan = viewport.center - Board.center * camera.Zoom;
            camera.Constrain(Board, viewport);
            var cursor = viewport.center + new Vector2(31, -27);
            var original = camera.ToBoard(cursor);
            camera.ZoomAt(cursor, 1.25f);
            Assert.That(Vector2.Distance(original, camera.ToBoard(cursor)), Is.LessThan(.001f));
            camera.ZoomAt(cursor, .001f);
            Covers(camera, viewport);
        }

        [Test]
        public void ResizeRaisesTheMinimumZoomAndClampsTheExistingPosition()
        {
            var camera = new CameraState { Zoom = .6f, Pan = new Vector2(-70, -120) };
            camera.Constrain(Board, new Rect(0, 0, 600, 500));
            var wide = new Rect(0, 0, 3840, 1080);
            camera.Constrain(Board, wide);
            Covers(camera, wide);
            var tall = new Rect(0, 0, 800, 4000);
            camera.Constrain(Board, tall);
            Covers(camera, tall);
            Assert.GreaterOrEqual(camera.MaximumZoom, camera.MinimumZoom);
        }

        [Test]
        public void RoundedCoverCannotAlternatePanBetweenInvertedBoundsOnRefresh()
        {
            foreach (var raster in new[] { new Rect(0, 0, 1632, 1338), Board })
            {
                var viewport = new Rect(0, 0, 1366, 768);
                var camera = new CameraState { Zoom = .01f };
                camera.Constrain(raster, viewport);
                Assert.GreaterOrEqual((double)camera.Zoom * raster.width, viewport.width);
                Assert.GreaterOrEqual((double)camera.Zoom * raster.height, viewport.height);
                camera.Pan = viewport.center - raster.center * camera.Zoom;
                camera.Constrain(raster, viewport);
                var pan = camera.Pan;
                for (var refresh = 0; refresh < 30; refresh++)
                {
                    camera.Constrain(raster, viewport);
                    Assert.AreEqual(pan.x, camera.Pan.x, "Refresh must be exactly idempotent.");
                    Assert.AreEqual(pan.y, camera.Pan.y, "Refresh must be exactly idempotent.");
                }
            }
        }

        [Test]
        public void RepeatedSubpixelLayoutKeepsTheCameraExactAndStillConstrainsInputAndResize()
        {
            var viewport = new Rect(11, 23, 1092.8f, 614.4f);
            var camera = new CameraState { Zoom = .01f, Pan = Vector2.one * -100000 };
            camera.Constrain(Board, viewport);
            var pan = camera.Pan;
            var zoom = camera.Zoom;
            var minimum = camera.MinimumZoom;
            for (var index = 0; index < 30; index++)
            {
                var offset = index % 2 == 0 ? .0002f : -.0002f;
                var jitter = Rect.MinMaxRect(viewport.xMin + offset, viewport.yMin - offset,
                    viewport.xMax - offset, viewport.yMax + offset);
                camera.Constrain(Board, jitter);
                Assert.AreEqual(pan.x, camera.Pan.x);
                Assert.AreEqual(pan.y, camera.Pan.y);
                Assert.AreEqual(zoom, camera.Zoom);
                Assert.AreEqual(minimum, camera.MinimumZoom);
                Covers(camera, jitter);
            }
            camera.Pan = Vector2.one * 100000;
            camera.Zoom = .001f;
            camera.Constrain(Board, viewport);
            Assert.AreEqual(minimum, camera.Zoom);
            Covers(camera, viewport);
            camera.Pan = Vector2.one * -100000;
            camera.Zoom = 100000;
            camera.Constrain(Board, viewport);
            Assert.AreEqual(camera.MaximumZoom, camera.Zoom);
            Covers(camera, viewport);

            var resized = new Rect(11, 23, 1920, 1080);
            camera.Zoom = minimum;
            camera.Constrain(Board, resized);
            Assert.AreEqual(CameraState.CoverZoom(Board, resized), camera.MinimumZoom);
            Assert.Greater(camera.MinimumZoom, minimum);
            Covers(camera, resized);
        }

        [TestCase(1, 400, 1, 0)]
        [TestCase(999, 400, -1, 0)]
        [TestCase(500, 1, 0, 1)]
        [TestCase(500, 799, 0, -1)]
        [TestCase(1, 1, 1, 1)]
        [TestCase(999, 799, -1, -1)]
        public void EdgePanMovesTowardThePointerWithoutFasterDiagonals(float x, float y, float dx, float dy)
        {
            var result = MapEdgePan.VelocityAt(new Vector2(x, y), new Rect(0, 0, 1000, 800));
            Assert.That(Vector2.Distance(result.normalized, new Vector2(dx, dy).normalized), Is.LessThan(.001f));
            Assert.LessOrEqual(result.magnitude, 650.01f);
        }

        [Test]
        public void EdgePanIsIdleInTheMiddleAndOutsideAndSlowsNearItsInnerLimit()
        {
            var area = new Rect(0, 0, 1000, 800);
            Assert.AreEqual(Vector2.zero, MapEdgePan.VelocityAt(area.center, area));
            Assert.AreEqual(Vector2.zero, MapEdgePan.VelocityAt(new Vector2(-1, 400), area));
            Assert.Greater(MapEdgePan.VelocityAt(new Vector2(1, 400), area).magnitude,
                MapEdgePan.VelocityAt(new Vector2(20, 400), area).magnitude);
        }

        [TestCase(0f, .5f, 1f, 0f)]
        [TestCase(1f, .5f, -1f, 0f)]
        [TestCase(.5f, 0f, 0f, 1f)]
        [TestCase(.5f, 1f, 0f, -1f)]
        [TestCase(0f, 0f, 1f, 1f)]
        [TestCase(1f, 0f, -1f, 1f)]
        [TestCase(0f, 1f, 1f, -1f)]
        [TestCase(1f, 1f, -1f, -1f)]
        public void ExactViewportEdgesAndCornersHaveSymmetricVelocityAtEveryLayout(float x, float y, float dx, float dy)
        {
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                    foreach (var origin in new[] { Vector2.zero, new Vector2(11.25f, 23.5f) })
                    {
                        // Binary-exact origins also test equality with Contains' internal
                        // max calculation; a rounded-down decimal offset can lie inside it.
                        var area = new Rect(origin.x, origin.y, width / scale, (width == 1366 ? 768 : 1080) / scale);
                        var point = new Vector2(x == 0 ? area.xMin : x == 1 ? area.xMax : area.center.x,
                            y == 0 ? area.yMin : y == 1 ? area.yMax : area.center.y);
                        var result = MapEdgePan.VelocityAt(point, area);
                        var expected = new Vector2(dx, dy).normalized * 650f;
                        Assert.That(Vector2.Distance(result, expected), Is.LessThan(.01f),
                            $"Exact edge {point} at {width}/{scale} must match its opposite edge.");
                        var outside = point - new Vector2(dx, dy).normalized * .1f;
                        Assert.AreEqual(Vector2.zero, MapEdgePan.VelocityAt(outside, area),
                            "Outside the viewport remains idle; symmetric edges must not expand the screen.");
                    }
        }

        [Test]
        public void RecordedMovementBesidePinnedCardRaisesZoomBeforeChoosingLegalPan()
        {
            var raster = new Rect(0, 0, 1632, 1338);
            var viewport = new Rect(0, 0, 1092.8f, 614.4f);
            var safe = Rect.MinMaxRect(425.6f, 74.4f, 683.2f, 502.4f);
            var targets = Rect.MinMaxRect(828, 762, 1039, 1030);
            var camera = new CameraState { Zoom = .01f };
            camera.Constrain(raster, viewport);
            Assert.Greater(camera.ToScreen(targets.max).x, safe.xMax);
            Assert.IsTrue(camera.TryFrame(targets, safe, safe.width / (targets.width + 200)));
            Assert.Greater(camera.Zoom, camera.MinimumZoom);
            Inside(camera, targets, safe);
            Inside(camera, raster, viewport, covers: true);
            var pan = camera.Pan;
            var zoom = camera.Zoom;
            camera.Constrain(raster, viewport);
            Assert.AreEqual(pan, camera.Pan);
            Assert.AreEqual(zoom, camera.Zoom);
        }

        [TestCase(50, 150, 450, 500)]
        [TestCase(550, 150, 450, 500)]
        [TestCase(150, 50, 800, 250)]
        [TestCase(150, 500, 800, 250)]
        public void FramingIntersectsEverySafeEdgeWithTheOffsetRaster(float x, float y, float width, float height)
        {
            var viewport = new Rect(11, 23, 1100, 800);
            var safe = new Rect(x, y, width, height);
            var targets = Rect.MinMaxRect(720, 570, 920, 750);
            var camera = new CameraState();
            camera.Constrain(Board, viewport);
            Assert.IsTrue(camera.TryFrame(targets, safe, .01f));
            Inside(camera, targets, safe);
            Covers(camera, viewport);
        }

        [Test]
        public void ImpossiblePinnedGapLeavesCameraUntouchedAndSingleAnchorCanStillBeFramed()
        {
            var viewport = new Rect(11, 23, 1100, 800);
            var safe = new Rect(400, 300, 150, 150);
            var camera = new CameraState { Zoom = 1.1f, Pan = new Vector2(-80, -120) };
            camera.Constrain(Board, viewport);
            var pan = camera.Pan;
            var zoom = camera.Zoom;
            Assert.IsFalse(camera.TryFrame(Board, safe, 1.5f));
            Assert.AreEqual(pan, camera.Pan);
            Assert.AreEqual(zoom, camera.Zoom);
            var anchor = new Rect(830, 690, 0, 0);
            Assert.IsTrue(camera.TryFrame(anchor, safe, 1.5f));
            Assert.IsTrue(safe.Contains(camera.ToScreen(anchor.position)));
            Covers(camera, viewport);
        }

        private static void Inside(CameraState camera, Rect target, Rect area, bool covers = false)
        {
            var actual = new Rect(camera.ToScreen(target.min), target.size * camera.Zoom);
            var outer = covers ? actual : area;
            var inner = covers ? area : actual;
            Assert.LessOrEqual(outer.xMin, inner.xMin + .001f);
            Assert.LessOrEqual(outer.yMin, inner.yMin + .001f);
            Assert.GreaterOrEqual(outer.xMax, inner.xMax - .001f);
            Assert.GreaterOrEqual(outer.yMax, inner.yMax - .001f);
        }

        private static void Covers(CameraState camera, Rect viewport)
        {
            var min = camera.ToScreen(Board.min);
            var max = camera.ToScreen(Board.max);
            Assert.LessOrEqual(min.x, viewport.xMin + .001f);
            Assert.LessOrEqual(min.y, viewport.yMin + .001f);
            Assert.GreaterOrEqual(max.x, viewport.xMax - .001f);
            Assert.GreaterOrEqual(max.y, viewport.yMax - .001f);
        }
    }
}
