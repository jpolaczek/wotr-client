using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class T22BoundaryNetworkTests
    {
        [UnityTest]
        public IEnumerator RandomCompanionPreviewKeepsTheVictimUnknownAndReconnectPreservesTheDraw() => Wait(RandomCompanion());

        [UnityTest]
        public IEnumerator StrongholdDefenderChoosesFieldCombatWithoutBecomingAGarrison() => Wait(FieldDefense());

        [UnityTest]
        public IEnumerator FreePeoplesMilitaryVictoryWaitsForTheLastAction() =>
            Wait(Military("boundary-military-fp", Side.FreePeoples, 4, 0));

        [UnityTest]
        public IEnumerator ShadowMilitaryVictoryWaitsForTheLastAction() =>
            Wait(Military("boundary-military-sh", Side.Shadow, 0, 10));

        [UnityTest]
        public IEnumerator SimultaneousMilitaryThresholdsGiveShadowPriority() =>
            Wait(Military("boundary-military-priority", Side.Shadow, 4, 10));

        private static async Task RandomCompanion()
        {
            using (var room = await Connect("boundary-random-companion", ScenarioKind.BaseAcceptance))
            {
                var before = room.Fp.Board.View;
                Assert.AreEqual(DecisionKind.HuntDamage, before.Decision.Kind);
                Assert.AreEqual(3, before.Hunt.Damage);
                var companions = before.OwnCompanions.ToArray();
                Assert.AreEqual(3, companions.Length);
                Assert.IsTrue(companions.All(c => c.Level == 2));
                var paidDie = before.Dice.Single(d => d.Location == DieLocation.Resolving).Id;
                Assert.IsNull(room.Sh.Board.View.Decision);
                Assert.IsEmpty(room.Sh.Board.View.Options);

                await AssertUnknownPreview(room, before);
                await Reconnect(room);
                Assert.AreEqual(before.Revision, room.Fp.Board.View.Revision);
                CollectionAssert.AreEquivalent(companions.Select(c => c.Id), room.Fp.Board.View.OwnCompanions.Select(c => c.Id));
                Assert.AreEqual(DieLocation.Resolving, Die(room.Fp, paidDie).Location);
                Click(room.Fp, "hunt-response-RandomCompanion");
                await room.At(before.Revision + 1);
                var remaining = room.Fp.Board.View.OwnCompanions.Select(c => c.Id).ToArray();
                var victim = companions.Single(c => !remaining.Contains(c.Id));
                Assert.AreEqual(companions.Length - 1, remaining.Length);
                Assert.AreEqual(DecisionKind.Guide, room.Fp.Board.View.Decision.Kind);
                Assert.AreEqual(2, room.Fp.Board.View.Fellowship.Corruption, "Corruption waits for the Guide decision.");
                Assert.AreEqual(2, room.Fp.Board.View.Fellowship.MordorStep);
                Assert.AreEqual(DieLocation.Resolving, Die(room.Fp, paidDie).Location);
                AssertCasualty(room.Fp, victim.Id);
                AssertCasualty(room.Sh, victim.Id);

                var committed = JsonConvert.SerializeObject(room.Fp.Board.View);
                await Reconnect(room);
                Assert.AreEqual(committed, JsonConvert.SerializeObject(room.Fp.Board.View),
                    "Reconnect must retain the committed casualty and pending Guide, without another draw.");
                var guides = room.Fp.Board.View.Options.Single(o => o.Kind == CommandKind.SelectGuide).EligibleIds;
                CollectionAssert.AreEquivalent(remaining, guides);
                var guide = guides.Contains(before.Fellowship.Guide) ? before.Fellowship.Guide : guides[0];
                Click(room.Fp, "companion-" + guide);
                await room.Confirm(room.Fp);
                Assert.AreEqual(guide, room.Fp.Board.View.Fellowship.Guide);
                Assert.AreEqual(3, room.Fp.Board.View.Fellowship.Corruption);
                Assert.AreEqual(3, room.Fp.Board.View.Fellowship.MordorStep);
                Assert.IsNull(room.Fp.Board.View.Hunt);
                Assert.AreEqual(DieLocation.Hunt, Die(room.Fp, paidDie).Location);
                Assert.AreEqual(1, room.Fp.Board.View.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Hunt));
                Assert.AreEqual(Side.Shadow, room.Fp.Board.View.DecisionOwner);
                AssertCasualty(room.Fp, victim.Id);
            }
        }

        private static async Task AssertUnknownPreview(MatchTestRoom room, PlayerView before)
        {
            var command = new GameCommand
            {
                MatchId = before.MatchId,
                CommandId = Guid.NewGuid().ToString("N"),
                ExpectedRevision = before.Revision,
                DecisionId = before.Decision.Id,
                Kind = CommandKind.ResolveHuntDamage,
                Payload = new CommandPayload { HuntResponse = HuntResponse.RandomCompanion }
            };
            var raw = await room.Fp.Api.Post<JObject>("api/matches/" + before.MatchId + "/preview", command, room.Token);
            var preview = raw.ToObject<ActionPreview>();
            Assert.IsTrue(preview.Legal, preview.Error);
            Assert.IsTrue(preview.HasUnknownResult);
            Assert.IsEmpty(preview.RemovedIds);
            Assert.IsEmpty(preview.Costs, "The casualty choice must not charge a second action die.");
            foreach (var companion in before.OwnCompanions)
                StringAssert.DoesNotContain(companion.Id, raw.ToString());
            var unchanged = await room.Fp.Api.Get<PlayerView>("api/matches/" + before.MatchId, room.Token);
            Assert.AreEqual(JsonConvert.SerializeObject(before), JsonConvert.SerializeObject(unchanged));
        }

        private static async Task FieldDefense()
        {
            using (var room = await Connect("boundary-field-defense", ScenarioKind.BaseAcceptance))
            {
                await room.Attack(room.Sh, "osgiliath", "minas-tirith");
                Assert.AreEqual(DecisionKind.StrongholdDefense, room.Fp.Board.View.Decision.Kind);
                Assert.IsNotNull(room.Fp.Board.Tray.Q<Button>("action-FightFieldBattle"));
                Assert.IsNotNull(room.Fp.Board.Tray.Q<Button>("action-RetreatIntoSiege"));
                var paidDie = room.Sh.Board.View.Dice.Single(d => d.Location == DieLocation.Resolving).Id;
                await room.Direct(room.Fp, "action-FightFieldBattle");
                var battle = room.Sh.Board.View.Battle;
                Assert.AreEqual(BattleMode.Field, battle.Mode);
                Assert.IsFalse(battle.Defender.Garrison);
                Assert.IsFalse(battle.DefenderSheltered);
                Assert.AreEqual(5, battle.Attacker.HitTarget, "Field defense at a stronghold is not a siege assault.");
                Assert.AreEqual(7, battle.Defender.Units.Count);
                Assert.AreEqual(Side.FreePeoples, room.Fp.Board.View.Control["minas-tirith"]);
                Assert.AreEqual(DieLocation.Resolving, Die(room.Sh, paidDie).Location);
                await room.NoCard(room.Sh);
                Assert.AreNotEqual(DecisionKind.CombatCard, room.Sh.Board.View.Decision?.Kind);
                await DefenderNoCard(room);
                await ResolveFieldRound(room);
                Assert.AreEqual(DecisionKind.BattleContinue, room.Sh.Board.View.Decision.Kind);
                await room.Direct(room.Sh, "action-StopBattle");
                Assert.IsNull(room.Fp.Board.View.Battle);
                Assert.AreEqual(DieLocation.Spent, Die(room.Sh, paidDie).Location);
                Assert.AreEqual(1, room.Sh.Board.View.Dice.Count(d => d.Owner == Side.Shadow && d.Location == DieLocation.Spent));
                Assert.IsFalse(room.Fp.Board.View.Units.Any(u => u.Location == "minas-tirith" && u.Garrison));
                Assert.AreEqual(Side.FreePeoples, room.Fp.Board.View.Control["minas-tirith"]);
                Assert.AreEqual(BattleMode.Field, room.Fp.Board.View.LastBattleRound.Mode);
                Assert.AreEqual(5, room.Fp.Board.View.LastBattleRound.Attacker.Dice.Count);
                Assert.AreEqual(5, room.Fp.Board.View.LastBattleRound.Defender.Dice.Count);
            }
        }

        private static async Task ResolveFieldRound(MatchTestRoom room)
        {
            for (var step = 0; step < 5; step++)
            {
                var client = room.Actor;
                var view = client.Board.View;
                if (view.Decision.Kind == DecisionKind.BattleContinue)
                    return;
                if (view.Decision.Kind == DecisionKind.BattleReroll)
                {
                    await room.Confirm(client);
                    continue;
                }
                Assert.AreEqual(DecisionKind.Losses, view.Decision.Kind);
                var option = view.Options.Single(o => o.Kind == CommandKind.TakeBattleLosses);
                var losses = option.EligibleIds.Take(view.Battle.PendingHits).ToArray();
                Assert.AreEqual(view.Battle.PendingHits, losses.Length);
                foreach (var id in losses)
                {
                    Assert.AreEqual(UnitKind.Regular, view.Units.Single(u => u.Id == id).Kind);
                    Click(client, "loss-" + id);
                }
                await room.Confirm(client);
            }
            Assert.Fail("One field round did not reach its continuation decision.");
        }

        private static async Task DefenderNoCard(MatchTestRoom room)
        {
            await room.Settle();
            var client = room.Fp;
            var option = client.Board.View.Options.FirstOrDefault(o => o.Kind == CommandKind.CommitCombatCard);
            if (client.Board.View.Decision?.Kind != DecisionKind.CombatCard || option?.EligibleIds.Count == 0)
            {
                // UI-151: without a legal Combat card the defender's No card goes without a click.
                await room.NoCard(client);
                return;
            }
            var revision = client.Board.View.Revision;
            var button = client.Board.Tray.Q<Button>("battle-no-card");
            LayoutTests.Save(client.Viewport, "t22-field-defender.png");
            Assert.IsTrue(button.enabledInHierarchy);
            Assert.IsTrue(client.Board.Root.worldBound.Contains(button.worldBound.center), button.worldBound.ToString());
            Assert.AreEqual("Live", client.Board.Connection.text);
            var clicked = false;
            button.clicked += () => clicked = true;
            CardNetworkTests.Click(button);
            Assert.IsTrue(clicked, "The visible No card button must receive the pointer click.");
            await room.At(revision + 1);
        }

        private static async Task Military(string scenario, Side winner, int fpPoints, int shPoints)
        {
            using (var room = await Connect(scenario, ScenarioKind.BaseAcceptance))
            {
                AssertPoints(room.Fp, fpPoints, shPoints);
                Assert.IsNull(room.Fp.Board.View.Winner);
                Assert.AreEqual(GamePhase.ActionResolution, room.Fp.Board.View.Phase);
                Assert.AreEqual(DisplayStyle.None, room.Fp.Board.Victory.Banner.style.display.value);
                await DiscardLastDie(room, room.Fp);
                Assert.IsNull(room.Fp.Board.View.Winner, "Military thresholds do not end the action phase early.");
                Assert.IsNull(room.Sh.Board.View.Winner);
                Assert.AreEqual(GamePhase.ActionResolution, room.Fp.Board.View.Phase);
                await DiscardLastDie(room, room.Sh);
                AssertFinished(room.Fp, winner, fpPoints, shPoints);
                AssertFinished(room.Sh, winner, fpPoints, shPoints);
                var revision = room.Fp.Board.View.Revision;
                var history = JsonConvert.SerializeObject(room.Fp.Board.View.History);
                await Reconnect(room);
                Assert.AreEqual(revision, room.Fp.Board.View.Revision);
                Assert.AreEqual(history, JsonConvert.SerializeObject(room.Fp.Board.View.History));
                AssertFinished(room.Fp, winner, fpPoints, shPoints);
                AssertFinished(room.Sh, winner, fpPoints, shPoints);
            }
        }

        private static async Task DiscardLastDie(MatchTestRoom room, MountedMatchClient client)
        {
            var die = client.Board.View.Dice.Single(d => d.Owner == client.Board.View.You && d.Location == DieLocation.Available).Id;
            await ComponentGestures.Cost(room, client, die);
            var revision = client.Board.View.Revision;
            await ComponentGestures.Select(client, "DiscardDie");
            await room.At(revision + 1);
            Assert.AreEqual(DieLocation.Discarded, Die(client, die).Location);
        }

        private static void AssertFinished(MountedMatchClient client, Side winner, int fpPoints, int shPoints)
        {
            var view = client.Board.View;
            var reason = winner == Side.Shadow ? VictoryCondition.ShadowMilitary : VictoryCondition.FreePeoplesMilitary;
            AssertPoints(client, fpPoints, shPoints);
            Assert.AreEqual(GamePhase.Finished, view.Phase);
            Assert.AreEqual(winner, view.Winner);
            Assert.AreEqual(reason, view.Victory);
            Assert.IsNull(view.Decision);
            Assert.IsNull(view.DecisionOwner);
            Assert.IsEmpty(view.Options);
            Assert.IsFalse(view.Dice.Any(d => d.Location == DieLocation.Available || d.Location == DieLocation.Resolving));
            Assert.AreEqual(1, view.History.Count(e => e.Kind == reason.ToString()));
            StringAssert.Contains("Military victory", client.Board.Victory.Banner.text);
            Assert.IsNull(client.Board.Tray.Q<Button>("confirm-decision"));
            Assert.IsNotNull(client.Board.Map.panel);
        }

        private static void AssertPoints(MountedMatchClient client, int fp, int sh)
        {
            Assert.AreEqual(fp, client.Board.View.Players.Single(p => p.Side == Side.FreePeoples).VictoryPoints);
            Assert.AreEqual(sh, client.Board.View.Players.Single(p => p.Side == Side.Shadow).VictoryPoints);
        }

        private static void AssertCasualty(MountedMatchClient client, string victim)
        {
            var events = client.Board.View.History.Where(e => e.Kind == "HuntCasualty").ToArray();
            Assert.AreEqual(1, events.Length);
            StringAssert.Contains(victim, events[0].Text);
        }

        private static DieView Die(MountedMatchClient client, string id) => client.Board.View.Dice.Single(d => d.Id == id);

        private static Task Ready(MatchTestRoom room, MountedMatchClient client) =>
            room.Until(() => client.Board.Tray.Q<Button>("confirm-decision")?.enabledSelf == true);

        private static async Task Reconnect(MatchTestRoom room)
        {
            var fp = room.Fp.Connected;
            var sh = room.Sh.Connected;
            room.Fp.Connection.Reconnect();
            room.Sh.Connection.Reconnect();
            await room.Until(() => room.Fp.Connected > fp && room.Sh.Connected > sh);
            await room.Settle();
        }
    }
}
