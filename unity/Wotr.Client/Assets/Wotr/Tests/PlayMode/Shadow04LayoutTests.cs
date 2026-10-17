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
    public class Shadow04LayoutTests
    {
        [UnityTest]
        public IEnumerator ServerProjectedChoicesAndTwoArmiesFitBothSizesAndScales()
        {
            var config = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            Assert.IsNotEmpty(config, "Run scripts/test-client.sh to generate server projections for layout checks.");
            var file = Path.Combine(Path.GetDirectoryName(config), "shadow04-layout.json");
            var views = JsonConvert.DeserializeObject<Dictionary<string, PlayerView>>(File.ReadAllText(file));
            using (var client = new MountedMatchClient("http://localhost", "shadow04-layout"))
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                        foreach (var entry in views)
                        {
                            var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                            target.Create();
                            client.Panel.targetTexture = target;
                            client.Panel.scale = scale;
                            client.Apply(entry.Value);
                            client.SetOnline();
                            for (var frame = 0; frame < 10; frame++)
                                yield return null;
                            client.Board.Map.Fit();
                            yield return Prepare(client, entry.Key);
                            for (var frame = 0; frame < 5; frame++)
                                yield return null;
                            if (entry.Key == "shadow-ringwraiths")
                            {
                                // UI-216: two planned armies, the event's alternatives in the console.
                                Assert.That(client.Board.Console.Status.Hint.text, Does.StartWith("Armies moved 2"));
                                Assert.IsNotNull(MatchTestRoom.TrayOrConsole(client, "action-AttackArmy"));
                                Assert.IsNotNull(MatchTestRoom.TrayOrConsole(client, "action-FinishArmyEvent"));
                            }
                            foreach (var commander in client.Board.Tray.Query<Button>(className: "required-commander").ToList())
                                Assert.LessOrEqual(commander.MeasureTextSize(commander.text, 0, VisualElement.MeasureMode.Undefined,
                                    0, VisualElement.MeasureMode.Undefined).x, commander.contentRect.width + 1, commander.name);
                            LayoutTests.Save(target, $"shadow04-{entry.Key}-{width}-{scale:0.00}.png");
                            FellowshipLayoutTests.Check(client.Board);
                            LayoutTests.AssertNoDecisionScroll(client.Board);
                            Assert.IsEmpty(client.Board.Tray.Query<TextField>().ToList());
                            client.Panel.targetTexture = client.Viewport;
                            target.Release();
                            Object.Destroy(target);
                        }
        }

        private static IEnumerator Prepare(MountedMatchClient client, string scenario)
        {
            if (scenario == "shadow-grond")
                client.Board.Map.Select("minas-tirith");
            if (scenario == "shadow-ringwraiths")
                foreach (var region in new[] { "osgiliath", "north-ithilien", "south-ithilien", "north-ithilien" })
                    client.Board.Map.Select(region);
            if (scenario == "shadow-palantir-remove")
            {
                var cost = client.Board.View.Options.Single(o => o.Kind == CommandKind.DiscardPalantir).EligibleDice[0];
                CardNetworkTests.Click(client.Board.OwnDice.Q<Button>(cost));
                client.Board.TableEffects.Open();
                for (var frame = 0; frame < 3; frame++)
                    yield return null;
                CardNetworkTests.Click(client.Board.TableEffects.Root.Q<Button>("remove-table-base.sh.character.21"));
            }
            if (scenario == "shadow-wormtongue")
            {
                FellowshipGestures.Die(client);
                var origin = client.Board.View.Characters.Single(c => c.Id == "base.character.gandalf-grey").Location;
                FellowshipGestures.Figure(client, origin, "base.character.gandalf-grey");
            }
        }
    }
}
