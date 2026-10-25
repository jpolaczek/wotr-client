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
using Object = UnityEngine.Object;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class BattleWindowNetworkTests
    {
        [UnityTest]
        public IEnumerator RegionClickCloseAndCombatUseOneHostAndKeepSecretsThroughReconnect() => Wait(Window());

        [UnityTest]
        public IEnumerator ReliefForcesAndLegalCombatChoicesFitFourLayoutsWithRealHitTargets() => Wait(Layouts());

        [UnityTest]
        public IEnumerator CombatDragStartedBeforeAnotherDeviceCommitsCannotSpendAnotherCard() => Wait(StaleDrag());

        private static async Task StaleDrag()
        {
            using var room = await Connect("ui44-stale", ScenarioKind.BaseAcceptance);
            await AttackWithRear(room, "minas-tirith", "lossarnach");
            using var other = await room.AnotherDevice(room.Fp, null);
            other.Board.Battles.Open();
            await room.Settle();
            var board = room.Fp.Board;
            var source = CardGestures.Source(room.Fp, "base.fp.strategy.01");
            var destination = board.Tray.worldBound.center;
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = source.worldBound.center }))
                source.SendEvent(down);
            try
            {
                using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 0, mousePosition = destination }))
                    source.SendEvent(move);
                await room.Direct(other, "battle-no-card");
            }
            finally
            {
                using var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = destination });
                board.Root.SendEvent(up);
            }
            await room.Settle();
            Assert.AreEqual(2, board.View.Revision);
            Assert.Contains("base.fp.strategy.01", board.View.Hand);
            Assert.IsTrue(string.IsNullOrEmpty(board.View.Battle.Attacker.CardId));
            Assert.AreEqual(DecisionKind.CombatCard, room.Sh.Board.View.Decision.Kind);
        }

        private static async Task Window()
        {
            using var room = await Connect("ui44-window", ScenarioKind.BaseAcceptance, openPanels: false);
            await AttackWithRear(room, "minas-tirith", "lossarnach");
            var board = room.Fp.Board;
            // UI-149: the new battle opens its window for both players; closing it is the player's choice.
            Assert.IsTrue(board.Battles.IsOpen);
            Assert.IsTrue(room.Sh.Board.Battles.IsOpen);
            PointerGestureEvents.ClickAt(board.Battles.Root.Q<Button>("close-battle"));
            await room.Settle();
            Assert.IsFalse(board.Battles.IsOpen);
            Assert.AreEqual("minas-tirith", board.View.Battle.Attacker.RegionId);
            board.Map.Focus("lossarnach");
            await room.Settle();
            var pan = board.Map.Camera.Pan;
            var point = RegionInspectionTests.Terrain(board, "lossarnach");
            RegionInspectionTests.Move(board, point);
            await Task.Delay(100, room.Token);
            Assert.IsFalse(board.Battles.IsOpen, "Region hover never opens the battle.");
            await Task.Delay(180, room.Token);
            Assert.IsFalse(board.Battles.IsOpen);
            RegionInspectionTests.ClickTerrain(board, point);
            await room.Settle();
            Assert.IsTrue(board.Battles.IsOpen);
            Assert.IsTrue(board.Battles.Pinned);
            Assert.AreEqual(DisplayStyle.None, board.Hover.Panel.Root.resolvedStyle.display);
            CardGestures.Move(board.Battles.Root.Q<Label>("battle-decision-owner"));
            await Task.Delay(200, room.Token);
            Assert.IsTrue(board.Battles.IsOpen, "Moving from the region into its window keeps it open.");
            CardGestures.Move(board.Phase);
            await Task.Delay(220, room.Token);
            Assert.IsTrue(board.Battles.IsOpen, "Leaving the object does not close an explicit inspection.");
            Assert.IsTrue(board.Battles.Pinned);
            var rear = board.View.Units.Single(u => u.Nation == "rohan" && u.Location == "minas-tirith");
            Assert.IsFalse(board.View.Battle.Attacker.ParticipantIds.Contains(rear.Id));
            StringAssert.Contains("Rear guard", board.Tray.Q<Label>("battle-nonparticipants").text);
            Assert.IsTrue(board.Tray.Q<Label>("battle-characters").text.Contains("Gimli"));
            await ReadOnlyHand(room, board);
            PointerGestureEvents.ClickAt(board.Battles.Root.Q<Button>("close-battle"));
            await room.Fp.Connection.Refresh();
            await room.Settle();
            Assert.IsFalse(board.Battles.IsOpen, "The same projection respects Close.");
            Assert.AreEqual(pan, board.Map.Camera.Pan);
            await PanAcross(room, board);
            PointerGestureEvents.ClickAt(board.Notice.Button);
            await room.Settle();
            Assert.IsTrue(board.Battles.Pinned);
            Assert.IsTrue(board.HandDrawer.IsOpen, "UI-91: the hand is a permanent part of the right column.");
            var initial = board.Battles.Root.layout.position;
            var header = board.Battles.Root.Q(className: FloatingWindow.HandleClass);
            header.Focus();
            // UI-324: the window opens in the middle; wider than the middle of a narrow board it touches the left edge, so
            // the arrow moves it toward the side with room.
            var arrow = initial.x >= 16 ? KeyCode.LeftArrow : KeyCode.RightArrow;
            using (var key = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = arrow })) header.SendEvent(key);
            await room.Settle();
            Assert.AreNotEqual(initial, board.Battles.Root.layout.position);
            const string first = "base.fp.strategy.01";
            var source = CardGestures.Source(room.Fp, first);
            var openings = 0;
            System.Action opened = () => openings++;
            board.Cards.Opening += opened;
            // UI-147: one click on the Combat card plays it; no window with its text opens.
            PointerGestureEvents.ClickAt(source);
            await room.At(2);
            board.Cards.Opening -= opened;
            Assert.AreEqual(0, openings, "Playing a Combat card opens no card window.");
            Assert.IsFalse(board.Cards.Pinned);
            StringAssert.DoesNotContain(first, JsonConvert.SerializeObject(room.Sh.Board.View));
            Assert.IsNull(room.Sh.Board.View.Battle.Attacker.CardId);
            var count = room.Sh.Connected;
            room.Sh.Connection.Reconnect();
            await room.Until(() => room.Sh.Connected > count);
            Assert.IsNull(room.Sh.Board.View.Battle.Attacker.CardId);
            PointerGestureEvents.ClickAt(room.Sh.Board.Notice.Button);
            await room.Settle();
            var second = CardGestures.Source(room.Sh, "base.sh.strategy.01");
            PointerGestureEvents.ClickAt(second);
            await room.At(3);
            Assert.IsEmpty(room.Sh.Board.Cards.CardId, "UI-147: the played card's text stays out of a separate window.");
            Assert.IsTrue(board.View.Battle.CardsRevealed);
            Assert.AreEqual(first, room.Sh.Board.View.Battle.Attacker.CardId);
            Assert.AreEqual(1, board.View.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Resolving));
        }

        private static async Task ReadOnlyHand(MatchTestRoom room, BoardScreen board)
        {
            // UI-91: the permanent hand unrolls the card for reading; it cannot be played during a Combat choice.
            var card = board.Hand.Q<Button>("base.fp.strategy.01");
            Assert.IsNull(card.userData, "A reading copy cannot start a second Combat command.");
            PointerGestureEvents.ClickAt(card);
            await room.Settle();
            Assert.AreEqual(card.name, board.ExpandedCard);
            StringAssert.Contains("battle window", card.Q<Label>("hand-card-hint").text);
            Assert.AreEqual(1, board.View.Revision);
            board.Cards.Close();
            board.HandDrawer.Close();
            await room.Settle();
        }

        private static async Task PanAcross(MatchTestRoom room, BoardScreen board)
        {
            var point = board.Map.worldBound.center;
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 1, mousePosition = point })) board.Map.SendEvent(down);
            try
            {
                using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 1, mousePosition = point + Vector2.right * 30 })) board.Map.SendEvent(move);
                await Task.Delay(250, room.Token);
                Assert.IsFalse(board.Battles.IsOpen);
            }
            finally
            {
                using var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 1, mousePosition = point + Vector2.right * 30 });
                board.Map.SendEvent(up);
            }
            await room.Settle();
        }

        private static async Task Layouts()
        {
            using var room = await Connect("ui44-layout", ScenarioKind.BaseAcceptance, openPanels: false);
            await AttackWithRear(room, "lossarnach", "minas-tirith");
            var client = room.Fp;
            var board = client.Board;
            Assert.AreEqual(BattleMode.Relief, board.View.Battle.Mode);
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    texture.Create();
                    client.Panel.targetTexture = texture;
                    client.Panel.scale = scale;
                    board.Windows.Clear();
                    await room.Settle();
                    PointerGestureEvents.ClickAt(board.Notice.Button);
                    await room.Settle();
                    Assert.IsEmpty(board.Tray.Query<ScrollView>().ToList());
                    var text = board.Tray.Q<Label>("battle-nonparticipants").text;
                    StringAssert.Contains("Garrison · not fighting", text);
                    StringAssert.Contains("Rear guard", text);
                    LayoutTests.Save(texture, $"ui44-combat-{width}-{scale:F2}.png");
                    Assert.That(board.Battles.Root.worldBound.yMax, Is.LessThanOrEqualTo(board.Map.worldBound.yMax + 1),
                        string.Join("\n", board.Tray.Children().Select(c => c.name + " " + c.layout + " " + string.Join(",", c.GetClasses()))));
                    Assert.That(board.Battles.Root.worldBound.xMax, Is.LessThanOrEqualTo(board.Map.worldBound.xMax + 1));
                    var eligible = board.View.Options.Single(o => o.Kind == CommandKind.CommitCombatCard).EligibleIds;
                    var choices = board.Tray.Query<Button>(className: "combat-choice").ToList()
                        .Where(b => !b.ClassListContains("combat-choice-unavailable")).ToList();
                    Assert.AreEqual(eligible.Count, choices.Count);
                    // UI-111: the rest of the hand is shown dimmed with the condition its Combat part does not meet.
                    var unavailable = board.Tray.Query<Button>(className: "combat-choice-unavailable").ToList();
                    Assert.AreEqual(board.View.Hand.Count(id => !eligible.Contains(id) && CardLibrary.Cards.ContainsKey(id)), unavailable.Count);
                    foreach (var card in unavailable)
                    {
                        Assert.IsFalse(card.enabledInHierarchy, card.name);
                        Assert.IsNotEmpty(card.Q<Label>("combat-choice-condition").text, card.name);
                    }
                    foreach (var card in choices)
                    {
                        var picked = board.Root.panel.Pick(card.worldBound.center);
                        Assert.IsTrue(picked == card || card.Contains(picked), card.name + " must be reachable.");
                    }
                    var noCard = board.Tray.Q<Button>("battle-no-card");
                    Assert.AreSame(noCard, board.Root.panel.Pick(noCard.worldBound.center));
                    LayoutTests.Save(texture, $"ui44-combat-{width}-{scale:0.00}.png");
                    board.Battles.Close();
                    client.Panel.targetTexture = client.Viewport;
                    texture.Release();
                    Object.Destroy(texture);
                }
        }

        private static async Task AttackWithRear(MatchTestRoom room, string source, string target)
        {
            var client = room.Fp;
            ArmyGestures.Die(client, ArmyGestures.ArmyDie(client));
            await ArmyGestures.All(client, source);
            // The army's click opens its region panel; its figures are laid out on the next frame.
            await room.Settle();
            var rear = client.Board.View.Units.Single(u => u.Nation == "rohan" && u.Location == source);
            PointerGestureEvents.ClickAt(client.Board.Hover.Panel.Root.Q<Button>("hover-figure-" + rear.Id));
            await room.Settle();
            await ArmyGestures.Target(room, client, target);
            Assert.AreEqual(DecisionKind.CombatCard, client.Board.View.Decision.Kind);
            Assert.AreEqual(1, client.Board.View.Revision);
        }

        private static Vector2 BareRegion(BoardScreen board, string region)
        {
            var center = RegionInspectionTests.Terrain(board, region);
            for (var radius = 10; radius <= 120; radius += 10)
                for (var angle = 0; angle < 360; angle += 30)
                {
                    var point = center + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;
                    if (board.Root.panel.Pick(point) == board.Map && board.Map.HitTest(board.Map.WorldToLocal(point)) == region)
                        return point;
                }
            Assert.Fail("No visible terrain in " + region);
            return center;
        }
    }
}
