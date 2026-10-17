using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public class BattlePolicyNetworkTests
    {
        [UnityTest]
        public IEnumerator FreePeoplesAttackAllowsPrivateOptInReconnectAndCancellation() => Wait(Policy("policy-fp", true));

        [UnityTest]
        public IEnumerator ShadowAttackAllowsPrivateOptInReconnectAndCancellation() => Wait(Policy("policy-sh", false));

        private static async Task Policy(string scenario, bool fpAttacks)
        {
            using (var room = await Connect(scenario))
            {
                var attacker = fpAttacks ? room.Fp : room.Sh;
                var defender = fpAttacks ? room.Sh : room.Fp;
                var revision = attacker.Board.View.Revision;
                var decision = attacker.Board.View.Decision.Id;
                await Toggle(room, defender, 1);
                Assert.AreEqual(revision, defender.Board.View.Revision);
                Assert.AreEqual(decision, attacker.Board.View.Decision.Id);
                Assert.AreEqual(0, attacker.Board.View.AutoDecision.Version);
                Assert.AreEqual("Ask for cards", defender.Board.BattlePolicy.Toggle.text);
                var connections = defender.Connected;
                defender.Connection.Reconnect();
                await room.Until(() => defender.Connected > connections);
                await room.Settle();
                Assert.NotNull(defender.Board.View.AutoDecision.Policy);
                await Toggle(room, defender, 2);
                Assert.IsNull(defender.Board.View.AutoDecision.Policy);
                Assert.AreEqual(revision, defender.Board.View.Revision);

                await Toggle(room, attacker, 1);
                await room.At(++revision);
                Assert.AreEqual(DecisionKind.CombatCard, defender.Board.View.Decision.Kind);
                Assert.IsNull(attacker.Board.View.Decision);
                await Toggle(room, attacker, 2);
                Assert.AreEqual(revision, attacker.Board.View.Revision);
                Assert.IsTrue(attacker.Board.View.Battle.Attacker.CardCommitted);
                StringAssert.Contains("Round 1 kept", attacker.Board.BattlePolicy.Root.Q<Label>("battle-policy-status").text);

                await Toggle(room, defender, 3);
                await room.At(++revision);
                Assert.AreEqual(1, defender.Board.View.AutoDecision.LastSkippedRound);
                Assert.IsFalse(room.Actor.Board.View.Decision.Kind == DecisionKind.CombatCard);
                var nextDecision = room.Actor.Board.View.Decision.Id;
                await Toggle(room, defender, 4);
                Assert.AreEqual(revision, defender.Board.View.Revision);
                Assert.AreEqual(nextDecision, room.Actor.Board.View.Decision.Id);
                Assert.IsNull(defender.Board.View.AutoDecision.Policy);
            }
        }

        private static async Task Toggle(MatchTestRoom room, MountedMatchClient client, int version)
        {
            await room.Until(() => client.Board.BattlePolicy.Toggle.enabledSelf);
            CardNetworkTests.Click(client.Board.BattlePolicy.Toggle);
            await room.Until(() => client.Board.View.AutoDecision.Version == version);
            await room.Settle();
        }
    }
}
