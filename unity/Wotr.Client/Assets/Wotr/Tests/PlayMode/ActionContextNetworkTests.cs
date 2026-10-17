using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class ActionContextNetworkTests
    {
        [UnityTest]
        public IEnumerator LogOpensTwoRecordedMovesWithoutChangingCurrentArmyDraft() => Wait(Moves());
        [UnityTest]
        public IEnumerator EarlierBattleAndHuntKeepTheirIdentityAfterNextOne() => Wait(Archives());
        [UnityTest]
        public IEnumerator EveryOtherFamilyUsesItsHostAndKeepsLocationlessActionsOffTheMap() => Wait(Hosts());

        private static async Task Hosts()
        {
            var cases = new[]
            {
                ("ui42-hosts", new[] { ActionContextKind.Fellowship, ActionContextKind.Hunt, ActionContextKind.General, ActionContextKind.Dice }),
                ("ui42-ring", new[] { ActionContextKind.Resources }),
                ("ui42-politics", new[] { ActionContextKind.Politics }),
                ("ui42-cards", new[] { ActionContextKind.Cards }),
                ("ui42-characters", new[] { ActionContextKind.Characters })
            };
            foreach (var (scenario, kinds) in cases)
            {
                using var room = await Connect(scenario, ScenarioKind.BaseAcceptance);
                foreach (var client in new[] { room.Fp, room.Sh })
                {
                    var board = client.Board;
                    var revision = board.View.Revision;
                    await OpenLog(room, board);
                    foreach (var kind in kinds)
                    {
                        var group = board.Log.Model.Rows.First(r => r.Step == null && r.Group.Action.Context.Kind == kind).Group;
                        var pan = board.Map.Camera.Pan;
                        var selection = board.Map.Selected;
                        await OpenEntry(room, board, group.Action.Id);
                        Assert.AreEqual(kind, board.ActionHistory.Context.Kind, scenario);
                        Assert.AreEqual(revision, board.View.Revision);
                        Assert.AreEqual(selection, board.Map.Selected);
                        if (kind is ActionContextKind.General or ActionContextKind.Dice or ActionContextKind.Resources or ActionContextKind.Cards or ActionContextKind.Politics)
                        {
                            Assert.IsEmpty(board.ActionHistory.Context.Regions, scenario);
                            Assert.AreEqual(pan, board.Map.Camera.Pan, scenario);
                        }
                        board.Log.CloseCard();
                    }
                    board.Log.Close();
                    // This is the same entry point used by external notices, without loading a Log page.
                    var last = board.Log.Model.Rows.First().Group.Action;
                    board.ActionHistory.Open(new ActionOpenRequest(last.Id, last.Version));
                    await room.Until(() => board.ActionHistory.Context != null);
                    await room.Settle();
                    Assert.IsFalse(board.Log.IsOpen);
                    board.Log.CloseCard();
                }
            }
        }

        private static async Task Moves()
        {
            using var room = await Connect("ui42-moves", ScenarioKind.BaseAcceptance);
            var client = room.Sh;
            var board = client.Board;
            var revision = board.View.Revision;
            ArmyGestures.Die(client, ArmyGestures.ArmyDie(client));
            await ArmyGestures.All(client, "lossarnach");
            var die = board.ActionDie.DieId;
            var selected = board.Map.Selected;
            var targets = board.Map.DraftOverlay.Targets.ToArray();
            Assert.IsNotEmpty(targets);
            await OpenLog(room, board);
            var action = board.Log.Model.Rows.First(r => r.Group.Action.Command == CommandKind.MoveArmies).Group;
            var pan = board.Map.Camera.Pan;
            await OpenEntry(room, board, action.Action.Id);
            Assert.AreEqual(ActionContextKind.Army, board.ActionHistory.Context.Kind);
            Assert.AreEqual(2, board.ActionHistory.Context.Movements.Count);
            var first = board.ActionHistory.Context.Movements[0];
            Assert.AreEqual(2, first.Participants.Count(p => p.UnitKind == UnitKind.Regular));
            Assert.AreEqual(3, first.Participants.Count(p => p.UnitKind == UnitKind.Elite));
            Assert.AreEqual("minas-tirith", first.From);
            Assert.AreEqual("osgiliath", first.To);
            Assert.AreEqual("westemnet", board.ActionHistory.Context.Movements[1].To);
            Assert.AreNotEqual(pan, board.Map.Camera.Pan);
            Assert.AreEqual(selected, board.Map.Selected);
            Assert.AreEqual(die, board.ActionDie.DieId);
            CollectionAssert.AreEquivalent(targets, board.Map.DraftOverlay.Targets);
            Assert.IsTrue(board.Map.HistoryOverlay.Suppressed, "Current targets take priority over recorded region outlines.");
            Assert.IsNull(board.Map.HistoryOverlay.Root.userData);
            Assert.IsEmpty(board.Map.HistoryOverlay.Root.Query<Button>().ToList());
            var target = targets.First();
            var point = MapContourProbe.ReachRegion(board.Map, target);
            Assert.AreSame(board.Map, board.Root.panel.Pick(point));
            Assert.AreEqual(target, board.Map.HitTest(board.Map.WorldToLocal(point)));
            Assert.AreEqual(revision, board.View.Revision);
            board.Log.Close();
            await room.Settle();
            Assert.IsTrue(board.Log.Pinned);
            Assert.IsNotNull(board.Map.HistoryOverlay.Context);
            PointerGestureEvents.ClickAt(board.Log.Popup.Q<Button>("close-action-summary"));
            await room.Settle();
            Assert.IsNull(board.Map.HistoryOverlay.Context);
            CollectionAssert.AreEquivalent(targets, board.Map.DraftOverlay.Targets);
        }

        private static async Task Archives()
        {
            using (var room = await Connect("ui42-battles", ScenarioKind.BaseAcceptance))
                foreach (var client in new[] { room.Fp, room.Sh })
                {
                    var board = client.Board;
                    board.Battles.Close();
                    await OpenLog(room, board);
                    var old = board.Log.Model.Rows.Where(r => r.Step == null && r.Group.Action.Command == CommandKind.AttackArmy)
                        .Select(r => r.Group).Last();
                    var battle = old.Action.Context.BattleId;
                    Assert.AreNotEqual(board.View.Battle.Id, battle);
                    await OpenEntry(room, board, old.Action.Id);
                    Assert.AreEqual(battle, board.Log.Card.Archive.Displayed.Id);
                    Assert.AreEqual(BattleReportStatus.Completed, board.Log.Card.Archive.Displayed.Status);
                    Assert.AreEqual(1, board.Log.Card.Archive.Displayed.Rounds.Count);
                    Assert.AreEqual(battle, board.ActionHistory.Context.BattleId);
                    Assert.IsTrue(board.ActionHistory.Context.Movements.Any(m => m.Kind == ActionMovementKind.Attack));
                    board.Log.Close();
                    var pan = board.Map.Camera.Pan;
                    var version = board.Log.Card.Displayed.Action.Version;
                    var connected = client.Connected;
                    client.Connection.Reconnect();
                    await room.Until(() => client.Connected > connected);
                    await room.Settle();
                    Assert.AreEqual(version, board.Log.Card.Displayed.Action.Version);
                    Assert.AreEqual(battle, board.Log.Card.Archive.Displayed.Id);
                    Assert.AreEqual(pan, board.Map.Camera.Pan);
                    Assert.IsFalse(board.Log.IsOpen);
                    board.Log.CloseCard();
                }
            using (var room = await Connect("ui42-hunts", ScenarioKind.BaseAcceptance))
                foreach (var client in new[] { room.Fp, room.Sh })
                {
                    var board = client.Board;
                    await OpenLog(room, board);
                    var old = board.Log.Model.Rows.Where(r => r.Step == null && r.Group.Action.Command == CommandKind.MoveFellowship)
                        .Select(r => r.Group).Last();
                    var chain = old.Action.Context.HuntChainId;
                    Assert.AreNotEqual(board.View.HuntReport.Id, chain);
                    await OpenEntry(room, board, old.Action.Id);
                    Assert.AreEqual(chain, board.ActionHistory.Context.HuntChainId);
                    Assert.IsTrue(board.ActionHistory.Context.Hunt.All(h => h.ChainId == chain));
                    Assert.IsNotEmpty(board.ActionHistory.Context.Hunt);
                    Assert.AreEqual(ActionContextKind.Hunt, board.ActionHistory.Context.Kind);
                    board.Log.Close();
                    board.Log.CloseCard();
                }
        }

        internal static async Task OpenLog(MatchTestRoom room, BoardScreen board)
        {
            PointerGestureEvents.ClickAt(board.Log.Toggle);
            await room.Until(() => board.Log.Model.Loaded);
            await room.Settle();
        }

        internal static async Task OpenEntry(MatchTestRoom room, BoardScreen board, string id)
        {
            var rows = board.Log.Model.Rows;
            var index = Enumerable.Range(0, rows.Count).First(i => rows[i].Id == id);
            board.Log.List.Root.scrollOffset = new Vector2(0, index * ActionLogList.RowHeight);
            await room.Settle();
            var button = board.Log.Root.Q<Button>("log-open-" + id);
            Hit(button);
            PointerGestureEvents.ClickAt(button);
            await room.Until(() => board.ActionHistory.Context != null);
            await room.Settle();
        }

        private static void Hit(VisualElement element)
        {
            Assert.NotNull(element);
            var hit = element.panel.Pick(element.worldBound.center);
            Assert.IsTrue(hit == element || element.Contains(hit), element.name + " covered by " + hit?.name);
        }
    }
}
