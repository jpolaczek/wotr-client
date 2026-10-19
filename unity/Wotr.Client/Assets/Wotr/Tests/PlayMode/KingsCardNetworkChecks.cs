using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    internal static class KingsCardNetworkChecks
    {
        internal static async Task Reconnect(MatchTestRoom room, MountedMatchClient client)
        {
            var connected = client.Connected;
            client.Connection.Reconnect();
            await room.Until(() => client.Connected > connected);
            await room.Settle();
        }

        // UI-274: a king a card's step chooses is clicked on his glowing portrait in the Characters panel; the click sends.
        internal static async Task ChooseKing(MatchTestRoom room, MountedMatchClient client, string id, string detail = "")
        {
            var revision = client.Board.View.Revision;
            var portrait = await BoardNavigation.Sovereign(client.Board, id);
            Assert.IsTrue(portrait.ClassListContains("legal-target"), id + " glows for the card's choice.");
            StringAssert.StartsWith("Click to choose", portrait.tooltip);
            StringAssert.Contains(detail, portrait.tooltip);
            PointerGestureEvents.ClickAt(portrait);
            await room.At(revision + 1);
        }

        // UI-274: an inspected king's tile stands face up beside his portrait; its click returns it.
        // UI-279: each draw is the console's Draw tiles, even when it is the only legal step; nothing is drawn by itself.
        internal static async Task DrawTiles(MatchTestRoom room, MountedMatchClient client)
        {
            var board = client.Board;
            while (!board.View.Options.Any(o => o.Enabled && o.Kind == CommandKind.ChooseCorruptionTile))
            {
                Button draw = null;
                await room.Until(() => (draw = board.Console.Status.Options.Q<Button>("action-DrawCorruptionTile")) != null &&
                    draw.enabledInHierarchy && draw.worldBound.height > 0);
                await room.Settle();
                draw = board.Console.Status.Options.Q<Button>("action-DrawCorruptionTile");
                Assert.AreEqual("Draw tiles", draw.text);
                var revision = board.View.Revision;
                PointerGestureEvents.ClickAt(draw);
                await room.At(revision + 1);
            }
        }

        internal static async Task<Button> InspectedTile(MatchTestRoom room, MountedMatchClient client, string king, string tile)
        {
            await BoardNavigation.Sovereign(client.Board, king);
            await room.Settle();
            return client.Board.Sovereigns.Root.Q("sovereign-slot-" + king)?.Q<Button>("corruption-choice-" + tile);
        }

        internal static async Task Layout(MatchTestRoom room, MountedMatchClient client, string name, string prefix = "kings02")
        {
            // A choice may still be on its way to the server; measure the tray its reply rebuilds.
            await room.Until(() => client.Board.Tray.enabledSelf);
            await room.Settle();
            var source = client.Board.Tray.Query<Button>(className: "committed-choice").ToList().FirstOrDefault();
            if (source != null)
                CardNetworkTests.Click(source);
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Panel.scale = scale;
                    await room.Settle();
                    LayoutTests.Save(target, $"{prefix}-{name}-{width}-{scale:0.00}.png");
                    FellowshipLayoutTests.Check(client.Board);
                    ExpansionMatrixLayoutTests.ResourcesFit(client.Board);
                    LayoutTests.AssertNoDecisionScroll(client.Board);
                    Assert.IsEmpty(client.Board.Tray.Query<TextField>().ToList());
                    foreach (var button in client.Board.Tray.Query<Button>().ToList().Where(b => b.worldBound.width > 0))
                    {
                        Assert.LessOrEqual(button.worldBound.xMax, client.Board.Tray.worldBound.xMax + 1, button.name);
                        Assert.LessOrEqual(button.worldBound.yMax, client.Board.Tray.worldBound.yMax + 1, button.name);
                    }
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
            client.Panel.scale = 1f;
            await room.Settle();
        }
    }
}
