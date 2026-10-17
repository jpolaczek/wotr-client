#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Wotr.Contracts;
using Debug = UnityEngine.Debug;

namespace Wotr.Client.Diagnostics
{
    // Opt-in offline visual review. Uses the real board and setup, without touching account profiles.
    public sealed class MapRenderProfile : MonoBehaviour
    {
        private ClientBootstrap bootstrap;
        private PanelSettings panel;
        private string output;
        private string inputPath;
        private Dictionary<string, PlayerView> views;
        private MapProfileScene scene;
        private DesktopProfileInput input;
        private object geometryMemory;
        private readonly List<object> results = new List<object>();
        private readonly List<string> screenshots = new List<string>();
        private readonly WaitForEndOfFrame end = new WaitForEndOfFrame();

        public static bool TryStart(GameObject host, ClientBootstrap bootstrap, PanelSettings panel)
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "--wotr-map-review");
            if (index < 0)
                return false;
            if (index + 1 >= args.Length)
                throw new ArgumentException("--wotr-map-review requires an output directory.");
            var review = host.AddComponent<MapRenderProfile>();
            review.bootstrap = bootstrap;
            review.panel = panel;
            review.output = Path.GetFullPath(args[index + 1]);
            var inputIndex = Array.IndexOf(args, "--wotr-map-input");
            if (inputIndex < 0 || inputIndex + 1 >= args.Length)
                throw new ArgumentException("--wotr-map-input requires the public scenario projection artifact.");
            review.inputPath = Path.GetFullPath(args[inputIndex + 1]);
            Directory.CreateDirectory(review.output);
            review.StartCoroutine(review.GuardedRun());
            return true;
        }

        private IEnumerator GuardedRun()
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(Run());
            while (stack.Count > 0)
            {
                object current;
                try
                {
                    var work = stack.Peek();
                    if (!work.MoveNext())
                    {
                        (stack.Pop() as IDisposable)?.Dispose();
                        continue;
                    }
                    current = work.Current;
                    if (current is IEnumerator nested)
                    {
                        stack.Push(nested);
                        continue;
                    }
                }
                catch (Exception error)
                {
                    while (stack.Count > 0)
                        (stack.Pop() as IDisposable)?.Dispose();
                    Debug.LogError("WOTR_MAP_REVIEW_FAILED " + error);
                    Application.Quit(1);
                    yield break;
                }
                yield return current;
            }
            Debug.Log("WOTR_MAP_REVIEW_COMPLETE");
            Application.Quit(0);
        }

        private IEnumerator Run()
        {
            yield return null;
            geometryMemory = MapProfileMemory.LoadGeometry();
            views = JsonConvert.DeserializeObject<Dictionary<string, PlayerView>>(File.ReadAllText(inputPath));
            foreach (var name in new[] { "base", "fate", "newcities", "captured", "whitehand", "dense", "moved" })
                if (!views.ContainsKey(name))
                    throw new InvalidOperationException("Missing map profile projection: " + name);
            bootstrap.ShowFixture();
            var board = bootstrap.BoardScreen;
            var map = board.Map;
            using var ownedScene = new MapProfileScene(board, views);
            scene = ownedScene;
            using var ownedInput = new DesktopProfileInput(board.Root);
            input = ownedInput;
            scene.Prepare("idle");
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                    yield return ProfileViewport(map, width, scale);
            yield return ProfileResolution.Apply(1920, 1080);
            panel.scale = 1;
            for (var frame = 0; frame < 12; frame++)
                yield return null;
            yield return CaptureRegions(map);
            WriteReport(map);
        }

        private IEnumerator ProfileViewport(MapCanvas map, int width, float scale)
        {
            var height = width == 1366 ? 768 : 1080;
            yield return ProfileResolution.Apply(width, height);
            panel.scale = scale;
            for (var frame = 0; frame < 30; frame++)
                yield return null;
            if (!ProfileResolution.Matches(width, height))
                throw new InvalidOperationException($"Map review resolution mismatch: {Screen.width}×{Screen.height} " +
                    $"({ProfileResolution.Mode}) instead of {width}×{height}.");
            scene.Prepare("idle");
            map.Fit();
            for (var frame = 0; frame < 8; frame++)
                yield return null;
            yield return end;
            Save($"whole-{width}-{scale:0.00}");
            foreach (var kind in MapProfileScene.Kinds)
                yield return ProfileCase(map, width, height, scale, kind);
            scene.Prepare("idle");
            Center(map, "morannon", 2.6f);
            for (var frame = 0; frame < 8; frame++)
                yield return null;
            yield return end;
            Save($"maximum-zoom-{width}-{scale:0.00}");
            map.Fit();
            map.Legend.SetOpen(true);
            for (var frame = 0; frame < 8; frame++)
                yield return null;
            yield return end;
            Save($"legend-{width}-{scale:0.00}");
            map.Legend.SetOpen(false);
        }

        private IEnumerator ProfileCase(MapCanvas map, int width, int height, float scale, string kind)
        {
            scene.Prepare(kind);
            for (var settle = 0; settle < 8; settle++)
                yield return null;
            using var profile = new FrameProfile();
            using var memory = new MapProfileMemory();
            using var vertices = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count", 1);
            var count = map.Renderer.BorderRepaints;
            var builds = map.Renderer.BorderBuilds;
            var vertexPeak = 0L;
            for (var frame = 0; frame < FrameProfile.WarmupFrames + FrameProfile.SampleFrames; frame++)
            {
                var sample = frame - FrameProfile.WarmupFrames;
                if (sample >= 0)
                    profile.Frame(sample);
                scene.SetFrame(frame);
                var start = Stopwatch.GetTimestamp();
                input.Dispatch(scene.TickAction);
                var handler = Milliseconds(start);
                yield return end;
                if (sample >= 0 && kind != "idle")
                    profile.Action(handler, Milliseconds(start));
                if (vertices.Valid && vertices.Count > 0)
                    vertexPeak = Math.Max(vertexPeak, vertices.LastValue);
                memory.Sample();
                yield return null;
            }
            var actualBuilds = map.Renderer.BorderBuilds - builds;
            if (actualBuilds != scene.ExpectedBorderBuilds)
                throw new InvalidOperationException($"Unexpected border refresh in {kind}: {actualBuilds} != {scene.ExpectedBorderBuilds}");
            results.Add(new
            {
                Kind = kind, Width = width, Height = height, Scale = scale, WindowMode = ProfileResolution.Mode,
                Metrics = profile.Result(), map.Renderer.InputPoints, map.Renderer.LastUpdateMilliseconds,
                BorderRepaints = map.Renderer.BorderRepaints - count,
                BorderBuilds = actualBuilds, ExpectedBorderBuilds = scene.ExpectedBorderBuilds,
                GraphicsVertices = new { CounterValid = vertices.Valid, Peak = vertexPeak },
                Memory = memory.Result()
            });
            if (kind.StartsWith("stress-", StringComparison.Ordinal) || kind.StartsWith("override-", StringComparison.Ordinal))
            {
                yield return end;
                Save($"{kind}-{width}-{scale:0.00}");
            }
            Debug.Log($"WOTR_MAP_REVIEW_CASE {kind} {width} {scale}");
        }

        private IEnumerator CaptureRegions(MapCanvas map)
        {
            foreach (var id in new[] { "morannon", "minas-morgul", "erebor", "moria", "high-pass", "grey-havens", "nurn" })
            {
                Center(map, id, 1.4f);
                for (var frame = 0; frame < 8; frame++)
                    yield return null;
                yield return end;
                Save(id + "-before");
                map.DraftOverlay.AttackTargets.Add(id);
                map.SetAttention(id);
                map.Refresh();
                for (var frame = 0; frame < 8; frame++)
                    yield return null;
                yield return end;
                Save(id + "-after");
                map.DraftOverlay.AttackTargets.Clear();
                map.SetAttention("");
            }
        }

        private static void Center(MapCanvas map, string region, float zoom) => MapProfileScene.Center(map, region, zoom);

        private void Save(string name)
        {
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            try
            {
                texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                texture.Apply();
                var file = name + ".png";
                File.WriteAllBytes(Path.Combine(output, file), texture.EncodeToPNG());
                screenshots.Add(file);
            }
            finally
            {
                Destroy(texture);
            }
        }

        private void WriteReport(MapCanvas map)
        {
            var texture = map.Renderer.Terrain;
            var report = new
            {
                Version = "map-profile-2",
                RecordedAt = DateTime.UtcNow, Unity = Application.unityVersion,
                Development = Debug.isDebugBuild, Editor = Application.isEditor,
                Platform = Application.platform.ToString(), OS = SystemInfo.operatingSystem,
                CPU = SystemInfo.processorType, GPU = SystemInfo.graphicsDeviceName,
                MemoryMB = SystemInfo.systemMemorySize, GraphicsAPI = SystemInfo.graphicsDeviceType.ToString(),
                TargetFPS = Application.targetFrameRate, VSync = QualitySettings.vSyncCount,
                Geometry = map.Data.Geometry.Version, Borders = map.Data.Borders.Catalog.Version,
                Texture = new { texture.width, texture.height, Format = texture.format.ToString(), GraphicsFormat = texture.graphicsFormat.ToString(),
                    Bytes = Profiler.GetRuntimeMemorySizeLong(texture), texture.mipmapCount },
                map.Renderer.BorderCount, map.Renderer.InputPoints,
                GeometryMemory = geometryMemory,
                Workload = "Real offline Player UI and TurnInteraction with server-generated safe projections, dense field/garrison, pointer and projection/metadata refresh. Only match ID and revision use a monotonic replay envelope; game data and legal options are unchanged. Network behavior is verified separately. Uncapped FPS is not monitor refresh; input-to-render excludes display latency.",
                Results = results, Screenshots = screenshots
            };
            File.WriteAllText(Path.Combine(output, "report.json"), JsonConvert.SerializeObject(report, Formatting.Indented));
        }

        private static double Milliseconds(long start) => (Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency;
    }
}
#endif
