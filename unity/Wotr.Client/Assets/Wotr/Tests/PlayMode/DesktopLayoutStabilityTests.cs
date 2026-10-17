using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    public sealed class DesktopLayoutStabilityTests
    {
        [UnityTest]
        public IEnumerator LegacyBattleToCardAtLargeScaleStabilizesWithoutRecursiveLayout()
        {
            var configuration = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(configuration))
                Assert.Ignore("Run scripts/test-client.sh with the real legacy projection export.");
            var repository = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_REPOSITORY") ??
                Path.GetFullPath(Path.Combine(Application.dataPath, "../../.."));
            var path = Path.Combine(repository, ".artifacts", "ui13-profile-views.json");
            var views = JsonConvert.DeserializeObject<Dictionary<string, PlayerView>>(File.ReadAllText(path));
            using var mount = new MountedMatchClient("http://localhost", "layout-stability");
            mount.Board.Root.RemoveFromHierarchy();
            var target = new RenderTexture(1920, 1080, 24);
            target.Create();
            mount.Panel.targetTexture = target;
            mount.Interface.Preferences.SetScale(1.25f);
            mount.Interface.Preferences.SetReducedMotion(true);
            try
            {
                foreach (var kind in new[] { "battle", "card" })
                {
                    using var scene = new Scene(mount.Interface.Content, views[kind == "card" ? "map" : kind], kind);
                    using var trace = new GeometryTrace(scene.Board, kind);
                    for (var frame = 0; frame < 120; frame++)
                    {
                        if (frame == 20)
                            scene.Board.Map.Fit();
                        if (frame > 20)
                            scene.Tick(frame);
                        yield return null;
                    }
                    for (var frame = 0; frame < 360; frame++)
                    {
                        scene.Tick(frame);
                        yield return null;
                        scene.Verify();
                        yield return null;
                    }
                    LayoutTests.Save(target, "ui63-layout-stability-" + kind + "-1920-1.25.png");
                    LogAssert.NoUnexpectedReceived();
                }
            }
            finally
            {
                mount.Panel.targetTexture = mount.Viewport;
                target.Release();
                Object.Destroy(target);
            }
        }

        // Read-only tracing: no geometry, scheduling or log suppression is changed.
        private sealed class GeometryTrace : IDisposable
        {
            private readonly BoardScreen board;
            private readonly string kind;
            private readonly Queue<string> changes = new Queue<string>();
            private readonly List<VisualElement> observed = new List<VisualElement>();

            public GeometryTrace(BoardScreen board, string kind)
            {
                this.board = board;
                this.kind = kind;
                foreach (var element in new[] { board.Root, board.Map.parent, board.Map, board.Cards.Root,
                    board.Hand, board.Victory.Tab, board.Victory.Root, board.Battles.Root,
                    board.TableEffects.Root, board.Sovereigns.Root, board.Sovereigns.Inspector })
                {
                    if (observed.Contains(element))
                        continue;
                    observed.Add(element);
                    element.RegisterCallback<GeometryChangedEvent>(Changed);
                }
                Application.logMessageReceived += Logged;
            }

            private void Changed(GeometryChangedEvent value)
            {
                var element = (VisualElement)value.target;
                var style = element.style;
                changes.Enqueue($"frame={Time.frameCount} {element.name} {value.oldRect} -> {value.newRect}; " +
                    $"left={style.left}, top={style.top}, right={style.right}, bottom={style.bottom}, " +
                    $"width={style.width}, height={style.height}, world={element.worldBound}");
                while (changes.Count > 96)
                    changes.Dequeue();
            }

            private void Logged(string message, string stack, LogType type)
            {
                if (message.Contains("Layout update failed to stabilize"))
                    Save("error", message);
            }

            private void Save(string suffix, string message)
            {
                var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../.artifacts/screenshots"));
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "ui63-layout-trace-" + kind + "-" + suffix + ".txt"),
                    message + "\nPanel=" + board.Root.panel.visualTree.worldBound + "\n" + string.Join("\n", changes));
            }

            public void Dispose()
            {
                Application.logMessageReceived -= Logged;
                Save("last", "Final observed geometry events; this trace is not a performance sample.");
                foreach (var element in observed)
                    element.UnregisterCallback<GeometryChangedEvent>(Changed);
            }
        }

        private sealed class Scene : IDisposable
        {
            private readonly object scene, input;
            private readonly MethodInfo tick, verify;
            private readonly bool inspectCard;
            private string inspectedCard = "";
            public BoardScreen Board { get; }

            public Scene(VisualElement parent, PlayerView view, string kind)
            {
                inspectCard = kind == "card";
                var assembly = typeof(DesktopProfile).Assembly;
                var sceneType = assembly.GetType("Wotr.Client.Diagnostics.DesktopProfileScene", true);
                var inputType = assembly.GetType("Wotr.Client.Diagnostics.DesktopProfileInput", true);
                scene = Activator.CreateInstance(sceneType, parent, view, kind, null, null);
                input = Activator.CreateInstance(inputType, parent.panel.visualTree);
                Board = (BoardScreen)sceneType.GetProperty("Board").GetValue(scene);
                tick = inputType.GetMethod("Tick");
                verify = sceneType.GetMethod("VerifyRendered");
            }

            public void Tick(int frame)
            {
                Invoke(tick, input, scene, frame);
                // Keep the legacy floating inspector under reflow stress, alongside
                // the current UI-91 hand gesture performed by the profile driver.
                if (inspectCard && frame % 15 == 0 && Board.ExpandedCard.Length > 0)
                {
                    inspectedCard = Board.ExpandedCard;
                    Board.Cards.Show(inspectedCard, true);
                }
            }

            public void Verify()
            {
                Invoke(verify, scene);
                if (inspectCard)
                {
                    Assert.IsNotEmpty(inspectedCard);
                    Assert.AreEqual(inspectedCard, Board.Cards.CardId);
                    Assert.AreEqual(DisplayStyle.Flex, Board.Cards.Root.resolvedStyle.display);
                }
            }

            private static void Invoke(MethodInfo method, object owner, params object[] arguments)
            {
                try
                {
                    method.Invoke(owner, arguments);
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
