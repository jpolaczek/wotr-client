using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    public class MapContourRenderingTests
    {
        private UIDocument document;
        private RenderTexture target;
        private float originalScale;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (document != null)
            {
                document.panelSettings.targetTexture = null;
                document.panelSettings.scale = originalScale;
            }
            if (target != null)
            {
                target.Release();
                Object.Destroy(target);
                target = null;
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator TerrainAndSharedBordersRenderBeforeAndAfterEveryEmphasis()
        {
            var map = CreateMap(false);
            var data = map.Data;
            for (var frame = 0; frame < 12; frame++) yield return null;
            map.Fit();
            for (var frame = 0; frame < 5; frame++) yield return null;
            Assert.Greater(map.Renderer.BorderCount, 200);
            Assert.AreEqual(map.Renderer.BorderCount, map.Renderer.BorderBuilds);
            Assert.AreEqual(4096, map.Renderer.Terrain.width);
            Assert.AreEqual(4096, map.Renderer.Terrain.height);
            Assert.IsFalse(map.Renderer.Terrain.isReadable);
            var terrain = map.Renderer.Terrain;
            Debug.Log("MAP14_TEXTURE " + JsonConvert.SerializeObject(new
            {
                terrain.width, terrain.height, Format = terrain.format.ToString(),
                GraphicsFormat = terrain.graphicsFormat.ToString(), terrain.mipmapCount,
                Editor = Application.isEditor, ProfilerBytes = Profiler.GetRuntimeMemorySizeLong(terrain),
                BlockBytes = CompressedBytes(terrain), SupportsDxt = SystemInfo.SupportsTextureFormat(TextureFormat.DXT1)
            }));
#if UNITY_EDITOR
            Debug.Log("MAP14_IMPORT " + UnityEditor.EditorJsonUtility.ToJson(
                UnityEditor.AssetImporter.GetAtPath("Assets/Wotr/Resources/MapCanvas/terrain.jpg")));
#endif
            Assert.AreEqual(TextureFormat.DXT1, terrain.format, "Explicit desktop import must retain BC1 compression.");
            // UI-102: the canvas margins share the terrain's import; the flat water layer is gone.
            var margins = map.Renderer.Margins;
            Assert.AreEqual(4096, margins.width);
            Assert.AreEqual(4096, margins.height);
            Assert.AreEqual(TextureFormat.DXT1, margins.format);
            Assert.IsFalse(margins.isReadable);
            Assert.IsNull(map.Renderer.Root.Q("map-water"), "Water is painted into the canvas rasters.");
            Assert.IsTrue(GraphicsFormatUtility.IsCompressedFormat(terrain.graphicsFormat),
                "The actual graphics texture must use a compressed format.");
            Assert.Less(CompressedBytes(terrain), 20 * 1024 * 1024);
            if (!Application.isEditor)
                Assert.Less(Profiler.GetRuntimeMemorySizeLong(terrain), 20 * 1024 * 1024);
            var builds = map.Renderer.BorderBuilds;
            var staticRepaints = map.Renderer.BorderRepaints;
            LayoutTests.Save(target, "map14-whole-before.png");
            AssertPassagesVisible(map, target);

            foreach (var region in new[] { "morannon", "minas-morgul", "erebor", "moria", "high-pass", "grey-havens", "nurn" })
            {
                Center(map, region, 1.4f);
                for (var frame = 0; frame < 4; frame++) yield return null;
                LayoutTests.Save(target, "map14-" + region + "-before.png");
                var before = Capture(target);
                map.DraftOverlay.Targets.Add(region);
                map.DraftOverlay.AttackTargets.Add(region);
                data.ProtectedRegions.Add(region);
                map.SetAttention(region);
                map.Refresh();
                for (var frame = 0; frame < 4; frame++) yield return null;
                Assert.AreEqual(MapRegionEmphasis.Attack, map.Renderer.CurrentEmphasis(region));
                Assert.AreEqual(builds, map.Renderer.BorderBuilds, "Highlights must not retessellate static borders.");
                Assert.AreEqual(staticRepaints, map.Renderer.BorderRepaints,
                    "Pan, zoom and highlights must retain each geographic mesh.");
                AssertHighlightAtRegion(map, region, target, before);
                LayoutTests.Save(target, "map14-" + region + "-after.png");
                map.DraftOverlay.Targets.Clear();
                map.DraftOverlay.AttackTargets.Clear();
                data.ProtectedRegions.Clear();
                map.SetAttention("");
            }
            map.HighlightOwners(System.Array.Empty<string>());
            map.Fit();
            for (var frame = 0; frame < 4; frame++) yield return null;
            var metrics = new
            {
                Texture = new { map.Renderer.Terrain.width, map.Renderer.Terrain.height, Format = map.Renderer.Terrain.format.ToString(),
                    GraphicsFormat = map.Renderer.Terrain.graphicsFormat.ToString(),
                    BlockBytes = CompressedBytes(map.Renderer.Terrain),
                    Bytes = Profiler.GetRuntimeMemorySizeLong(map.Renderer.Terrain), map.Renderer.Terrain.mipmapCount },
                map.Renderer.BorderCount, map.Renderer.BorderBuilds,
                map.Renderer.InputPoints,
                map.Renderer.BorderRepaints,
                map.Renderer.InitialLayerDrawMilliseconds,
                map.Renderer.LastUpdateMilliseconds
            };
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../.artifacts"));
            File.WriteAllText(Path.Combine(folder, "map14-render-metrics.json"), JsonConvert.SerializeObject(metrics, Formatting.Indented));
        }

        [UnityTest]
        public IEnumerator LegendIsExplicitAndDoesNotChooseARegionOrChangeBorders()
        {
            var map = CreateMap(false);
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    ResizeViewport(width, scale);
                    for (var frame = 0; frame < 8; frame++) yield return null;
                    var selected = map.Selected;
                    var pan = map.Camera.Pan;
                    var builds = map.Renderer.BorderBuilds;
                    Assert.AreEqual(DisplayStyle.None, map.Legend.Popup.resolvedStyle.display);
                    AssertVisibleToPointer(map.Legend.Toggle);
                    PointerGestureEvents.ClickAt(map.Legend.Toggle);
                    for (var frame = 0; frame < 4; frame++) yield return null;
                    Assert.AreEqual(DisplayStyle.Flex, map.Legend.Popup.resolvedStyle.display);
                    AssertLegendVisible(map);
                    LayoutTests.Save(target, $"map14-legend-{width}-{scale:0.00}.png");
                    PointerGestureEvents.ClickAt(map.Legend.Popup.Q<Label>("map-legend-heading"));
                    Assert.AreEqual(selected, map.Selected);
                    Assert.AreEqual(pan, map.Camera.Pan);
                    Assert.AreEqual(builds, map.Renderer.BorderBuilds);
                    using (var key = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape }))
                        map.Legend.Popup.SendEvent(key);
                    using (var key = KeyUpEvent.GetPooled(new Event { type = EventType.KeyUp, keyCode = KeyCode.Escape }))
                        map.Legend.Popup.SendEvent(key);
                    yield return null;
                    Assert.AreEqual(DisplayStyle.None, map.Legend.Popup.resolvedStyle.display);
                }
        }

        private static void AssertLegendVisible(MapCanvas map)
        {
            var bounds = map.Legend.Popup.worldBound;
            Assert.IsTrue(map.worldBound.Contains(bounds.min + Vector2.one));
            Assert.IsTrue(map.worldBound.Contains(bounds.max - Vector2.one));
            AssertVisibleToPointer(map.Legend.Popup.Q<Label>("map-legend-heading"));
            foreach (var nation in map.Data.Borders.Palette.Style.Nations)
            {
                AssertVisibleToPointer(map.Legend.Popup.Q<Label>("map-legend-nation-" + nation.Id));
                AssertVisibleToPointer(map.Legend.Popup.Q("map-legend-swatch-" + nation.Id));
            }
        }

        private static void AssertVisibleToPointer(VisualElement element)
        {
            Assert.Greater(element.worldBound.width, 0, element.name);
            Assert.Greater(element.worldBound.height, 0, element.name);
            var picked = element.panel.Pick(element.worldBound.center);
            Assert.IsTrue(picked == element || element.Contains(picked),
                element.name + " is obscured by " + picked?.name);
        }

        private void ResizeViewport(int width, float scale)
        {
            document.panelSettings.targetTexture = null;
            target.Release();
            Object.Destroy(target);
            target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
            target.Create();
            document.panelSettings.targetTexture = target;
            document.panelSettings.scale = scale;
        }

        [UnityTest]
        public IEnumerator EveryHighlightStaysBelowPrintedBarriersAndInsideItsRegion()
        {
            var map = CreateMap(true);
            for (var frame = 0; frame < 12; frame++) yield return null;
            Center(map, "morannon", 1.4f);
            for (var frame = 0; frame < 5; frame++) yield return null;
            var staticRepaints = map.Renderer.BorderRepaints;
            foreach (var emphasis in new[] { MapRegionEmphasis.Source, MapRegionEmphasis.Selected,
                MapRegionEmphasis.Target, MapRegionEmphasis.Attack, MapRegionEmphasis.Protection })
            {
                SetEmphasis(map, MapRegionEmphasis.None);
                for (var frame = 0; frame < 3; frame++) yield return null;
                var before = Capture(target);
                SetEmphasis(map, emphasis);
                for (var frame = 0; frame < 3; frame++) yield return null;
                Assert.AreEqual(emphasis, map.Renderer.CurrentEmphasis("morannon"));
                Assert.AreEqual(staticRepaints, map.Renderer.BorderRepaints);
                var after = Capture(target);
                AssertInteriorChanged(map, "morannon", target, before, after, emphasis);
                AssertBarriersUnchanged(map, "morannon", target, before, after);
                LayoutTests.Save(target, "map14-emphasis-" + emphasis + ".png");
            }
        }

        private MapCanvas CreateMap(bool isolated)
        {
            var bootstrap = Object.FindAnyObjectByType<ClientBootstrap>() ??
                new GameObject("Map render test").AddComponent<ClientBootstrap>();
            bootstrap.ShowFixture();
            document = bootstrap.GetComponent<UIDocument>();
            originalScale = document.panelSettings.scale;
            target = new RenderTexture(1920, 1080, 24);
            target.Create();
            document.panelSettings.targetTexture = target;
            var data = BoardPresentation.Load();
            data.UseSetup();
            var map = bootstrap.BoardScreen.Map;
            if (isolated)
            {
                document.rootVisualElement.Clear();
                map = new MapCanvas(data) { style = { flexGrow = 1 } };
                document.rootVisualElement.Add(map);
            }
            map.UpdateData(data);
            map.DraftOverlay.Targets.Clear();
            map.DraftOverlay.AttackTargets.Clear();
            return map;
        }

        private static void SetEmphasis(MapCanvas map, MapRegionEmphasis emphasis)
        {
            map.DraftOverlay.Targets.Clear();
            map.DraftOverlay.AttackTargets.Clear();
            map.Data.ProtectedRegions.Clear();
            map.HighlightOwners(emphasis == MapRegionEmphasis.Source ? new[] { "morannon" } : System.Array.Empty<string>());
            map.Select(emphasis == MapRegionEmphasis.Selected ? "morannon" : "");
            if (emphasis == MapRegionEmphasis.Target)
                map.DraftOverlay.Targets.Add("morannon");
            if (emphasis == MapRegionEmphasis.Attack)
                map.DraftOverlay.AttackTargets.Add("morannon");
            if (emphasis == MapRegionEmphasis.Protection)
                map.Data.ProtectedRegions.Add("morannon");
            map.Refresh();
        }

        private static void AssertInteriorChanged(MapCanvas map, string region, RenderTexture target,
            Color32[] before, Color32[] after, MapRegionEmphasis emphasis)
        {
            var samples = 0;
            var changed = 0;
            for (var y = 30f; y < map.contentRect.height - 30; y += 18)
                for (var x = 30f; x < map.contentRect.width - 30; x += 18)
                {
                    var local = new Vector2(x, y);
                    if (!MapContourMath.Contains(map.Data.Shapes[region], map.Camera.ToBoard(local)) ||
                        map.MarkerBounds(region).Any(bounds => bounds.Contains(local)))
                        continue;
                    var pixel = Pixel(map, target, local);
                    var index = pixel.y * target.width + pixel.x;
                    samples++;
                    if (Difference(before[index], after[index]) > 8)
                        changed++;
                }
            Assert.Greater(samples, 20);
            Assert.Greater(changed, samples * .6f, emphasis + " must fill the region below borders.");
        }

        private static void AssertBarriersUnchanged(MapCanvas map, string region, RenderTexture target,
            Color32[] before, Color32[] after)
        {
            var samples = 0;
            var changed = 0;
            foreach (var path in map.Data.Borders.Catalog.Paths.Where(p =>
                p.PositiveNormalOwner == region || p.NegativeNormalOwner == region))
                foreach (var part in path.Parts.Where(p => p.Kind == MapBorderKind.Impassable))
                    for (var index = part.FromSegment; index < part.ToSegment; index += 3)
                    {
                        var point = map.Data.Borders.Geometry[path.Id].Points[index];
                        var local = map.Camera.ToScreen(new Vector2(point[0], point[1]));
                        if (!map.contentRect.Contains(local))
                            continue;
                        var pixel = Pixel(map, target, local);
                        var offset = pixel.y * target.width + pixel.x;
                        var color = before[offset];
                        if (color.r > 65 || color.g > 65 || color.b > 65)
                            continue;
                        samples++;
                        if (Difference(color, after[offset]) > 3)
                            changed++;
                    }
            Assert.Greater(samples, 15, "The review needs visible black barrier samples.");
            Assert.LessOrEqual(changed, samples * .05f, "Highlights must not recolor printed barriers.");
        }

        private static int Difference(Color32 a, Color32 b) =>
            Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b));

        private static long CompressedBytes(Texture2D texture)
        {
            var bytes = 0L;
            for (var level = 0; level < texture.mipmapCount; level++)
            {
                var width = Mathf.Max(1, texture.width >> level);
                var height = Mathf.Max(1, texture.height >> level);
                bytes += (long)Mathf.CeilToInt(width / 4f) * Mathf.CeilToInt(height / 4f) * 8;
            }
            return bytes;
        }

        private static Color32[] Capture(RenderTexture target)
        {
            var previous = RenderTexture.active;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                texture.Apply();
                return texture.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                Object.Destroy(texture);
            }
        }

        private static void AssertPassagesVisible(MapCanvas map, RenderTexture target)
        {
            var pixels = Capture(target);
            var matches = 0;
            foreach (var path in map.Data.Borders.Catalog.Paths)
                foreach (var part in path.Parts.Where(part => MapBorderData.Passable(part.Kind)))
                {
                    var points = map.Data.Borders.Geometry[path.Id].Points;
                    var index = (part.FromSegment + part.ToSegment) / 2;
                    var midpoint = (new Vector2(points[index][0], points[index][1]) +
                        new Vector2(points[index + 1][0], points[index + 1][1])) / 2;
                    var point = Pixel(map, target, map.Camera.ToScreen(midpoint));
                    if (WhiteNear(pixels, target, point))
                        matches++;
                }
            Assert.Greater(matches, 120, "White passages must render at shared boundary coordinates.");
        }

        private static bool WhiteNear(Color32[] pixels, RenderTexture target, Vector2Int point)
        {
            for (var y = point.y - 1; y <= point.y + 1; y++)
                for (var x = point.x - 1; x <= point.x + 1; x++)
                {
                    if (x < 0 || y < 0 || x >= target.width || y >= target.height)
                        continue;
                    var color = pixels[y * target.width + x];
                    if (color.r > 238 && color.g > 230 && color.b > 215)
                        return true;
                }
            return false;
        }

        private static void AssertHighlightAtRegion(MapCanvas map, string region, RenderTexture target, Color32[] before)
        {
            var after = Capture(target);
            var checkedPixels = 0;
            var redderPixels = 0;
            var shape = map.Data.Shapes[region];
            // UI-126: armies are always drawn, so a neighbour's figures may stand over this region too.
            var markers = map.Data.Armies.Select(a => a.RegionId).Distinct().SelectMany(map.MarkerBounds).ToArray();
            for (var y = 80f; y < map.contentRect.height - 20; y += 18)
                for (var x = 20f; x < map.contentRect.width - 20; x += 18)
                {
                    var local = new Vector2(x, y);
                    if (!MapContourMath.Contains(shape, map.Camera.ToBoard(local)) ||
                        markers.Any(bounds => bounds.Contains(local)))
                        continue;
                    var point = Pixel(map, target, local);
                    var index = point.y * target.width + point.x;
                    var oldColor = before[index];
                    var newColor = after[index];
                    // Printed barriers stay above every highlight by design; their black ink cannot turn red.
                    if (oldColor.r < 40 && oldColor.g < 40 && oldColor.b < 40)
                        continue;
                    checkedPixels++;
                    if (newColor.g + 8 < oldColor.g && newColor.b + 8 < oldColor.b)
                        redderPixels++;
                }
            Assert.Greater(checkedPixels, 5, region + " needs visible interior pixels.");
            Assert.Greater(redderPixels, checkedPixels * .6f, region + " must receive its own attack fill.");
        }

        private static Vector2Int Pixel(MapCanvas map, RenderTexture target, Vector2 local)
        {
            var world = map.LocalToWorld(local);
            var scale = target.width / map.panel.visualTree.worldBound.width;
            return new Vector2Int(Mathf.Clamp(Mathf.RoundToInt(world.x * scale), 0, target.width - 1),
                Mathf.Clamp(target.height - 1 - Mathf.RoundToInt(world.y * scale), 0, target.height - 1));
        }

        private static void Center(MapCanvas map, string region, float zoom)
        {
            var anchor = map.Data.Shapes[region].Anchor;
            map.Camera.Zoom = zoom;
            map.Camera.Pan = map.contentRect.center - new Vector2(anchor[0], anchor[1]) * zoom;
            map.Refresh();
        }
    }
}
