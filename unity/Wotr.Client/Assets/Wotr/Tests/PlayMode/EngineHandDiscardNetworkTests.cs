using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public sealed class EngineHandDiscardNetworkTests
    {
        [UnityTest]
        public IEnumerator MiniatureClicksAfterAnotherControlDiscardBothHandsAtFourLayouts() => Wait(FourLayouts(false));

        [UnityTest]
        public IEnumerator PreviewContentClicksDiscardBothHandsAtFourLayouts() => Wait(FourLayouts(true));

        private static async Task FourLayouts(bool preview)
        {
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 100, 125 })
                {
                    using var fpBoundary = new UI10HttpBoundary();
                    using var shBoundary = new UI10HttpBoundary();
                    var source = preview ? "preview" : "miniature";
                    using var room = await Connect($"ui82-discard-{source}-{width}-{scale}", ScenarioKind.BaseAcceptance,
                        fpHandler: fpBoundary, shHandler: shBoundary);
                    var matchPath = "/api/matches/" + room.Fp.Board.View.MatchId;
                    fpBoundary.MatchPath = shBoundary.MatchPath = matchPath;
                    await DiscardHand(room, room.Fp, room.Sh, fpBoundary, width, scale, preview);
                    await DiscardHand(room, room.Sh, room.Fp, shBoundary, width, scale, preview);
                    Assert.AreEqual(6, room.Fp.Board.View.Hand.Count);
                    Assert.AreEqual(6, room.Sh.Board.View.Hand.Count);
                    Assert.AreEqual(GamePhase.Fellowship, room.Fp.Board.View.Phase);
                }
        }

        private static async Task DiscardHand(MatchTestRoom room, MountedMatchClient client,
            MountedMatchClient opponent, UI10HttpBoundary boundary, int width, int scale, bool preview)
        {
            var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
            target.Create();
            try
            {
                client.Panel.targetTexture = target;
                client.Interface.Preferences.SetScale(scale / 100f);
                await Task.Delay(180, room.Token);
                Assert.AreEqual(8, client.Board.View.Hand.Count);
                Assert.AreEqual(DecisionKind.DiscardCards, client.Board.View.Decision.Kind);
                Assert.IsTrue(client.Board.HandDrawer.IsOpen);
                var ids = client.Board.View.Hand.ToArray();
                using var mouse = new EngineMouseInput(client.Board.Root);
                // The preceding click is part of the same device click series.
                // Neither the test nor the driver assigns a click count.
                // UI-91: the hand is permanent, so another visible control starts the series.
                mouse.Click(client.Board.Phase);
                for (var index = 0; index < 2; index++)
                {
                    // Choose a distinct position after the first removal. A deliberate
                    // move to another card must start a fresh object gesture immediately.
                    var id = ids[index * 2];
                    var source = client.Board.Hand.Q<Button>(id);
                    // UI-91: an unrolled card keeps the pointer over the cards it covers; leave it first, as a player would.
                    mouse.Move(client.Board.Map.worldBound.center);
                    await room.Settle();
                    // UI-142: an excess-hand discard is one click, on the miniature or on the rules of the unrolled card.
                    var clicked = (VisualElement)source;
                    var revision = client.Board.View.Revision;
                    var bounds = source.worldBound;
                    if (preview)
                    {
                        mouse.Move(source.worldBound.center);
                        await room.Settle();
                        clicked = source.Q<Label>("hand-card-event");
                        Assert.IsTrue(clicked.worldBound.height > 0 && client.Board.ExpandedCard == id, "The unrolled card shows its rules.");
                    }
                    Assert.AreEqual(index, boundary.Commands, "Hovering cannot discard.");
                    Assert.AreEqual(bounds, source.worldBound, "Unrolling must preserve the miniature's hit target.");
                    if (index == 0 && client == room.Fp)
                        LayoutTests.Save(target, $"ui82-discard-{(preview ? "preview" : "miniature")}-{width}-{scale}.png");
                    var completionPoint = clicked.worldBound.center + Vector2.right * (index + 1);
                    mouse.ClickAtPoint(completionPoint);
                    await room.Until(() => boundary.Commands == index + 1);
                    mouse.ClickAtPoint(completionPoint);
                    await Task.Delay(250, room.Token);
                    Assert.AreEqual(index + 1, boundary.Commands,
                        $"A click on {clicked.name} discards once; the fast press that follows cannot discard another card. Unity device counts: {string.Join(",", mouse.PressCounts)}.");
                    await room.At(revision + 1);
                    Assert.AreEqual(7 - index, client.Board.View.Hand.Count);
                    await AssertReceipt(room, client, boundary, id, revision + 1);
                    Assert.IsNull(client.Board.Tray.Q<Button>("confirm-decision"));
                    var publicView = JsonConvert.SerializeObject(opponent.Board.View);
                    foreach (var privateId in ids)
                        StringAssert.DoesNotContain(privateId, publicView);
                    if (index == 0)
                    {
                        Assert.IsTrue(client.Board.HandDrawer.IsOpen);
                        Assert.AreEqual(DecisionKind.DiscardCards, client.Board.View.Decision.Kind);
                    }
                }
                Assert.Greater(mouse.GeneratedClickCounts.Count, 0,
                    "The driver must reach Unity's real ClickDetector, not invoke application callbacks.");
            }
            finally
            {
                client.Panel.targetTexture = client.Viewport;
                client.Interface.Preferences.SetScale(1f);
                target.Release();
                Object.Destroy(target);
            }
        }

        private static async Task AssertReceipt(MatchTestRoom room, MountedMatchClient client,
            UI10HttpBoundary boundary, string cardId, long revision)
        {
            Assert.AreEqual(CommandKind.DiscardCards, boundary.LastCommand.Kind);
            CollectionAssert.AreEqual(new[] { cardId }, boundary.LastCommand.Payload.Ids);
            var receipt = await client.Api.Get<CommandReceipt>(
                $"api/matches/{client.Board.View.MatchId}/receipts/{boundary.LastCommand.CommandId}", room.Token);
            Assert.IsTrue(receipt.Accepted);
            Assert.AreEqual(revision, receipt.Revision);
        }
    }
}
