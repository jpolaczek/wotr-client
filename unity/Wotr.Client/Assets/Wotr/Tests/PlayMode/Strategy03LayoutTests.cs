using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using Wotr.Fixtures;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public class Strategy03LayoutTests
    {
        [UnityTest]
        public IEnumerator CoastalMapAndTwoRecruitsFitBothSizesAndScales()
        {
            using (var client = new MountedMatchClient("http://localhost", "strategy-03-layout"))
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                        foreach (var region in new[] { true, false })
                        {
                            var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                            target.Create();
                            client.Panel.targetTexture = target;
                            client.Panel.scale = scale;
                            var view = Screen(region);
                            client.Apply(view);
                            yield return Settle();
                            if (region)
                                client.Board.Map.Select("grey-havens");
                            else
                                foreach (var id in view.Options[0].EligibleIds.Take(2))
                                    CardNetworkTests.Click(MatchTestRoom.Reinforcement(client, "effect-choice-" + id));
                            yield return Settle();
                            FellowshipLayoutTests.Check(client.Board);
                            LayoutTests.AssertNoDecisionScroll(client.Board);
                            Assert.IsEmpty(client.Board.Tray.Query<TextField>().ToList());
                            // UI-131: the offered figures are Reinforcements tokens, not tray buttons.
                            Assert.AreEqual(0, client.Board.Tray.Query<Button>(className: "effect-companion").ToList().Count);
                            if (!region)
                            {
                                Assert.IsTrue(client.Board.Map.DraftOverlay.PlannedRegions.Contains("grey-havens"));
                                Assert.AreEqual(2, client.Board.Reinforcements.Root.Query<Button>(className: "reserve-token").ToList()
                                    .Count(b => b.resolvedStyle.display != DisplayStyle.None));
                                Assert.AreEqual(2, client.Board.Reinforcements.Root.Query<Button>(className: "planned-reinforcement").ToList()
                                    .Count(b => b.resolvedStyle.display != DisplayStyle.None));
                            }
                            LayoutTests.Save(target, $"strategy03-{(region ? "coast" : "recruits")}-{width}-{scale:0.00}.png");
                            client.Panel.targetTexture = client.Viewport;
                            target.Release();
                            Object.Destroy(target);
                        }
        }

        private static IEnumerator Settle()
        {
            for (var frame = 0; frame < 10; frame++)
                yield return null;
        }

        private static PlayerView Screen(bool region)
        {
            var fixture = new BoardFixture(true);
            var view = fixture.View;
            view.MatchId = "strategy-03-layout-" + region;
            view.Decision = new DecisionView
            {
                Id = "recruit-" + region,
                Kind = DecisionKind.EffectChoice,
                SourceId = "base.fp.strategy.13",
                Presentation = region ? "Region" : "ReserveFigures"
            };
            view.Options.Clear();
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = region ? 1 : 2, Maximum = region ? 1 : 2 };
            var ids = region ? fixture.Data.Board.Regions.Where(r => r.Coastal).Select(r => r.Id)
                : view.Units.Where(u => u.Nation == "elves" && u.Location == "reserve" && u.Kind != UnitKind.Leader)
                    .GroupBy(u => u.Kind).SelectMany(g => g.Take(2).Select(u => u.Id));
            foreach (var id in ids)
            {
                option.EligibleIds.Add(id);
                option.EffectChoices.Add(new EffectChoiceView
                {
                    Id = id,
                    TargetId = region ? id : "grey-havens",
                    Title = region ? "Recruit" : "",
                    SelectionGroup = region ? "" : "Recruits",
                    GroupMinimum = option.Minimum,
                    GroupMaximum = option.Maximum
                });
            }
            view.Options.Add(option);
            return view;
        }
    }
}
