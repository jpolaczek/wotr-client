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
    public class ShadowStrategy01LayoutTests
    {
        [UnityTest]
        public IEnumerator ServerProjectedMapChoicesSacrificesAndSiegesFitBothSizesAndScales()
        {
            var config = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            Assert.IsNotEmpty(config, "Run scripts/test-client.sh to generate server projections for layout checks.");
            var file = Path.Combine(Path.GetDirectoryName(config), "shadow-strategy01-layout.json");
            var views = JsonConvert.DeserializeObject<Dictionary<string, PlayerView>>(File.ReadAllText(file));
            using (var client = new MountedMatchClient("http://localhost", "sh-str01-layout"))
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
                            if ((client.Board.View.Battle != null || client.Board.View.EventLoss != null) && !client.Board.Battles.IsOpen)
                            {
                                CardNetworkTests.Click(client.Board.Console.Status.Plaque);
                                for (var frame = 0; frame < 3; frame++)
                                    yield return null;
                            }
                            Prepare(client, entry.Key);
                            for (var frame = 0; frame < 5; frame++)
                                yield return null;
                            LayoutTests.Save(target, $"shadow-strategy01-{entry.Key}-{width}-{scale:0.00}.png");
                            CheckScenario(client, entry.Key);
                            FellowshipLayoutTests.Check(client.Board);
                            client.Panel.targetTexture = client.Viewport;
                            target.Release();
                            Object.Destroy(target);
                        }
        }

        private static void Prepare(MountedMatchClient client, string scenario)
        {
            if (scenario == "sh-str01-uruk-hai")
                client.Board.Map.Select("minas-tirith");
            if (scenario.StartsWith("sh-str01-stormcrow", StringComparison.Ordinal))
                client.Board.Map.Select("helms-deep");
            if (scenario == "sh-str01-denethor-remove")
                CardNetworkTests.Click(client.Board.OwnDice.Q<Button>(client.Board.View.Options.Single(o => o.Kind == CommandKind.DiscardDenethor).EligibleDice[0]));
        }

        private static void CheckScenario(MountedMatchClient client, string scenario)
        {
            var tray = client.Board.Tray;
            if (scenario.StartsWith("sh-str01-stormcrow", StringComparison.Ordinal))
            {
                // UI-215: the region's panel lights the figures the card offers; no window opens.
                var option = client.Board.View.Options.Single(o => o.Kind == CommandKind.ChooseEffect);
                var panel = client.Board.Hover.Panel;
                Assert.AreEqual("helms-deep", panel.Region);
                var visible = panel.Root.Query<Button>(className: "eligible-figure").ToList().Where(Visible).ToList();
                var expected = option.EffectChoices.Where(c => c.TargetId == "helms-deep").Select(c => "hover-figure-" + c.Id);
                CollectionAssert.AreEquivalent(expected, visible.Select(b => b.name));
                Assert.AreEqual(scenario.EndsWith("dense", StringComparison.Ordinal) ? 14 : 3, visible.Count);
                for (var first = 0; first < visible.Count; first++)
                    for (var second = first + 1; second < visible.Count; second++)
                        Assert.IsFalse(visible[first].worldBound.Overlaps(visible[second].worldBound),
                            visible[first].name + " overlaps " + visible[second].name);
                Assert.IsNull(tray.Q<Button>("confirm-decision"), "A single figure is a direct choice.");
                Assert.IsFalse(visible.Any(b => b.ClassListContains("selected")), "Focusing a region must not select a figure.");
                Assert.IsFalse(client.Board.DecisionWindow.IsOpen);
            }
            if (scenario == "sh-str01-denethor-battle")
                Assert.That(tray.Q<Button>("battle-card-defender").text, Does.Contain("Denethor · No card"));
            if (scenario == "sh-str01-uruk-battle")
                Assert.That(tray.Q<Button>("battle-card-defender").text, Does.Contain("No Companion · No card this round"));
            if (scenario is "sh-str01-relentless" or "sh-str01-onslaught")
            {
                Assert.IsTrue(client.Board.View.Battle.VoluntaryLosses);
                Assert.IsNotNull(tray.Q<Button>("battle-keep-all"));
                var elite = client.Board.View.Units.First(u => u.Kind == UnitKind.Elite &&
                    client.Board.View.Options.Single(o => o.Kind == CommandKind.TakeBattleLosses).EligibleIds.Contains(u.Id));
                StringAssert.Contains("downgrade (1 hit)", tray.Q<Button>("loss-" + elite.Id).tooltip);
            }
        }

        private static bool Visible(VisualElement element)
        {
            for (var ancestor = element; ancestor != null; ancestor = ancestor.parent)
                if (ancestor.resolvedStyle.display == DisplayStyle.None || ancestor.resolvedStyle.visibility == Visibility.Hidden)
                    return false;
            return true;
        }
    }
}
