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
    public class Shadow03LayoutTests
    {
        [UnityTest]
        public IEnumerator ServerProjectedCardDecisionsFitBothSizesAndScales()
        {
            var config = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            Assert.IsNotEmpty(config, "Run scripts/test-client.sh to generate server projections for layout checks.");
            var file = Path.Combine(Path.GetDirectoryName(config), "shadow03-layout.json");
            var views = JsonConvert.DeserializeObject<Dictionary<string, PlayerView>>(File.ReadAllText(file));
            using (var client = new MountedMatchClient("http://localhost", "shadow03-layout"))
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                        foreach (var entry in views)
                        {
                            var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                            target.Create();
                            client.Panel.targetTexture = target;
                            client.Panel.scale = scale;
                            client.Apply(entry.Value);
                            client.Board.Fellowship.Open();
                            for (var frame = 0; frame < 10; frame++)
                                yield return null;
                            if (entry.Value.Decision.Kind == DecisionKind.Reaction)
                            {
                                for (var frame = 0; frame < 3; frame++)
                                    yield return null;
                            }
                            if (entry.Key == "shadow-breaking")
                            {
                                // The companion decision lives in Hunt; a pinned Fellowship
                                // inspection can cover it after the viewport changes.
                                client.Board.Fellowship.Close();
                                for (var frame = 0; frame < 3; frame++)
                                    yield return null;
                                Assert.IsTrue(client.Board.Hunt.IsOpen, "The Hunt window opened for this Hunt stays open.");
                                for (var frame = 0; frame < 3; frame++)
                                    yield return null;
                                Assert.AreSame(client.Board.Hunt.DecisionHost, client.Board.Tray.parent);
                                foreach (var choice in entry.Value.Options.Single(o => o.Kind == CommandKind.ChooseEffect).EligibleIds.Take(3))
                                {
                                    var portrait = client.Board.Tray.Q<Button>("effect-choice-" + choice);
                                    Assert.IsNotNull(portrait);
                                    Assert.IsTrue(portrait.enabledInHierarchy, choice);
                                    PointerGestureEvents.ClickAt(portrait);
                                    for (var frame = 0; frame < 3; frame++)
                                        yield return null;
                                    Assert.IsTrue(client.Board.Tray.Q<Button>("effect-choice-" + choice)
                                        .ClassListContains("selected"), choice);
                                }
                                Assert.IsTrue(client.Board.Map.DraftOverlay.PlannedRegions.Contains("hollin"));
                            }
                            LayoutTests.Save(target, $"shadow03-{entry.Key}-{width}-{scale:0.00}.png");
                            FellowshipLayoutTests.Check(client.Board);
                            LayoutTests.AssertNoDecisionScroll(client.Board);
                            Assert.IsEmpty(client.Board.Tray.Query<TextField>().ToList());
                            client.Panel.targetTexture = client.Viewport;
                            target.Release();
                            Object.Destroy(target);
                        }
        }
    }
}
