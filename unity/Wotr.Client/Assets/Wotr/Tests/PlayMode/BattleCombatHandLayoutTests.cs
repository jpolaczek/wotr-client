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
    // UI-559 (owner, 2026-11-20: "the cards should not stretch like that"): a Combat card tile in the battle window keeps
    // the size it has among four cards, a quarter of the row, whether one card is offered, one stays behind unavailable,
    // or four; six narrow together and stay inside the row. Frames: ui559-*.
    public sealed class BattleCombatHandLayoutTests
    {
        [UnityTest]
        public IEnumerator ACombatCardKeepsItsSizeHoweverFewTheHandHolds()
        {
            foreach (var width in new[] { 1366, 1920 })
            {
                var single = 0f;
                foreach (var (count, eligible) in new[] { (1, 1), (1, 0), (4, 4), (6, 6) })
                {
                    var label = $"{width} · {count} card(s), {eligible} eligible";
                    var view = BattleScreenFixture.Create(CommandKind.CommitCombatCard);
                    view.Hand = view.Hand.Take(count).ToList();
                    view.Options.Single().EligibleIds = view.Hand.Take(eligible).ToList();
                    using var client = new MountedMatchClient("http://localhost", view.MatchId);
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Apply(view);
                    yield return Frames(4);
                    if (!client.Board.Battles.IsOpen)
                        CardNetworkTests.Click(client.Board.Console.Status.Plaque);
                    yield return Frames(8);
                    var row = client.Board.Battles.Root.Q("battle-combat-hand");
                    var tiles = row.Query<Button>(className: "combat-choice").ToList();
                    Assert.AreEqual(count, tiles.Count, label);
                    var rowWidth = row.contentRect.width;
                    foreach (var tile in tiles)
                    {
                        Assert.LessOrEqual(tile.layout.width, rowWidth / 4 + 1, "No wider than a quarter of the row · " + label);
                        Assert.IsTrue(row.worldBound.xMax + 1 >= tile.worldBound.xMax, "Inside the row · " + label);
                    }
                    if (count == 1 && eligible == 1)
                        single = tiles[0].layout.width;
                    if (count == 4)
                        Assert.AreEqual(single, tiles[0].layout.width, 1.5f, "One card has the size it has among four · " + label);
                    LayoutTests.Save(target, $"ui559-combat-hand-{width}-{count}-{eligible}.png");
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
            }
        }

        private static IEnumerator Frames(int count)
        {
            for (var i = 0; i < count; i++)
                yield return null;
        }
    }
}
