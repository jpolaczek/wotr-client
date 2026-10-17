#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;
using Debug = UnityEngine.Debug;

namespace Wotr.Client.Diagnostics
{
    public sealed class DesktopProfile : MonoBehaviour
    {
        private readonly List<object> results = new List<object>();
        private InterfaceController ui;
        private PanelSettings panel;
        private string input;
        private string output;
        private string inputSha256;
        private string suite = "legacy";

        public static bool TryStart(GameObject host, InterfaceController ui, PanelSettings panel)
        {
            var args = Environment.GetCommandLineArgs();
            var position = Array.IndexOf(args, "--wotr-profile");
            if (position < 0)
                return false;
            if (position + 2 >= args.Length)
                throw new ArgumentException("--wotr-profile requires projection and report paths.");
            var profile = host.AddComponent<DesktopProfile>();
            profile.ui = ui;
            profile.panel = panel;
            profile.input = Path.GetFullPath(args[position + 1]);
            profile.output = Path.GetFullPath(args[position + 2]);
            var suitePosition = Array.IndexOf(args, "--wotr-profile-suite");
            if (suitePosition >= 0)
            {
                if (suitePosition + 1 >= args.Length)
                    throw new ArgumentException("--wotr-profile-suite requires a suite name.");
                profile.suite = args[suitePosition + 1];
            }
            InGameProfileKinds.For(profile.suite);
            profile.StartCoroutine(profile.GuardedRun());
            return true;
        }

        private IEnumerator GuardedRun()
        {
            var run = Run();
            while (true)
            {
                object current;
                try
                {
                    if (!run.MoveNext())
                        break;
                    current = run.Current;
                }
                catch (Exception error)
                {
                    Debug.LogError("WOTR_PROFILE_FAILED " + error);
                    Application.Quit(1);
                    yield break;
                }
                yield return current;
            }
            Application.Quit(0);
        }

        private IEnumerator Run()
        {
            var views = ReadViews();
            ActionLogPage history = null;
            if (suite == "legacy")
            {
                var historyPath = Path.Combine(Path.GetDirectoryName(input), "ui47-profile-actions.json");
                history = JsonConvert.DeserializeObject<ActionLogPage>(File.ReadAllText(historyPath));
            }
            var end = new WaitForEndOfFrame();
            yield return null;
            using var inputGuard = new DesktopProfileInput(ui.Content.panel.visualTree);
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                    foreach (var kind in InGameProfileKinds.For(suite))
                    {
                        var height = width == 1366 ? 768 : 1080;
                        var resize = ProfileResolution.Apply(width, height);
                        while (resize.MoveNext())
                            yield return resize.Current;
                        panel.scale = scale;
                        var reduced = scale == 1.25f;
                        ui.Content.parent.EnableInClassList("motion-enabled", !reduced);
                        ui.Content.parent.EnableInClassList("reduced-motion", reduced);
                        var view = InGameProfileKinds.View(views, suite, kind);
                        using (var scene = new DesktopProfileScene(ui.Content, view, kind, history, views))
                        using (var profile = new FrameProfile())
                        {
                            for (var warmup = 0; warmup < FrameProfile.WarmupFrames; warmup++)
                            {
                                if (warmup == 20)
                                    scene.Board.Map.Fit();
                                if (warmup > 20)
                                    inputGuard.Tick(scene, warmup);
                                yield return null;
                            }
                            if (!ProfileResolution.Matches(width, height))
                                throw new InvalidOperationException("Player did not reach the requested render resolution: " +
                                    $"{Screen.width}×{Screen.height} ({ProfileResolution.Mode}) instead of {width}×{height}.");
                            for (var sample = 0; sample < FrameProfile.SampleFrames; sample++)
                            {
                                profile.Frame(sample);
                                var start = Stopwatch.GetTimestamp();
                                var changed = inputGuard.Tick(scene, sample);
                                var handler = Milliseconds(start);
                                yield return end;
                                var renderEnd = Milliseconds(start);
                                scene.VerifyRendered();
                                if (changed)
                                    profile.Action(handler, renderEnd);
                                yield return null;
                            }
                            if (suite == "figure-motion")
                            {
                                var capture = CaptureMotion(scene, kind, width, scale, end);
                                while (capture.MoveNext())
                                    yield return capture.Current;
                            }
                            results.Add(new { Kind = kind, Width = Screen.width, Height = Screen.height, Scale = scale,
                                WindowMode = ProfileResolution.Mode,
                                ReducedMotion = reduced, Metrics = profile.Result(), MotionEvidence = scene.MotionEvidence });
                            if (kind == "tooltip" || suite == "in-game" || suite == "board")
                            {
                                inputGuard.Tick(scene, FrameProfile.SampleFrames);
                                yield return end;
                                scene.VerifyRendered();
                                SaveScreenshot(suite == "legacy" ? "ui24-tooltip" : (suite == "board" ? "ui72-" : "ui63-") + kind, width, scale);
                            }
                            Debug.Log($"WOTR_PROFILE_CASE {kind} {width}x{height} {scale}");
                        }
                    }
            WriteReport();
            Debug.Log("WOTR_PROFILE_COMPLETE");
        }

