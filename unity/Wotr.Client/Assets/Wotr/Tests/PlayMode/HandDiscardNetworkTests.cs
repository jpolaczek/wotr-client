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

namespace Wotr.PlayTests
{
    public class HandDiscardNetworkTests
    {
        [UnityTest]
        public IEnumerator OneClickDiscardsOneCardAndReconnectKeepsTheRemainingDecision() => Wait(Discard());

        private static async Task Discard()
        {
            using var fpBoundary = new UI10HttpBoundary();
            using var shBoundary = new UI10HttpBoundary();
            using var room = await Connect("ui31-discard", ScenarioKind.BaseAcceptance,
                fpHandler: fpBoundary, shHandler: shBoundary);
            var fp = room.Fp;
            var sh = room.Sh;
            fpBoundary.MatchPath = shBoundary.MatchPath = "/api/matches/" + fp.Board.View.MatchId;
            var revision = fp.Board.View.Revision;
            var privateIds = fp.Board.View.Hand.ToArray();
            Assert.AreEqual(8, privateIds.Length);
            Assert.IsTrue(fp.Board.HandDrawer.IsOpen);
            Assert.IsNull(fp.Board.Tray.Q<Button>("confirm-decision"));
            var card = fp.Board.Hand.Q<Button>(privateIds[0]);
            await SaveOverlappingPreview(room, fp, card);
            await ReadPassiveHint(room, fp, card, fpBoundary);
            // UI-142: an excess-hand discard is one click; the fast clicks that follow cannot consume another card.
            AssertReachable(fp, card, card.worldBound);
            PointerGestureEvents.ClickAt(card);
            ArmyGestures.Click(card, 2);
            ArmyGestures.Click(card, 3);
            await room.At(revision + 1);
            Assert.AreEqual(7, fp.Board.View.Hand.Count);
            await Receipt(room, fp, fpBoundary, 1, privateIds[0], revision + 1);
            Assert.AreEqual(DecisionKind.DiscardCards, fp.Board.View.Decision.Kind);
            Assert.AreEqual(1, fp.Board.View.Options.Single().Maximum);
            Assert.IsTrue(fp.Board.HandDrawer.IsOpen);
            Assert.IsNull(fp.Board.Tray.Q<Button>("confirm-decision"));
            foreach (var id in privateIds)
                StringAssert.DoesNotContain(id, JsonConvert.SerializeObject(sh.Board.View));
            var connection = fp.Connected;
            fp.Connection.Reconnect();
            await room.Until(() => fp.Connected > connection);
            await room.Settle();
            Assert.AreEqual(7, fp.Board.View.Hand.Count);
            Assert.AreEqual(1, fpBoundary.Commands);
            var remaining = fp.Board.Hand.Q<Button>(privateIds[1]);
            PointerGestureEvents.ClickAt(remaining);
            await room.At(revision + 2);
            Assert.AreEqual(6, fp.Board.View.Hand.Count);
            await Receipt(room, fp, fpBoundary, 2, privateIds[1], revision + 2);
            Assert.IsNull(fp.Board.View.Decision);
            Assert.AreEqual(DecisionKind.DiscardCards, sh.Board.View.Decision.Kind);
            Assert.IsTrue(sh.Board.HandDrawer.IsOpen);
            await SaveOverlappingPreview(room, sh, sh.Board.Hand.Q<Button>(sh.Board.View.Hand[0]));
            for (var index = 0; index < 2; index++)
            {
                var id = sh.Board.View.Hand[0];
                var chosen = sh.Board.Hand.Q<Button>(id);
                await ReadPassiveHint(room, sh, chosen, shBoundary);
                AssertReachable(sh, chosen, chosen.worldBound);
                PointerGestureEvents.ClickAt(chosen);
                await room.At(revision + 3 + index);
                await Receipt(room, sh, shBoundary, index + 1, id, revision + 3 + index);
            }
            Assert.AreEqual(6, sh.Board.View.Hand.Count);
            Assert.AreEqual(GamePhase.Fellowship, fp.Board.View.Phase);
        }

