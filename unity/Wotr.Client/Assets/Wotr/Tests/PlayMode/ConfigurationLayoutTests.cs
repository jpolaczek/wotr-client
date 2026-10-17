using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public class ConfigurationLayoutTests
    {
        [UnityTest]
        public IEnumerator EveryLegalConfigurationFitsInTheLobbyAndReadOnlyGameSummary()
        {
            var config = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            var file = Path.Combine(Path.GetDirectoryName(config), "ui09-matrix.json");
            var views = JsonConvert.DeserializeObject<Dictionary<string, PlayerView>>(File.ReadAllText(file))
                .Values.Where(v => v.You == Side.FreePeoples).ToArray();
            Assert.AreEqual(93, views.Length);
            using (var client = new MountedMatchClient("http://localhost", "configuration-layout"))
            {
                var root = client.Board.Root.parent;
                var room = new RoomScreen();
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        target.Create();
                        client.Panel.targetTexture = target;
                        client.Panel.scale = scale;
                        foreach (var view in views)
                        {
                            client.Board.Root.RemoveFromHierarchy();
                            root.Add(room.Root);
                            room.Update(new RoomView { ExpansionsAvailable = true, Id = view.MatchId, Version = 2, Configuration = view.Configuration,
                                FreePeoples = "Free Peoples player", Shadow = "Shadow player" });
                            for (var frame = 0; frame < 4; frame++)
                                yield return null;
                            Fits(room.Root, root);
                            room.Root.RemoveFromHierarchy();
                            root.Add(client.Board.Root);
                            client.Apply(view);
                            client.Board.Information.Rules();
                            for (var frame = 0; frame < 4; frame++)
                                yield return null;
                            Fits(client.Board.Information.Root, client.Board.Information.Root);
                        }
                        LayoutTests.Save(target, $"t34-configuration-{width}-{scale:0.00}.png");
                        client.Panel.targetTexture = client.Viewport;
                        target.Release();
                        Object.Destroy(target);
                    }
            }
        }

        private static void Fits(VisualElement content, VisualElement bounds)
        {
            Assert.IsEmpty(content.Query<ScrollView>().ToList());
            Assert.IsEmpty(content.Query<TextField>().ToList());
            foreach (var element in content.Query<VisualElement>().ToList()
                .Where(e => e is Button or Toggle or Label && e.worldBound.width > 0))
            {
                Assert.GreaterOrEqual(element.worldBound.xMin, bounds.worldBound.xMin, element.name);
                Assert.LessOrEqual(element.worldBound.xMax, bounds.worldBound.xMax + 1, element.name);
                Assert.LessOrEqual(element.worldBound.yMax, bounds.worldBound.yMax + 1, element.name);
            }
        }
    }
}
