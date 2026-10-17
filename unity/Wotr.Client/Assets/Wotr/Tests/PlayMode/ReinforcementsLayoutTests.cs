using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    // UI-130/UI-179: the Reinforcements tab's panel shows every own nation without scrolling, docked at the left
    // rail and clear of the console, for both sides at 1366×768 and 1920×1080 with 100% and 125% scale.
    public sealed class ReinforcementsLayoutTests
    {
        [UnityTest]
        public IEnumerator EveryNationRowFitsFourLayoutsForBothSides()
        {
            foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var fixture = Fixture(side);
                        using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
                        var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        texture.Create();
                        try
                        {
                            client.Panel.targetTexture = texture;
                            client.Panel.scale = scale;
                            client.Apply(fixture.View);
                            client.SetOnline();
                            yield return Settle();
                            CardNetworkTests.Click(client.Board.OwnDice.Q<Button>("muster"));
                            yield return Settle();
                            // UI-181: the Muster die lights the Reserve tab; the player opens it.
                            Assert.IsTrue(client.Board.Reinforcements.Tab.ClassListContains("legal-target"));
                            client.Board.Reinforcements.Open();
                            yield return Settle();
                            var label = $"{side}-{width}-{scale:0.00}";
                            CheckFits(client.Board, side == Side.FreePeoples ? 5 : 3, label);
                            LayoutTests.Save(texture, $"ui130-reinforcements-{label}.png");
                        }
                        finally
                        {
                            client.Panel.targetTexture = client.Viewport;
                            texture.Release();
                            Object.Destroy(texture);
                        }
                    }
        }

        private static BoardFixture Fixture(Side side)
        {
            var fixture = new BoardFixture();
            fixture.View.You = side;
            fixture.View.DecisionOwner = side;
            foreach (var die in fixture.View.Dice)
                die.Owner = side;
            fixture.View.Nations.First(n => n.Id == "rohan").StepsToWar = 1;
            fixture.View.Nations.First(n => n.Id == "southrons").StepsToWar = 1;
            fixture.Refresh();
            foreach (var nation in new[] { "rohan", "elves", "southrons" })
                fixture.View.Availability.Add(new ActionAvailabilityView
                {
                    SubjectId = nation, ActionId = "recruit",
                    Conditions = { new AvailabilityConditionView { Code = "at-war", Met = false,
                        Description = FigureGrid.Name(nation) + " is not at war yet." } }
                });
            return fixture;
        }

        private static void CheckFits(BoardScreen board, int nations, string label)
        {
            var panel = board.Reinforcements.Root;
            Assert.AreEqual(DisplayStyle.Flex, panel.resolvedStyle.display, "The opened Reserve tab · " + label);
            var bounds = panel.worldBound;
            var rows = panel.Query(className: "reinforcement-row").ToList();
            Assert.AreEqual(nations, rows.Count, "A row per own nation · " + label);
            foreach (var row in rows)
            {
                Assert.LessOrEqual(row.worldBound.yMax, bounds.yMax + .5f, $"{row.name} is not cut off · {label}");
                foreach (var token in row.Query<Button>(className: "reserve-token").ToList())
                {
                    Assert.LessOrEqual(token.worldBound.xMax, bounds.xMax + .5f, $"{token.name} fits the row · {label}");
                    // UI-197: the figure stands in a soft glow inside its own slot.
                    var glow = token.Q("reserve-token-glow");
                    Assert.AreEqual(PickingMode.Ignore, glow.pickingMode);
                    Assert.IsTrue(token.worldBound.Contains(glow.worldBound.center), $"{token.name} glow · {label}");
                }
                Assert.IsNotNull(row.Q("reinforcement-badge").Q("reinforcement-glow"), $"{row.name} crest glow · {label}");
            }
            Assert.GreaterOrEqual(bounds.yMin, board.TopBar.Root.worldBound.yMax - .5f, "Below the top bar · " + label);
            Assert.GreaterOrEqual(bounds.xMin, board.Characters.Tab.worldBound.xMax - .5f, "Clear of the left rail · " + label);
            Assert.LessOrEqual(bounds.yMax, board.Console.Root.worldBound.yMin + .5f, "Clear of the turn console · " + label);
            Assert.LessOrEqual(bounds.xMax, board.Root.worldBound.xMax, "Inside the screen · " + label);
            Assert.IsTrue(panel.ClassListContains("edge-left-panel"), "Docked at the left rail · " + label);
            Assert.IsNull(panel.Q(className: "reinforcement-track"), "No political track · " + label);
            Assert.IsNotEmpty(panel.Query(className: "recruitable").ToList(), "The recruiting nations are lit · " + label);
            var header = panel.Q(className: "panel-header").worldBound;
            foreach (var name in new[] { "reinforcements-close", "reinforcement-budget" })
            {
                var part = panel.Q(name).worldBound;
                Assert.GreaterOrEqual(part.xMin, header.xMin - .5f, name + " · " + label);
                Assert.LessOrEqual(part.xMax, header.xMax + .5f, name + " · " + label);
            }
        }

        private static IEnumerator Settle()
        {
            for (var frame = 0; frame < 6; frame++)
                yield return null;
        }
    }
}
