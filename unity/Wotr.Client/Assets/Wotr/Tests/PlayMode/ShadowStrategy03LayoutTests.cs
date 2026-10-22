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
    public class ShadowStrategy03LayoutTests
    {
        [UnityTest]
        public IEnumerator RecruitmentUpgradeAndLossChoicesFitBothSizesAndScales()
        {
            var config = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            Assert.IsNotEmpty(config);
            var file = Path.Combine(Path.GetDirectoryName(config), "shadow-strategy03-layout.json");
            var views = JsonConvert.DeserializeObject<Dictionary<string, PlayerView>>(File.ReadAllText(file));
            Assert.AreEqual(9, views.Count);
            using (var client = new MountedMatchClient("http://localhost", "sh-str03-layout"))
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
                            LayoutTests.Save(target, $"shadow-strategy03-{entry.Key}-{width}-{scale:0.00}.png");
                            Check(client, entry.Key);
                            FellowshipLayoutTests.Check(client.Board);
                            client.Panel.targetTexture = client.Viewport;
                            target.Release();
                            Object.Destroy(target);
                        }
        }

        private static void Prepare(MountedMatchClient client, string scenario)
        {
            if (scenario == "sh-str03-hill-trolls")
                client.Board.Map.Select("moria");
            if (scenario is "sh-str03-new-power" or "sh-str03-new-power-shortage")
                Fill(client, "isengard", "orthanc", UnitKind.Elite, 2);
            if (scenario == "sh-str03-new-power")
            {
                Fill(client, "isengard", "north-dunland", UnitKind.Regular, 2);
                Fill(client, "isengard", "south-dunland", UnitKind.Regular, 2);
            }
            if (scenario == "sh-str03-new-power-shortage")
                Fill(client, "isengard", "south-dunland", UnitKind.Regular, 2);
            if (scenario == "sh-str03-many-kings")
                foreach (var region in new[] { "far-harad", "umbar", "north-rhun" })
                    Fill(client, "southrons", region, UnitKind.Regular, 2);
        }

        private static void Fill(MountedMatchClient client, string nation, string region, UnitKind kind, int count)
        {
            client.Board.Map.Select(region);
            for (var i = 0; i < count; i++)
                CardNetworkTests.Click(client.Board.Reinforcements.Root.Q<Button>("event-reserve-" + nation + "-" + kind));
        }

        private static void Check(MountedMatchClient client, string scenario)
        {
            var tray = client.Board.Tray;
            Assert.IsEmpty(tray.Query<TextField>().ToList());
            if (scenario is "sh-str03-new-power" or "sh-str03-new-power-shortage" or "sh-str03-many-kings")
            {
                var count = scenario.EndsWith("shortage", StringComparison.Ordinal) ? 4 : 6;
                // UI-131: the planned figures are chips of the Reinforcements panel.
                var placements = client.Board.Reinforcements.Root.Query<Button>().ToList().Where(b =>
                    b.name.StartsWith("event-placement-", StringComparison.Ordinal) && b.resolvedStyle.display != DisplayStyle.None).ToList();
                Assert.AreEqual(count, placements.Count);
                for (var first = 0; first < placements.Count; first++)
                    for (var second = first + 1; second < placements.Count; second++)
                        Assert.IsFalse(placements[first].worldBound.Overlaps(placements[second].worldBound));
            }
            if (scenario == "sh-str03-new-power-excess")
            {
                Assert.AreEqual(DecisionKind.RemoveExcess, client.Board.View.Decision.Kind);
                Assert.AreEqual(6, client.Board.View.EffectTrail.Count(step => step.TargetId.Length > 0));
                Assert.AreEqual(4, client.Board.Console.Status.Trail.Query<Button>(className: "committed-choice").ToList().Count);
            }
            if (scenario == "sh-str03-hill-trolls")
            {
                // UI-215: the figures are chosen in the region panel, whose regions glow on the map.
                Assert.IsFalse(client.Board.DecisionWindow.IsOpen);
                Assert.IsNotEmpty(client.Board.Map.Query<Button>(className: "eligible-army").ToList());
            }
        }
    }
}