        private IEnumerator CaptureMotion(DesktopProfileScene scene, string kind, int width, float scale, WaitForEndOfFrame end)
        {
            var textures = new Texture2D[3];
            var phases = new[] { "start", "middle", "finished" };
            try
            {
                scene.BeginMotionCapture();
                for (var i = 0; i < phases.Length; i++)
                {
                    if (i > 0)
                        yield return new WaitForSecondsRealtime(i == 1 ? .24f : 1.1f);
                    yield return end;
                    scene.CaptureMotionFrame(phases[i]);
                    textures[i] = ReadScreenshot();
                }
                // Encoding and file I/O happen after the timeline, outside the frame samples.
                for (var i = 0; i < phases.Length; i++)
                    WriteScreenshot(textures[i], $"ui77-{kind}-{phases[i]}", width, scale);
            }
            finally
            {
                foreach (var texture in textures)
                    if (texture != null)
                        Destroy(texture);
            }
        }

        private void SaveScreenshot(string prefix, int width, float scale)
        {
            var texture = ReadScreenshot();
            try
            {
                WriteScreenshot(texture, prefix, width, scale);
            }
            finally
            {
                Destroy(texture);
            }
        }

        private static Texture2D ReadScreenshot()
        {
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            texture.Apply();
            return texture;
        }

        private void WriteScreenshot(Texture2D texture, string prefix, int width, float scale)
        {
            var path = Path.Combine(Path.GetDirectoryName(output), $"{prefix}-{width}-{scale:0.00}.png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }

        private void WriteReport()
        {
            File.WriteAllText(output, JsonConvert.SerializeObject(new
            {
                RecordedAt = DateTime.UtcNow,
                Suite = suite,
                InputSha256 = inputSha256,
                Unity = Application.unityVersion,
                Development = Debug.isDebugBuild,
                Editor = Application.isEditor,
                Platform = Application.platform.ToString(),
                OS = SystemInfo.operatingSystem,
                CPU = SystemInfo.processorType,
                MemoryMB = SystemInfo.systemMemorySize,
                GPU = SystemInfo.graphicsDeviceName,
                GraphicsAPI = SystemInfo.graphicsDeviceType.ToString(),
                TargetFPS = Application.targetFrameRate,
                VSync = QualitySettings.vSyncCount,
                Workload = "Offline presentation projections; no server commands. Render-end timing excludes display/input hardware latency.",
                Refresh = suite == "in-game" ? "Dense cases alternate the public Corruption value (2/3) and revision; no authoritative outcome is simulated."
                    : suite == "board" ? "Own character inspection, private local setup drafts and Menu over actual Battle/Hunt decisions; authoritative projections remain unchanged."
                    : suite == "figure-motion" ? "Consecutive public HTTP/WebSocket projections replay through the production motion renderer; no fabricated connection or edits to positions/revisions."
                    : "Legacy presentation fixtures.",
                Results = results
            }, Formatting.Indented));
        }

        private Dictionary<string, PlayerView> ReadViews()
        {
            var bytes = File.ReadAllBytes(input);
            using var hash = SHA256.Create();
            inputSha256 = BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            using var stream = new MemoryStream(bytes, false);
            using var reader = new StreamReader(stream);
            return JsonConvert.DeserializeObject<Dictionary<string, PlayerView>>(reader.ReadToEnd());
        }

        private static double Milliseconds(long start) => (Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency;
    }
}
#endif