        private static async Task ReadPassiveHint(MatchTestRoom room, MountedMatchClient client,
            Button source, UI10HttpBoundary boundary)
        {
            var commands = boundary.Commands;
            var revision = client.Board.View.Revision;
            var cardId = client.Board.Cards.CardId;
            var sourceBounds = source.worldBound;
            await ComponentGestures.Hover(source);
            // UI-91: hovering unrolls the card itself; no separate tooltip covers it.
            Assert.AreEqual(source.name, client.Board.ExpandedCard);
            var tooltip = client.Interface.Content.parent.Q<Label>("runtime-tooltip");
            Assert.AreNotEqual(DisplayStyle.Flex, tooltip.resolvedStyle.display);
            AssertReachable(client, source, sourceBounds);
            Assert.AreEqual(cardId, client.Board.Cards.CardId, "Hover must not replace the saved preview.");
            Assert.AreEqual(commands, boundary.Commands);
            Assert.AreEqual(revision, client.Board.View.Revision);
        }

        // A preview opened elsewhere (e.g. from Effects) may still be saved over the hand.
        private static async Task SaveOverlappingPreview(MatchTestRoom room, MountedMatchClient client, Button source)
        {
            client.Board.Cards.Show(source.name, true);
            await room.Settle();
            var inspector = client.Board.Cards.Root;
            var header = inspector.Q(className: FloatingWindow.HandleClass).Q<Label>();
            var start = header.worldBound.center;
            var destination = source.worldBound.center - new Vector2(20, 80);
            var end = start + destination - inspector.worldBound.min;
            Assert.IsTrue(header == header.panel.Pick(start) || header.Contains(header.panel.Pick(start)));
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown,
                button = 0, mousePosition = start, clickCount = 1 }))
                header.SendEvent(down);
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag,
                button = 0, mousePosition = end }))
                inspector.SendEvent(move);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp,
                button = 0, mousePosition = end, clickCount = 1 }))
                inspector.SendEvent(up);
            await room.Settle();
            Assert.IsTrue(client.Interface.Windows.TryGet(inspector.name, out _), "The preview position must be genuinely saved by a drag.");
            var point = source.worldBound.center;
            Assert.IsTrue(inspector.worldBound.Contains(point) || !inspector.parent.worldBound.Contains(point),
                "The saved preview must cover the card when its clamped viewport can reach that card.");
            PointerGestureEvents.ClickAt(inspector.Q<Button>("close-card"));
            await room.Settle();
            Assert.IsEmpty(client.Board.Cards.CardId);
        }

        private static void AssertReachable(MountedMatchClient client, Button source, Rect before)
        {
            Assert.AreSame(source, client.Board.Hand.Q<Button>(source.name));
            Assert.AreEqual(before, source.worldBound, "Inspecting cannot move the next click destination.");
            var picked = source.panel.Pick(before.center);
            Assert.IsTrue(picked == source || source.Contains(picked),
                "The first click must keep its hand card above the saved overlapping preview; hit " + picked?.name);
        }

        private static async Task Receipt(MatchTestRoom room, MountedMatchClient client, UI10HttpBoundary boundary,
            int expectedCommands, string id, long revision)
        {
            Assert.AreEqual(expectedCommands, boundary.Commands, "Each complete double click sends one command.");
            Assert.AreEqual(CommandKind.DiscardCards, boundary.LastCommand.Kind);
            CollectionAssert.AreEqual(new[] { id }, boundary.LastCommand.Payload.Ids);
            var receipt = await client.Api.Get<CommandReceipt>(
                $"api/matches/{client.Board.View.MatchId}/receipts/{boundary.LastCommand.CommandId}", room.Token);
            Assert.IsTrue(receipt.Accepted);
            Assert.AreEqual(revision, receipt.Revision);
        }
    }
}
