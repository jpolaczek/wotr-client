using System;
using System.Collections;
using System.Collections.Generic;
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
    // UI-337 with two real clients on `two-hits`: the Shadow's roll left the Free Peoples two hits, and the Free Peoples
    // answer through the API 800 ms later, as the demo CPU does (DemoBotRunner), removing a Regular and downgrading their
    // Elite in one decision. The Shadow's board plays the two hits one at a time in the battle window, at its Losses stage,
    // and only then the next round's state. Frames go to .artifacts/screenshots (ui337-network-*).
    public sealed class OpponentLossesNetworkTests
    {
        private const int CpuDelayMilliseconds = 800;

        [UnityTest]
        public IEnumerator TheCpusCasualtiesPlayHitByHitBeforeTheNextRound() => Wait(Run());

        private static async Task Run()
        {
            using var room = await Connect("two-hits", openPanels: false);
            var sh = room.Sh;
            var attacker = room.Fp.Board.View.Battle.Attacker;
            var regular = attacker.Units.First(unit => unit.Kind == UnitKind.Regular);
            var elite = attacker.Units.Single(unit => unit.Kind == UnitKind.Elite);
            Assert.IsTrue(sh.Board.Battles.IsOpen, "The Shadow's battle window shows the round.");
            await Task.Delay(CpuDelayMilliseconds);
            await Send(room.Fp, new CommandPayload
            {
                Losses =
                {
                    new BattleLossChoice { UnitId = regular.Id, Kind = BattleLossKind.Remove },
                    new BattleLossChoice { UnitId = elite.Id, Kind = BattleLossKind.Downgrade }
                }
            });
            var struck = new List<string>();
            var stages = new HashSet<string>();
            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (DateTime.UtcNow < deadline && (struck.Count < 2 || sh.Playback.Holding))
            {
                var stack = sh.Board.Battles.Presented.Q(className: BattleHits.HitClass);
                if (stack != null)
                {
                    stages.Add(Stage(sh));
                    if (struck.LastOrDefault() != stack.name)
                    {
                        struck.Add(stack.name);
                        LayoutTests.Save(sh.Viewport, "ui337-network-" + struck.Count + ".png");
                    }
                }
                await Task.Delay(20);
            }
            CollectionAssert.AreEqual(new[] { "battle-figure-" + regular.Nation + "-Regular", "battle-figure-gondor-Elite" }, struck,
                "One hit per loss, in the committed order.");
            CollectionAssert.AreEqual(new[] { "battle-stage-losses" }, stages, "The hits play at the Losses stage.");
            await room.Until(() => !sh.Playback.Holding && sh.Board.View.Revision == room.Fp.Board.View.Revision);
            Assert.AreEqual("battle-stage-next", Stage(sh), "Then the round ends.");
            Assert.IsNull(sh.Board.Battles.Root.Q("battle-figure-gondor-Elite"), "The Elite became a Regular.");
        }

        private static string Stage(MountedMatchClient client) =>
            client.Board.Battles.Ribbon.Root.Q(className: "current-stage")?.name ?? "";

        // The decision as the demo CPU sends it: one command, straight to the server.
        private static async Task Send(MountedMatchClient client, CommandPayload payload)
        {
            var view = client.Board.View;
            var receipt = await client.Api.Post<CommandReceipt>($"api/matches/{view.MatchId}/commands", new GameCommand
            {
                MatchId = view.MatchId, ExpectedRevision = view.Revision, DecisionId = view.Decision.Id,
                CommandId = Guid.NewGuid().ToString("N"), Kind = CommandKind.TakeBattleLosses, Payload = payload
            }, default);
            Assert.IsTrue(receipt.Accepted, receipt.Error);
        }
    }
}
