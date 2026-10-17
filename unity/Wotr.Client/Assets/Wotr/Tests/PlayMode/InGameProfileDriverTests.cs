using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Client.Diagnostics;
using Wotr.Contracts;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public sealed class InGameProfileDriverTests
    {
        [UnityTest]
        public IEnumerator AllRealProfileDriversChangeAndRenderTheirExpectedStateInFourLayouts()
        {
            var configuration = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(configuration))
                Assert.Ignore("Run scripts/test-client.sh to export real side-filtered scenario projections.");
            var path = Path.Combine(Path.GetDirectoryName(configuration), "ui63-profile-views.json");
            var views = JsonConvert.DeserializeObject<Dictionary<string, PlayerView>>(File.ReadAllText(path));
            Assert.AreEqual(8, views.Count);
            using var mount = new MountedMatchClient("http://localhost", "profile-driver-validation");
            mount.Board.Root.RemoveFromHierarchy();
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    mount.Panel.targetTexture = target;
                    mount.Interface.Preferences.SetScale(scale);
                    mount.Interface.Preferences.SetReducedMotion(scale > 1);
                    try
                    {
                        // UI-173: the exported Help workload of the pinned backend has no driver any more.
                        foreach (var pair in views.Where(pair => pair.Key != "help"))
                        {
                            using var driver = new Driver(mount.Interface.Content, pair.Value, pair.Key);
                            yield return Frames();
                            driver.Board.Map.Fit();
                            yield return Frames();
                            for (var step = 0; step < 8; step++)
                            {
                                Check(() => Assert.IsTrue(driver.Tick(step * 15)), target, pair.Key, width, scale, step);
                                yield return Frames();
                                Check(driver.Verify, target, pair.Key, width, scale, step);
                            }
                            ExpansionMatrixLayoutTests.ResourcesFit(driver.Board);
                            LayoutTests.AssertNoDecisionScroll(driver.Board);
                        }
                    }
                    finally
                    {
                        mount.Panel.targetTexture = mount.Viewport;
                        target.Release();
                        Object.Destroy(target);
                    }
                }
        }

        private static IEnumerator Frames()
        {
            for (var frame = 0; frame < 5; frame++)
                yield return null;
        }

        private static void Check(Action action, RenderTexture target, string kind, int width, float scale, int step)
        {
            try
            {
                action();
            }
            catch (Exception error)
            {
                var context = $"{kind}-{width}-{scale:0.00}-step-{step}";
                LayoutTests.Save(target, "ui63-driver-failure-" + context + ".png");
                throw new InvalidOperationException("Profile driver failed: " + context + "\n" + error.Message, error);
            }
        }

        // Instantiate the exact internal production diagnostics, without exposing a new runtime API
        // or replacing their strict pointer/Pick path. Reflection is confined to this test adapter.
        private sealed class Driver : IDisposable
        {
            public BoardScreen Board { get; }
            private readonly object scene, input;
            private readonly MethodInfo tick, verify;
            private readonly PropertyInfo diagnostic;
            public string DiagnosticState => (string)diagnostic.GetValue(scene);

            public Driver(VisualElement parent, PlayerView view, string kind)
            {
                var assembly = typeof(DesktopProfile).Assembly;
                var sceneType = assembly.GetType("Wotr.Client.Diagnostics.DesktopProfileScene", true);
                var inputType = assembly.GetType("Wotr.Client.Diagnostics.DesktopProfileInput", true);
                scene = Activator.CreateInstance(sceneType, parent, view, kind, null, null);
                input = Activator.CreateInstance(inputType, parent.panel.visualTree);
                Board = (BoardScreen)sceneType.GetProperty("Board").GetValue(scene);
                tick = inputType.GetMethod("Tick");
                verify = sceneType.GetMethod("VerifyRendered");
                diagnostic = sceneType.GetProperty("DiagnosticState");
            }

            public bool Tick(int frame) => (bool)Invoke(tick, input, scene, frame);
            public void Verify() => Invoke(verify, scene);

            private static object Invoke(MethodInfo method, object owner, params object[] arguments)
            {
                try
                {
                    return method.Invoke(owner, arguments);
                }
                catch (TargetInvocationException error)
                {
                    ExceptionDispatchInfo.Capture(error.InnerException ?? error).Throw();
                    throw;
                }
            }

            public void Dispose()
            {
                ((IDisposable)input).Dispose();
                ((IDisposable)scene).Dispose();
            }
        }
    }
}
