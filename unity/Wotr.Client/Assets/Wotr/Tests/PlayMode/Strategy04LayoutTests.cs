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
    public class Strategy04LayoutTests
    {
        [UnityTest]
        public IEnumerator TwoNationalGroupsAndMapGhostsFitWithoutScrolling()
        {
            using (var client = new MountedMatchClient("http://localhost", "strategy-04-layout"))
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        target.Create();
                        client.Panel.targetTexture = target;
                        client.Panel.scale = scale;
                        var view = Screen();
                        client.Apply(view);
                        yield return Settle();
                        foreach (var choice in view.Options[0].EffectChoices.GroupBy(c => c.SelectionGroup).Select(g => g.Last()))
                            CardNetworkTests.Click(MatchTestRoom.Reinforcement(client, "effect-choice-" + choice.Id));
                        yield return Settle();
                        FellowshipLayoutTests.Check(client.Board);
                        LayoutTests.AssertNoDecisionScroll(client.Board);
                        Assert.IsEmpty(client.Board.Tray.Query<TextField>().ToList());
                        Assert.IsTrue(client.Board.Map.DraftOverlay.PlannedRegions.Contains("the-shire"));
                        Assert.IsTrue(client.Board.Map.DraftOverlay.PlannedRegions.Contains("ered-luin"));
                        // UI-131: each planned region shows a ghost with × on the map.
                        CollectionAssert.AreEquivalent(new[] { "the-shire", "ered-luin" }, client.Board.Map.PlannedRecruits.Regions);
                        Assert.IsNotNull(client.Board.Map.Q<Button>("planned-recruit-remove-the-shire"));
                        LayoutTests.Save(target, $"strategy04-swords-{width}-{scale:0.00}.png");
                        client.Board.Map.Focus("the-shire");
                        yield return Settle();
                        var ghost = client.Board.Map.Q<Button>("planned-recruit-the-shire");
                        Assert.AreEqual(DisplayStyle.Flex, ghost.resolvedStyle.display, "The planned figure shows where it will arrive.");
                        Assert.IsTrue(client.Board.Map.contentRect.Overlaps(ghost.layout), "The ghost lies on the visible map.");
                        LayoutTests.Save(target, $"strategy04-ghosts-{width}-{scale:0.00}.png");
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

        private static PlayerView Screen()
        {
            var fixture = new BoardFixture(true);
            var view = fixture.View;
            view.MatchId = "strategy-04-layout";
            view.Decision = new DecisionView
            {
                Id = "swords",
                Kind = DecisionKind.EffectChoice,
                SourceId = "base.fp.strategy.20",
                Presentation = "ReserveFigures"
            };
            view.Options.Clear();
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = 2, Maximum = 2 };
            foreach (var nation in new[] { "north", "dwarves" })
                foreach (var kind in new[] { UnitKind.Regular, UnitKind.Elite })
                {
                    var unit = view.Units.First(u => u.Nation == nation && u.Kind == kind && u.Location == "reserve");
                    var target = nation == "north" ? "the-shire" : "ered-luin";
                    option.EligibleIds.Add(unit.Id);
                    option.EffectChoices.Add(new EffectChoiceView
                    {
                        Id = unit.Id,
                        TargetId = target,
                        SelectionGroup = fixture.Data.Regions[target].Name,
                        GroupMinimum = 1,
                        GroupMaximum = 1
                    });
                }
            view.Options.Add(option);
            return view;
        }
    }
}
