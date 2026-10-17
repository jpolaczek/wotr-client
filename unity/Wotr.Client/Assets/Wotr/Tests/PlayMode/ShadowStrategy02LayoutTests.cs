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
using Wotr.Contracts;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public class ShadowStrategy02LayoutTests
    {
        [UnityTest]
        public IEnumerator ArmyRoutesLandingsAndCombatFitBothSizesAndScales()
        {
            var config = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            Assert.IsNotEmpty(config);
            var file = Path.Combine(Path.GetDirectoryName(config), "shadow-strategy02-layout.json");
            var views = JsonConvert.DeserializeObject<Dictionary<string, PlayerView>>(File.ReadAllText(file));
            Assert.AreEqual(14, views.Count);
            using (var client = new MountedMatchClient("http://localhost", "sh-str02-layout"))
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                        foreach (var entry in views)
                        {
                            var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                            target.Create();
                            client.Panel.targetTexture = target;
                            client.Panel.scale = scale;
                            client.Apply(entry.Value);
                            for (var frame = 0; frame < 10; frame++)
                                yield return null;
                            client.Board.Map.Fit();
                            yield return Prepare(client, entry.Key);
                            for (var frame = 0; frame < 5; frame++)
                                yield return null;
                            LayoutTests.Save(target, $"shadow-strategy02-{entry.Key}-{width}-{scale:0.00}.png");
                            Check(client, entry.Key);
                            FellowshipLayoutTests.Check(client.Board);
                            client.Panel.targetTexture = client.Viewport;
                            target.Release();
                            Object.Destroy(target);
                        }
        }

        private static IEnumerator Prepare(MountedMatchClient client, string scenario)
        {
            switch (scenario)
            {
                case "sh-str02-gather":
                    yield return March(client, "moria", "hollin", "north-dunland", "south-dunland");
                    break;
                case "sh-str02-lengthens":
                    yield return March(client, "moria", "north-dunland", "south-dunland");
                    yield return March(client, "south-dunland", "north-dunland", "moria");
                    break;
                case "sh-str02-moving":
                    yield return March(client, "moria", "north-dunland");
                    yield return March(client, "north-dunland", "hollin");
                    yield return March(client, "hollin", "moria");
                    yield return March(client, "south-dunland", "north-dunland");
                    break;
                case "sh-str02-rage-move":
                    foreach (var region in new[] { "north-dunland", "south-dunland" })
                    {
                        client.Board.Map.Select(region);
                        yield return Settle();
                        foreach (var unit in client.Board.View.Units.Where(u => u.Nation == "isengard" && u.Location == region))
                            Click(client, "effect-choice-" + unit.Id);
                    }
                    break;
                case "sh-str02-corsairs-landing":
                    client.Board.Map.Select("umbar");
                    break;
                case "sh-str02-corsairs-relief-excess":
                    var excess = client.Board.View.Options.Single(o => o.Kind == CommandKind.RemoveExcess);
                    foreach (var id in excess.EligibleIds.Take(excess.Minimum))
                        Click(client, "figure-" + id);
                    break;
            }
        }

        private static IEnumerator March(MountedMatchClient client, string from, params string[] route)
        {
            client.Board.Map.Select(from);
            yield return Settle();
            foreach (var region in route)
            {
                MapContourProbe.ClickRegion(client.Board.Map, region);
                yield return Settle();
            }
        }

        private static IEnumerator Settle()
        {
            for (var frame = 0; frame < 4; frame++)
                yield return null;
        }

        // UI-215: a card's figures are in the open region panel.
        private static void Click(MountedMatchClient client, string name) =>
            CardNetworkTests.Click(MatchTestRoom.TrayOrConsole(client, name) ?? MatchTestRoom.PanelFigure(client, name));

        private static void Check(MountedMatchClient client, string scenario)
        {
            var tray = client.Board.Tray;
            Assert.IsEmpty(tray.Query<TextField>().ToList());
            if (scenario is "sh-str02-gather" or "sh-str02-lengthens" or "sh-str02-moving")
            {
                var expected = scenario == "sh-str02-gather" ? 1 : scenario == "sh-str02-lengthens" ? 2 : 4;
                // UI-216: the armies are planned on the map; the console counts them and no window opens.
                Assert.That(client.Board.Console.Status.Hint.text, Does.StartWith("Armies moved " + expected));
                Assert.IsFalse(client.Board.DecisionWindow.IsOpen);
            }
            if (scenario == "sh-str02-rage-move")
            {
                Assert.That(client.Board.Console.Status.Hint.text, Does.StartWith("Chosen 4 of 4"));
                Assert.IsFalse(client.Board.DecisionWindow.IsOpen);
            }
            if (scenario == "sh-str02-corsairs-forced")
            {
                Assert.IsTrue(client.Board.View.Battle.ArrivalBeforeBattle);
                Assert.IsNull(tray.Q<Button>("action-StopBattle"));
                Assert.IsNotNull(tray.Q<Button>("action-HoldBattle"));
            }
            if (scenario == "sh-str02-swarm")
                Assert.IsTrue(client.Board.View.Battle.Defender.CardCancelled);
            if (scenario == "sh-str02-corsairs-relief-excess")
            {
                Assert.AreEqual(DecisionKind.RemoveExcess, client.Board.View.Decision.Kind);
                Assert.AreEqual("Complete", client.Board.View.Battle.Stage);
                Assert.AreEqual(12, client.Board.View.Units.Count(u => u.Location == "dol-amroth"));
                Assert.IsFalse(client.Board.View.Units.Any(u => u.Location == "dol-amroth" && u.Garrison));
                Assert.IsTrue(client.Board.View.Units.Any(u => u.Side == Side.FreePeoples && u.Location == "erech"));
            }
        }
    }
}
