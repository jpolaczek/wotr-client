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
    public sealed class BoardProfileDriverTests
    {
        [UnityTest]
        public IEnumerator EveryBoardWorkloadUsesRealGesturesAndPreservesItsDecisionAcrossAllFourLayouts()
        {
            var configuration = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(configuration))
                Assert.Ignore("Run scripts/test-client.sh to export the ten normal side-filtered profile inputs.");
            var path = Path.Combine(Path.GetDirectoryName(configuration), "ui72-profile-views.json");
            var views = JsonConvert.DeserializeObject<Dictionary<string, PlayerView>>(File.ReadAllText(path));
            CollectionAssert.AreEquivalent(new[] { "characters-fp", "characters-sh", "setup-lords-fp", "setup-council-fp",
                "setup-fate-fp", "setup-waiting-sh", "homelands-fp", "homelands-sh", "menu-battle", "menu-hunt" }, views.Keys);
            using var mount = new MountedMatchClient("http://localhost", "board-profile-validation");
            mount.Board.Root.RemoveFromHierarchy();
            var cases = 0;
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
                        foreach (var pair in views)
                        {
                            using var driver = new Driver(mount.Interface.Content, pair.Value, pair.Key);
                            using var geometry = new BoardProfileGeometryTrace(driver.Board, pair.Key, width, scale);
                            var before = JsonConvert.SerializeObject(pair.Value);
                            var inspected = new HashSet<string>();
                            var compared = new HashSet<string>();
                            var states = new HashSet<string>();
                            yield return Frames();
                            driver.Board.Map.Fit();
                            yield return Frames();
                            for (var step = 0; step < Steps(pair.Key); step++)
                            {
                                geometry.Step = step;
                                Check(() => Assert.IsTrue(driver.Tick(step * 15)), driver, target, pair.Key, width, scale, step);
                                yield return Frames();
                                Check(driver.Verify, driver, target, pair.Key, width, scale, step);
                                Observe(driver.Board, inspected, compared, states);
                            }
                            Assert.AreEqual(before, JsonConvert.SerializeObject(driver.Board.View), pair.Key);
                            AssertCoverage(driver.Board, pair.Key, inspected, compared, states);
                            ExpansionMatrixLayoutTests.ResourcesFit(driver.Board);
                            LayoutTests.AssertNoDecisionScroll(driver.Board);
                            cases++;
                        }
                    }
                    finally
                    {
                        mount.Panel.targetTexture = mount.Viewport;
                        target.Release();
                        Object.Destroy(target);
                    }
                }
            Assert.AreEqual(40, cases);
        }

        private static int Steps(string kind) => kind.StartsWith("characters-", StringComparison.Ordinal) ? 34
            : kind.StartsWith("setup-", StringComparison.Ordinal) && kind != "setup-waiting-sh" ? 32
            : kind.EndsWith("-sh", StringComparison.Ordinal) ? 14 : 12;

        private static void Observe(BoardScreen board, HashSet<string> inspected, HashSet<string> compared, HashSet<string> states)
        {
            if (board.Characters.Inspector.Root.resolvedStyle.display != DisplayStyle.None)
                inspected.Add(board.Characters.Inspector.FigureId);
            if (board.Fellowship.Versions.Figure.Length > 0)
                compared.Add(board.Fellowship.Versions.Figure);
            states.Add(board.Menu.IsOpen ? "menu" : "board");
            var location = board.CompanionSetup.Root.Q<Button>("homeland-character-base.character.gimli")?.Q<Label>("homeland-location");
            if (location != null)
                states.Add(location.text);
        }

        private static void AssertCoverage(BoardScreen board, string kind, HashSet<string> inspected,
            HashSet<string> compared, HashSet<string> states)
        {
            if (kind.StartsWith("characters-", StringComparison.Ordinal))
                CollectionAssert.AreEquivalent(CharacterRosterPresentation.Rows(board.View).Where(row =>
                    CharacterRosterPresentation.Section(row) != CharacterRosterSection.Removed).Select(row => row.FigureId), inspected);
            if (kind.StartsWith("setup-", StringComparison.Ordinal) && kind != "setup-waiting-sh")
                CollectionAssert.AreEquivalent(board.View.Options.Single(option => option.Kind == CommandKind.ChooseCompanionVersions)
                    .CompanionVersions.Select(group => group.FigureId), compared);
            if (kind == "homelands-fp")
                foreach (var expected in new[] { "Fellowship", "→ Iron Hills", "→ Ered Luin" })
                    CollectionAssert.Contains(states, expected);
            if (kind.StartsWith("menu-", StringComparison.Ordinal) || kind == "setup-waiting-sh" || kind == "homelands-sh")
                foreach (var expected in new[] { "menu", "board" })
                    CollectionAssert.Contains(states, expected);
        }

        private static IEnumerator Frames()
        {
            for (var frame = 0; frame < 5; frame++)
                yield return null;
        }

        private static void Check(Action action, Driver driver, RenderTexture target, string kind, int width, float scale, int step)
        {
            try { action(); }
            catch (Exception error)
            {
                var context = $"{kind}-{width}-{scale:0.00}-step-{step}";
                LayoutTests.Save(target, "ui72-driver-failure-" + context + ".png");
                throw new InvalidOperationException("Board profile driver failed: " + context + "\n" + driver.DiagnosticState +
                    "\n" + error.Message, error);
            }
        }

        // Reflect only the internal diagnostics adapter; every input still uses its real strict Pick path.
        internal sealed class Driver : IDisposable
        {
            public BoardScreen Board { get; }
            private readonly object scene, input;
            private readonly MethodInfo tick, verify;
            private readonly PropertyInfo diagnostic;
            public string DiagnosticState => (string)diagnostic.GetValue(scene);

            public Driver(VisualElement parent, PlayerView view, string kind,
                Dictionary<string, PlayerView> recordedViews = null)
            {
                var assembly = typeof(DesktopProfile).Assembly;
                var sceneType = assembly.GetType("Wotr.Client.Diagnostics.DesktopProfileScene", true);
                var inputType = assembly.GetType("Wotr.Client.Diagnostics.DesktopProfileInput", true);
                scene = Activator.CreateInstance(sceneType, parent, view, kind, null, recordedViews);
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
                try { return method.Invoke(owner, arguments); }
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
