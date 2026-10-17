using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class LiveActivityNetworkTests
    {
        [UnityTest]
        public IEnumerator TwoMovesAttackCardsDiceRetreatAndAdvanceKeepNoticesAndArchivesConsistent() => Wait(Play());

        private static async Task Play()
        {
            using var room = await Connect("ui47-live-activity", ScenarioKind.BaseAcceptance, openPanels: false);
            await ActionNoticeNetworkTests.Loaded(room);
            var die = ArmyGestures.ArmyDie(room.Fp);
            ArmyGestures.Die(room.Fp, die);
            await ArmyGestures.All(room.Fp, "minas-tirith");
            await ArmyGestures.Target(room, room.Fp, "osgiliath");
            await ReadLatest(room, room.Sh);
            var movement = room.Sh.Board.Log.Card.Displayed.Action.Id;
            var first = room.Sh.Board.ActionHistory.Context.Movements.Single();
            Assert.AreEqual(2, first.Participants.Count(p => p.UnitKind == UnitKind.Regular));
            Assert.AreEqual(3, first.Participants.Count(p => p.UnitKind == UnitKind.Elite));
            Assert.AreEqual(1, room.Fp.Board.View.ActionSequence.Remaining);
            room.Sh.Board.Log.CloseCard();
            await ArmyGestures.All(room.Fp, "edoras");
            await ArmyGestures.Target(room, room.Fp, "westemnet");
            await ReadLatest(room, room.Sh);
            Assert.AreEqual(movement, room.Sh.Board.Log.Card.Displayed.Action.Id);
            Assert.AreEqual(2, room.Sh.Board.ActionHistory.Context.Movements.Count);
            Assert.AreEqual(1, room.Sh.Board.Log.Card.Displayed.Action.Costs.Count(c => c.Kind == ActionCostKind.Die));
            Assert.AreEqual(DieLocation.Spent, room.Fp.Board.View.Dice.Single(d => d.Id == die).Location);
            room.Sh.Board.Log.CloseCard();

            await room.Attack(room.Sh, "north-ithilien", "osgiliath");
            var battleId = room.Sh.Board.View.Battle.Id;
            // UI-149: the new battle opens its window; the player closes it to read the chronicle.
            Assert.IsTrue(room.Fp.Board.Battles.IsOpen);
            PointerGestureEvents.ClickAt(room.Fp.Board.Battles.Root.Q<Button>("close-battle"));
            await room.Settle();
            Assert.IsFalse(room.Fp.Board.Battles.IsOpen);
            await ReadLatest(room, room.Fp);
            Assert.AreEqual(battleId, room.Fp.Board.Log.Card.Archive.Displayed.Id);
            room.Fp.Board.Log.CloseCard();
            await CombatCard(room, room.Sh, "base.sh.strategy.01");
            await ReadLatest(room, room.Fp);
            Assert.IsNull(room.Fp.Board.View.Battle.Attacker.CardId);
            Assert.IsFalse(room.Fp.Board.Log.Card.Displayed.Steps.SelectMany(s => s.Facts.Cards)
                .Any(c => c.Id == "base.sh.strategy.01"), "The opponent's commitment remains face down in the action card.");
            room.Fp.Board.Log.CloseCard();
            await CombatCard(room, room.Fp, "base.fp.strategy.01");
            await ReadLatest(room, room.Sh);
            Assert.AreEqual("base.fp.strategy.01", room.Sh.Board.View.Battle.Defender.CardId);
            room.Sh.Board.Log.CloseCard();
            await RollsAndLosses(room);
            await OwnDecision(room, room.Sh);
            await room.Direct(room.Sh, "action-ContinueBattle");
            await ReadLatest(room, room.Fp);
            room.Fp.Board.Log.CloseCard();
            await OwnDecision(room, room.Fp);
            await ArmyGestures.Target(room, room.Fp, "minas-tirith");
            await ReadLatest(room, room.Sh);
            room.Sh.Board.Log.CloseCard();
            await OwnDecision(room, room.Sh);
            await room.SelectAllAdvancing(room.Sh);
            await ReadLatest(room, room.Fp);
            Assert.IsNull(room.Fp.Board.View.Control["osgiliath"], "A region without a settlement has no captured control marker.");
            Assert.IsTrue(room.Fp.Board.View.Units.Any(u => u.Side == Side.Shadow && u.Location == "osgiliath"));
            Assert.IsFalse(room.Fp.Board.View.Units.Any(u => u.Side == Side.FreePeoples && u.Location == "osgiliath"));
            Assert.IsNull(room.Fp.Board.View.Battle);
            Assert.AreEqual(BattleReportStatus.Completed, room.Fp.Board.Log.Card.Archive.Displayed.Status);
            await VerifyHistory(room, battleId, movement);
        }

        private static async Task CombatCard(MatchTestRoom room, MountedMatchClient client, string id)
        {
            await OwnDecision(room, client);
            var revision = client.Board.View.Revision;
            // UI-147: one click plays the Combat card.
            PointerGestureEvents.ClickAt(CardGestures.Source(client, id));
            await room.At(revision + 1);
        }

        private static async Task RollsAndLosses(MatchTestRoom room)
        {
            for (var step = 0; step < 8; step++)
            {
                var client = room.Actor;
                var kind = client.Board.View.Decision.Kind;
                if (kind == DecisionKind.BattleContinue)
                    return;
                await OwnDecision(room, client);
                if (kind == DecisionKind.BattleReroll)
                {
                    var option = client.Board.View.Options.Single(o => o.Kind == CommandKind.RerollBattle);
                    foreach (var id in option.EligibleIds.Take(option.Maximum))
                        PointerGestureEvents.ClickAt(client.Board.Tray.Q<Button>("battle-die-" + id));
                }
                else
                {
                    Assert.AreEqual(DecisionKind.Losses, kind);
                    SelectLosses(client);
                }
                await room.Confirm(client);
                if (kind == DecisionKind.Losses)
                {
                    var observer = client == room.Fp ? room.Sh : room.Fp;
                    await ReadLatest(room, observer);
                    observer.Board.Log.CloseCard();
                }
            }
            Assert.Fail("The first round did not reach continuation.");
        }

        private static void SelectLosses(MountedMatchClient client)
        {
            var view = client.Board.View;
            var option = view.Options.Single(o => o.Kind == CommandKind.TakeBattleLosses);
            var remaining = view.Battle.PendingHits;
            foreach (var unit in view.Units.Where(u => option.EligibleIds.Contains(u.Id)).OrderBy(u => u.Kind))
            {
                if (remaining == 0)
                    break;
                var button = client.Board.Tray.Q<Button>("loss-" + unit.Id);
                PointerGestureEvents.ClickAt(button);
                var hits = unit.Kind == UnitKind.Elite && remaining >= 2 ? 2 : 1;
                if (hits == 2)
                    PointerGestureEvents.ClickAt(button);
                remaining -= hits;
            }
            Assert.AreEqual(0, remaining);
        }

        private static async Task OwnDecision(MatchTestRoom room, MountedMatchClient client)
        {
            client.Board.Log.CloseCard();
            PointerGestureEvents.ClickAt(client.Board.Notice.Button);
            await room.Settle();
            Assert.IsTrue(client.Board.Battles.IsOpen);
            Assert.IsNotNull(client.Board.View.Decision);
        }

        private static async Task ReadLatest(MatchTestRoom room, MountedMatchClient client)
        {
            await room.Until(() => client.Activity.Model.StepCount > 0);
            var revision = client.Board.View.Revision;
            await ActionNoticeNetworkTests.OpenLatest(room, client);
            Assert.IsFalse(client.Board.Log.IsOpen);
            Assert.AreEqual(revision, client.Board.View.Revision);
            Assert.IsNotNull(client.Board.ActionHistory.Context);
        }

        private static async Task VerifyHistory(MatchTestRoom room, string battleId, string movementId)
        {
            var revision = room.Fp.Board.View.Revision;
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                client.Board.Log.CloseCard();
                client.Board.Battles.Close();
                PointerGestureEvents.ClickAt(client.Board.BattleReport.Open);
                await room.Until(() => client.Board.BattleReport.Displayed?.Status == BattleReportStatus.Completed);
                // The HTTP result replaces the scroll content and resizes the
                // report. Wait for that layout before hitting the close button.
                await room.Settle();
                var report = client.Board.BattleReport.Displayed;
                Assert.AreEqual(battleId, report.Id);
                Assert.AreEqual(1, report.Rounds.Count);
                Assert.AreEqual(3, report.Rounds[0].Result.Attacker.Dice.Count);
                Assert.AreEqual(5, report.Rounds[0].Result.Defender.Dice.Count);
                Assert.IsTrue(report.Steps.Any(s => s.Command == CommandKind.RetreatBattle));
                Assert.IsTrue(report.Steps.Any(s => s.Command == CommandKind.AdvanceBattle));
                LayoutTests.Save(client.Viewport, "map17-live-history-report-" + client.Board.View.You + ".png");
                PointerGestureEvents.ClickAt(client.Board.BattleReport.Root.Q<Button>("close-battle-report"));
                await ActionContextNetworkTests.OpenLog(room, client.Board);
                await ActionContextNetworkTests.OpenEntry(room, client.Board, movementId);
                var first = client.Board.ActionHistory.Context.Movements.First();
                Assert.AreEqual(3, first.Participants.Count(p => p.UnitKind == UnitKind.Elite),
                    "The earlier move must retain its original figures after combat casualties.");
                Assert.AreEqual(revision, client.Board.View.Revision);
                client.Board.Log.CloseCard();
                client.Board.Log.Close();
            }
        }
    }
}
