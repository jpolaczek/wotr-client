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
    public class CardGestureNetworkTests
    {
        [UnityTest]
        public IEnumerator CombatPlayAndDropKeepBothCardsSecretUntilReveal() => Wait(Combat());

        private static async Task Combat()
        {
            using var fpBoundary = new UI10HttpBoundary();
            using var shBoundary = new UI10HttpBoundary();
            using (var room = await Connect("ui18-combat", ScenarioKind.BaseAcceptance,
                fpHandler: fpBoundary, shHandler: shBoundary))
            {
                fpBoundary.MatchPath = shBoundary.MatchPath = "/api/matches/" + room.Fp.Board.View.MatchId;
                await room.Attack(room.Fp, "minas-tirith", "lossarnach");
                var fp = room.Fp;
                var sh = room.Sh;
                const string first = "base.fp.strategy.01";
                const string second = "base.sh.strategy.01";
                Assert.Contains(first, fp.Board.View.Options.Single(o => o.Kind == CommandKind.CommitCombatCard).EligibleIds);
                var revision = fp.Board.View.Revision;
                var commands = fpBoundary.Commands;
                var attackerDice = JsonConvert.SerializeObject(fp.Board.View.Dice);
                await CardGestures.Preview(fp, first);
                await room.Settle();
                Assert.AreEqual(revision, fp.Board.View.Revision);
                Assert.IsEmpty(fp.Board.Cards.CardId, "The permanent hand reads Combat inline.");
                Assert.IsNull(fp.Board.Tray.Q<Button>("confirm-decision"));
                Assert.AreEqual(commands, fpBoundary.Commands, "Reading the hand must not send a command.");
                LayoutTests.Save(fp.Viewport, "ui74-combat-before-play.png");
                // UI-147: one click on the Combat card in the battle window plays it.
                PointerGestureEvents.ClickAt(CardGestures.Source(fp, first));
                await room.At(revision + 1);
                Assert.IsEmpty(fp.Board.Cards.CardId, "Playing opens no window with the card's text.");
                await CombatReceipt(room, fp, fpBoundary, commands + 1, first, revision + 1);
                Assert.AreEqual(attackerDice, JsonConvert.SerializeObject(fp.Board.View.Dice), "Committing Combat has no action-die cost.");
                Assert.IsTrue(sh.Board.View.Battle.Attacker.CardCommitted);
                Assert.IsNull(sh.Board.View.Battle.Attacker.CardId);
                StringAssert.DoesNotContain(first, JsonConvert.SerializeObject(sh.Board.View));
                var connection = fp.Connected;
                fp.Connection.Reconnect();
                await room.Until(() => fp.Connected > connection);
                await room.Settle();
                Assert.IsFalse(fp.Board.Cards.Pinned);
                Assert.IsEmpty(fp.Board.Cards.CardId);
                var dice = sh.Board.View.Dice.Count(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
                await CardGestures.Preview(sh, second);
                await room.Settle();
                var card = CardGestures.Source(sh, second);
                Assert.IsTrue(card.ClassListContains("playable-card"));
                await Drop(room, sh, card, sh.Board.Tray, holdMilliseconds: 250,
                    dropPoint: CombatTrayPoint(sh));
                Assert.IsTrue(fp.Board.View.Battle.CardsRevealed);
                Assert.AreEqual(first, sh.Board.View.Battle.Attacker.CardId);
                Assert.AreEqual(second, fp.Board.View.Battle.Defender.CardId);
                await CombatReceipt(room, sh, shBoundary, 1, second, revision + 2);
                Assert.AreEqual(dice, sh.Board.View.Dice.Count(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available));
            }
        }

        private static async Task CombatReceipt(MatchTestRoom room, MountedMatchClient client,
            UI10HttpBoundary boundary, int count, string card, long revision)
        {
            Assert.AreEqual(count, boundary.Commands, "Each complete card gesture sends exactly one command.");
            Assert.AreEqual(CommandKind.CommitCombatCard, boundary.LastCommand.Kind);
            Assert.AreEqual(card, boundary.LastCommand.Payload.CardId);
            var receipt = await client.Api.Get<CommandReceipt>(
                $"api/matches/{client.Board.View.MatchId}/receipts/{boundary.LastCommand.CommandId}", room.Token);
            Assert.IsTrue(receipt.Accepted);
            Assert.AreEqual(revision, receipt.Revision);
        }

        private static Vector2 CombatTrayPoint(MountedMatchClient client)
        {
            var tray = client.Board.Tray;
            var bounds = tray.worldBound;
            // Miniatures are drag sources, not play targets. The tray's center
            // may now lie inside the only eligible card; use its exposed surface.
            foreach (var y in new[] { .9f, .1f, .5f })
                foreach (var x in new[] { .25f, .5f, .75f })
                {
                    var point = bounds.min + Vector2.Scale(bounds.size, new Vector2(x, y));
                    var picked = tray.panel.Pick(point);
                    if (picked != tray && !tray.Contains(picked))
                        continue;
                    while (picked != null && picked.userData is not DraftToken)
                        picked = picked.parent;
                    if (picked?.userData is DraftToken { Object: DraftObject.Tray })
                        return point;
                }
            Assert.Fail("The Combat tray has no exposed drop surface outside its card choices.");
            return default;
        }

        internal static async Task Drop(MatchTestRoom room, MountedMatchClient client, VisualElement source, VisualElement target,
            bool commits = true, int holdMilliseconds = 0, Vector2? dropPoint = null,
            System.Action beforeRelease = null)
        {
            if (client.Board.Hand.Contains(source))
                await CardGestures.Open(client);
            var revision = client.Board.View.Revision;
            var bounds = target.worldBound;
            var destination = dropPoint ?? target.worldBound.center;
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = source.worldBound.center, clickCount = 1 }))
                source.SendEvent(down);
            await room.Settle();
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 0, mousePosition = destination }))
                source.SendEvent(move);
            await room.Settle();
            if (holdMilliseconds > 0)
                await Task.Delay(holdMilliseconds, room.Token);
            if (client.Board.View.Decision?.Kind == DecisionKind.CombatCard)
            {
                Assert.AreEqual(bounds, target.worldBound, "Closing a transient card preview must not move the battle drop target.");
                var hit = client.Board.Root.panel.Pick(destination);
                var token = hit;
                while (token != null && token.userData is not DraftToken)
                    token = token.parent;
                LayoutTests.Save(client.Viewport, "ui47-combat-drop.png");
                Assert.AreEqual(DraftObject.Tray, (token?.userData as DraftToken)?.Object,
                    $"Combat drop at {destination} hit {hit?.name}, token {token?.name}; tray {target.worldBound}.");
            }
            if (client.Board.Hand.Contains(source))
            {
                Assert.IsTrue(client.Board.HandDrawer.IsOpen, "Dragging keeps the hand open.");
                var picked = client.Board.Root.panel.Pick(destination);
                LayoutTests.Save(client.Viewport, "ui29-card-drop.png");
                Assert.IsTrue(picked == target || target.Contains(picked),
                    $"Card drop target {target.name} is covered by {picked?.name} ({picked?.GetType().Name}).");
            }
            beforeRelease?.Invoke();
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = destination, clickCount = 1 }))
                client.Board.Root.SendEvent(up);
            if (commits)
                await room.At(revision + 1);
            else
            {
                await room.Settle();
                Assert.AreEqual(revision, client.Board.View.Revision, "This drop only prepares a set of choices.");
            }
        }
    }
}
