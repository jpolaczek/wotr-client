using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public class CardPack02LayoutTests
    {
        [UnityTest]
        public IEnumerator HeroicWithEveryLeaderAndCompanionFitsWithoutScrolling()
        {
            using (var client = new MountedMatchClient("http://localhost", "card-pack-02-layout"))
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        target.Create();
                        client.Panel.targetTexture = target;
                        client.Panel.scale = scale;
                        client.Apply(Screen());
                        for (var frame = 0; frame < 12; frame++)
                            yield return null;
                        foreach (var tab in client.Board.Tray.Query<Button>().ToList().Where(b => b.name.StartsWith("effect-tab-")))
                        {
                            CardNetworkTests.Click(tab);
                            for (var frame = 0; frame < 4; frame++)
                                yield return null;
                            LayoutTests.Save(target, $"heroic-{tab.name}-{width}-{scale:0.00}.png");
                            CheckVisible(client);
                        }
                        CardNetworkTests.Click(client.Board.Tray.Q<Button>("effect-tab-companions"));
                        for (var frame = 0; frame < 4; frame++)
                            yield return null;
                        CardNetworkTests.Click(client.Board.Tray.Q<Button>("effect-choice-base.character.aragorn"));
                        CardNetworkTests.Click(client.Board.Tray.Q<Button>("effect-choice-hits-3"));
                        for (var frame = 0; frame < 4; frame++)
                            yield return null;
                        CheckVisible(client);
                        LayoutTests.Save(target, $"heroic-death-{width}-{scale:0.00}.png");
                        client.Panel.targetTexture = client.Viewport;
                        target.Release();
                        Object.Destroy(target);
                    }
        }

        private static void CheckVisible(MountedMatchClient client)
        {
            var board = client.Board;
            foreach (var button in board.Tray.Query<Button>().ToList().Where(b => b.worldBound.height > 0))
            {
                Assert.GreaterOrEqual(button.worldBound.xMin, board.Tray.worldBound.xMin, button.name);
                Assert.LessOrEqual(button.worldBound.xMax, board.Tray.worldBound.xMax + 1, button.name);
                Assert.LessOrEqual(button.worldBound.yMax, board.Root.worldBound.yMax, button.name);
                Assert.GreaterOrEqual(button.resolvedStyle.height, 32, button.name);
                var text = button.MeasureTextSize(button.text, button.contentRect.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                Assert.LessOrEqual(text.y, button.contentRect.height + 1, "Clipped: " + button.text);
            }
            LayoutTests.AssertNoDecisionScroll(board);
            Assert.IsEmpty(board.Tray.Query<TextField>().ToList());
            Assert.Greater(board.Map.worldBound.height, board.Root.worldBound.height / 2);
        }

        private static PlayerView Screen()
        {
            var view = CardPackLayoutTests.Screen();
            view.MatchId = "card-pack-02-layout";
            view.Decision.Id = "heroic-death";
            view.Decision.SourceId = "base.fp.character.10";
            view.Battle.Attacker.CardId = "base.fp.character.10";
            var option = view.Options.Single();
            option.Minimum = 1;
            option.Maximum = 2;
            option.EffectChoices.Clear();
            foreach (var nation in new[] { "gondor", "rohan", "elves", "dwarves", "north" })
                for (var index = 0; index < 4; index++)
                {
                    var id = nation + "-leader-" + index;
                    view.Units.Add(new UnitView { Id = id, Nation = nation, Kind = UnitKind.Leader, Location = "minas-tirith", Side = Side.FreePeoples });
                    option.EffectChoices.Add(new EffectChoiceView { Id = id, SelectionGroup = "Sacrifice" });
                }
            foreach (var character in view.Characters)
                option.EffectChoices.Add(new EffectChoiceView { Id = character.Id, SelectionGroup = "Sacrifice" });
            option.EffectChoices.Add(new EffectChoiceView { Id = "decline", SelectionGroup = "Sacrifice", Exclusive = true, Title = "Keep all" });
            for (var hits = 0; hits <= 3; hits++)
                option.EffectChoices.Add(new EffectChoiceView
                {
                    Id = "hits-" + hits,
                    SelectionGroup = "Hits",
                    Title = hits == 0 ? "Keep hits" : "− " + new string('●', hits),
                    RequiresAny = { "base.character.aragorn" }
                });
            option.EligibleIds = option.EffectChoices.Select(c => c.Id).ToList();
            return view;
        }
    }
}
