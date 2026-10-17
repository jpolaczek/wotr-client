using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    internal static class T22NetworkActions
    {
        // UI-172: the Shadow's own Hunt allocation is confirmed with the Hunt plaque, other decisions in the tray.
        public static Task Ready(MatchTestRoom room, MountedMatchClient client) => room.Until(() => OwnAllocation(client)
            ? PlaqueReady(client)
            : client.Board.Tray.Q<Button>("confirm-decision")?.enabledInHierarchy == true);

        public static bool PlaqueReady(MountedMatchClient client) => client.Board.Hunt.Tab.ClassListContains("hunt-confirm-ready");

        public static bool OwnAllocation(MountedMatchClient client) =>
            client.Board.View?.Decision?.Kind == DecisionKind.AllocateHunt && client.Board.View.DecisionOwner == client.Board.View.You;

        public static void Die(MountedMatchClient client, string id) =>
            CardNetworkTests.Click(client.Board.OwnDice.Q<Button>(id));

        public static async Task Region(MatchTestRoom room, MountedMatchClient client, string region)
        {
            await room.Settle();
            MapContourProbe.ClickRegion(client.Board.Map, region);
            await room.Settle();
        }

        public static void SameDice(MatchTestRoom room) => Assert.AreEqual(
            JsonConvert.SerializeObject(room.Fp.Board.View.Dice), JsonConvert.SerializeObject(room.Sh.Board.View.Dice));

        public static void Spent(MatchTestRoom room, Side side, string id, int count = 1)
        {
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                Assert.AreEqual(DieLocation.Spent, client.Board.View.Dice.Single(d => d.Id == id).Location);
                Assert.AreEqual(count, client.Board.View.Dice.Count(d => d.Owner == side && d.Location == DieLocation.Spent));
            }
            SameDice(room);
        }

        public static async Task Reject(MatchTestRoom room, MountedMatchClient client, CommandKind kind, CommandPayload payload)
        {
            var view = client.Board.View;
            var path = "api/matches/" + view.MatchId;
            var before = await client.Api.Get<PlayerView>(path, room.Token);
            var command = Command(view, kind, payload);
            var preview = await client.Api.Post<ActionPreview>(path + "/preview", command, room.Token);
            Assert.IsFalse(preview.Legal);
            var receipt = await client.Api.Post<CommandReceipt>(path + "/commands", command, room.Token);
            Assert.IsFalse(receipt.Accepted);
            Assert.AreEqual(view.Revision, receipt.Revision);
            var after = await client.Api.Get<PlayerView>(path, room.Token);
            Assert.AreEqual(JsonConvert.SerializeObject(before), JsonConvert.SerializeObject(after), "Rejected choice must not mutate the authoritative projection.");
            Assert.AreEqual(view.Revision, room.Fp.Board.View.Revision);
            Assert.AreEqual(view.Revision, room.Sh.Board.View.Revision);
        }

        public static GameCommand Command(PlayerView view, CommandKind kind, CommandPayload payload) => new GameCommand
        {
            MatchId = view.MatchId,
            CommandId = Guid.NewGuid().ToString("N"),
            ExpectedRevision = view.Revision,
            DecisionId = view.Decision.Id,
            Kind = kind,
            Payload = payload
        };
    }
}
