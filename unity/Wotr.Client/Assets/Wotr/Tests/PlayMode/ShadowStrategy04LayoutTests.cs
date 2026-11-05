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
    public class ShadowStrategy04LayoutTests
    {
        [UnityTest]
        public IEnumerator FullRecruitmentPlansAndSubsequentDecisionsFitBothSizesAndScales()
        {
            var config = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            Assert.IsNotEmpty(config);
            var views = JsonConvert.DeserializeObject<Dictionary<string, PlayerView>>(File.ReadAllText(
                Path.Combine(Path.GetDirectoryName(config), "shadow-strategy04-layout.json")));
            Assert.AreEqual(11, views.Count);
            using (var client = new MountedMatchClient("http://localhost", "sh-str04-layout"))
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
                            Prepare(client, entry.Key);
                            for (var frame = 0; frame < 5; frame++)
                                yield return null;
                            LayoutTests.Save(target, $"shadow-strategy04-{entry.Key}-{width}-{scale:0.00}.png");
                            Check(client);
                            FellowshipLayoutTests.Check(client.Board);
                            client.Panel.targetTexture = client.Viewport;
                            target.Release();
                            Object.Destroy(target);
                        }
        }

        private static void Prepare(MountedMatchClient client, string scenario)
        {
            if (scenario is "sh-str04-misty" or "sh-str04-misty-siege")
            {
                Fill(client, "sauron", "moria", UnitKind.Regular, 1);
                Fill(client, "sauron", "moria", UnitKind.Elite, 1);
                Fill(client, "sauron", "moria", UnitKind.Nazgul, 1);
            }
            if (scenario == "sh-str04-orcs-shortage")
            {
                Fill(client, "sauron", "dol-guldur", UnitKind.Regular, 1);
                Fill(client, "sauron", "mount-gundabad", UnitKind.Regular, 1);
            }
            if (scenario == "sh-str04-horde")
                Fill(client, "southrons", "south-rhun", UnitKind.Regular, 5);
            if (scenario == "sh-str04-monsters")
            {
                Fill(client, "sauron", "weather-hills", UnitKind.Regular, 1);
                Fill(client, "sauron", "trollshaws", UnitKind.Elite, 1);
            }
            if (scenario is "sh-str04-pits" or "sh-str04-pits-shortage")
            {
                Fill(client, "sauron", "mount-gundabad", UnitKind.Regular, 2);
                Fill(client, "sauron", "minas-morgul", UnitKind.Regular, 2);
                Fill(client, "sauron", "dol-guldur", UnitKind.Regular, scenario.EndsWith("shortage", StringComparison.Ordinal) ? 1 : 2);
            }
        }

        private static void Fill(MountedMatchClient client, string nation, string region, UnitKind kind, int count)
        {
            client.Board.Map.Select(region);
            for (var index = 0; index < count; index++)
                CardNetworkTests.Click(client.Board.Reinforcements.Root.Q<Button>("event-reserve-" + nation + "-" + kind));
        }

        private static void Check(MountedMatchClient client)
        {
            var tray = client.Board.Tray;
            Assert.IsEmpty(tray.Query<TextField>().ToList());
            Assert.IsEmpty(tray.Query<ScrollView>().ToList());
            var option = client.Board.View.Options.FirstOrDefault(o => o.EventRecruitment != null);
            if (option == null)
                return;
            // UI-131: the planned figures sit in the Reinforcements panel, each visible, inside it and apart.
            var panel = client.Board.Reinforcements.Root;
            Assert.IsEmpty(panel.Query<ScrollView>().ToList());
            var placed = panel.Query<Button>().ToList().Where(b => b.name.StartsWith("event-placement-", StringComparison.Ordinal) &&
                b.resolvedStyle.display != DisplayStyle.None).ToList();
            Assert.AreEqual(option.Maximum, placed.Count, "Every planned reinforcement remains visible.");
            for (var first = 0; first < placed.Count; first++)
            {
                Assert.IsTrue(panel.worldBound.Contains(placed[first].worldBound.min) && panel.worldBound.Contains(placed[first].worldBound.max),
                    placed[first].name + " lies inside Reinforcements");
                for (var second = first + 1; second < placed.Count; second++)
                    Assert.IsFalse(placed[first].worldBound.Overlaps(placed[second].worldBound));
            }
            Assert.AreEqual(option.Maximum, panel.Q("reinforcement-budget").Children().Count(slot => slot.ClassListContains("filled")));
            // UI-381: the console's ✓ confirms the plan; the panel has none of its own.
            Assert.IsNull(panel.Q<Button>("reinforcements-confirm"), "A card plan is confirmed with the console's ✓.");
            Assert.AreEqual(client.Board.Tray.Q<Button>("confirm-decision")?.enabledSelf ?? false, client.Board.Console.Actions.Confirm.enabledSelf,
                "The console's ✓ follows the tray's confirmation, which needs the server's preview.");
        }
    }
}
