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
    public class BattleNetworkTests
    {
        [UnityTest]
        public IEnumerator TwoClientsAttackResolveRealDiceAndReopenTheSavedReport() => Wait(Field());

        [UnityTest]
        public IEnumerator LossPreviewExplainsMissingRegularAndCommanderDeathAndCyclesElitesWithoutChangingLossOrder() => Wait(Losses());

        [UnityTest]
        public IEnumerator ContinuingRetreatingAndAdvancingUseOneTrayAndKeepTheCamera() => Wait(Retreat());

        private static async Task Field()
        {
            using (var room = await Connect("field"))
            {
                await room.Attack(room.Fp, "minas-tirith", "lossarnach");
                // Reaching the army and target is deliberate player navigation.
                var camera = room.Fp.Board.Map.Camera.Pan;
                Assert.AreEqual(DecisionKind.CombatCard, room.Fp.Board.View.Decision.Kind);
                Assert.IsNull(room.Sh.Board.View.Decision);
                Assert.IsNull(room.Fp.Board.Tray.Q<Button>("confirm-decision"));
                // The Shadow sees the attacker's hidden commitment in the first view after it.
                PlayerView hidden = null;
                void Seen(PlayerView view)
                {
                    if (hidden == null && view.Battle?.Attacker.CardCommitted == true && !view.Battle.CardsRevealed)
                        hidden = view;
                }
                room.Sh.Connection.Updated += Seen;
                await room.Direct(room.Fp, "battle-no-card");
                await room.NoCard(room.Sh);
                room.Sh.Connection.Updated -= Seen;
                Assert.IsNotNull(hidden, "The Shadow saw the attacker's commitment before the reveal.");
                Assert.IsNull(hidden.Battle.Attacker.CardId, "A committed card stays secret until both have chosen.");
                var connected = room.Fp.Connected;
                room.Fp.Connection.Reconnect();
                await room.Until(() => room.Fp.Connected > connected);
                await room.Settle();
                await RollsAndLosses(room);
                if (room.Fp.Board.View.Battle != null)
                {
                    if (room.Actor.Board.View.Decision.Kind == DecisionKind.BattleContinue)
                        await room.Direct(room.Actor, "action-StopBattle");
                    else
                        await room.Direct(room.Actor, "battle-stay");
                }
                Assert.IsNull(room.Fp.Board.View.Battle);
                Assert.AreEqual(3, room.Fp.Board.View.LastBattleRound.Attacker.Dice.Count);
                Assert.AreEqual(3, room.Sh.Board.View.LastBattleRound.Defender.Dice.Count);
                Assert.AreEqual(1, room.Fp.Board.View.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Spent));
                Assert.AreEqual(camera, room.Fp.Board.Map.Camera.Pan);
                CardNetworkTests.Click(room.Fp.Board.BattleReport.Open);
                await room.Settle();
                Assert.AreEqual(6, room.Fp.Board.BattleReport.Root.Query<Button>(className: "battle-die").ToList().Count);
            }
        }

        private static async Task RollsAndLosses(MatchTestRoom room)
        {
            for (var step = 0; step < 8 && room.Fp.Board.View.Battle != null; step++)
            {
                var client = room.Actor;
                var view = client.Board.View;
                if (view.Decision.Kind == DecisionKind.BattleReroll)
                {
                    var option = view.Options.Single(o => o.Kind == CommandKind.RerollBattle);
                    foreach (var id in option.EligibleIds.Take(option.Maximum))
                    {
                        Click(client, "battle-die-" + id);
                        if (view.You == Side.FreePeoples)
                            ArmyGestures.Click(client.Board.Tray.Q<Button>("battle-die-" + id), 2);
                    }
                }
                else if (view.Decision.Kind == DecisionKind.Losses)
                {
                    var option = view.Options.Single(o => o.Kind == CommandKind.TakeBattleLosses);
                    var remaining = view.Battle.PendingHits;
                    foreach (var unit in view.Units.Where(u => option.EligibleIds.Contains(u.Id)).OrderBy(u => u.Kind))
                    {
                        if (remaining <= 0)
                            break;
                        Click(client, "loss-" + unit.Id);
                        if (unit.Kind == UnitKind.Elite && remaining >= 2)
                            Click(client, "loss-" + unit.Id);
                        remaining -= unit.Kind == UnitKind.Elite && remaining >= 2 ? 2 : 1;
                    }
                }
                else
                    return;
                await room.Confirm(client);
            }
        }

        private static async Task Losses()
        {
            using (var room = await Connect("no-regular"))
            {
                var elite = room.Fp.Board.View.Battle.Attacker.Units.Single(u => u.Kind == UnitKind.Elite).Id;
                Click(room.Fp, "loss-" + elite);
                await room.Until(() => room.Fp.Board.Tray.Q<Button>("confirm-decision").enabledSelf);
                var status = room.Fp.Board.Tray.Q<Label>(className: "draft-status").text;
                StringAssert.Contains("No regular available", status);
                StringAssert.Contains("Gimli", status);
                var count = room.Fp.Connected;
                room.Fp.Connection.Reconnect();
                await room.Until(() => room.Fp.Connected > count);
                await room.Settle();
                Assert.IsTrue(room.Fp.Board.Tray.Q<Button>("loss-" + elite).ClassListContains("selected"));
                await room.Confirm(room.Fp);
                Assert.IsFalse(room.Fp.Board.View.Characters.Any(c => c.Id == "base.character.gimli"));
                Assert.AreEqual("casualties", room.Fp.Board.View.Units.Single(u => u.Id == elite).Location);
            }
            using (var room = await Connect("two-hits"))
            {
                var elite = room.Fp.Board.View.Battle.Attacker.Units.Single(u => u.Kind == UnitKind.Elite).Id;
                Click(room.Fp, "loss-" + elite);
                ArmyGestures.Click(room.Fp.Board.Tray.Q<Button>("loss-" + elite), 2);
                StringAssert.StartsWith("1.", room.Fp.Board.Tray.Q<Button>("loss-" + elite).text);
                StringAssert.Contains("×", room.Fp.Board.Tray.Q<Button>("loss-" + elite).text);
                await room.Confirm(room.Fp);
                Assert.AreEqual("casualties", room.Fp.Board.View.Units.Single(u => u.Id == elite).Location);
                Assert.AreEqual(2, room.Fp.Board.View.Battle.Attacker.Strength);
            }
        }

        private static async Task Retreat()
        {
            using (var room = await Connect("retreat"))
            {
                var camera = room.Sh.Board.Map.Camera.Pan;
                await room.Direct(room.Fp, "action-ContinueBattle");
                Assert.AreEqual(DecisionKind.BattleRetreat, room.Sh.Board.View.Decision.Kind);
                var connected = room.Sh.Connected;
                room.Sh.Connection.Reconnect();
                await room.Until(() => room.Sh.Connected > connected);
                Assert.AreEqual(camera, room.Sh.Board.Map.Camera.Pan, "Continuing and reconnecting must preserve the defender's camera.");
                await ArmyGestures.Target(room, room.Sh, "pelargir");
                // The target helper navigates first, then checks click-to-receipt stability.
                Assert.AreEqual(DecisionKind.BattleAdvance, room.Fp.Board.View.Decision.Kind);
                await room.SelectAllAdvancing(room.Fp);
                Assert.IsNull(room.Fp.Board.View.Battle);
                Assert.AreEqual(Side.FreePeoples, room.Fp.Board.View.Control["lossarnach"]);
                Assert.AreEqual(3, room.Sh.Board.View.Units.Count(u => u.Side == Side.Shadow && u.Location == "pelargir"));
                // UI-305 (OPPONENT_ACTIONS_UX §4, §6.5): the opponent's advance and capture are brought into the defender's
                // view (the battle window covered them), where the camera stays.
                await room.Until(() => !room.Sh.Playback.Holding);
                var map = room.Sh.Board.Map;
                var lossarnach = map.Data.Shapes["lossarnach"].Anchor;
                Assert.IsTrue(map.contentRect.Contains(map.Camera.ToScreen(new Vector2(lossarnach[0], lossarnach[1]))));
            }
        }
    }
}
