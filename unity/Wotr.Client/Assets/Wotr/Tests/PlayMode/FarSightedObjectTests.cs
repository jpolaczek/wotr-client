using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    // UI-432 (owner's rule, UI-430): the Fellowship tray's Standard and Far-sighted buttons are gone. On a real server
    // (seeded ui22-separate: Thranduil awakened, the Free Peoples' Ruler die, Legolas separating at Rivendell), Thranduil's
    // Far-sighted component and the plain portrait gesture send the separation the old buttons sent, recorded from the
    // buttons at ability-chips 425411db (a temporary probe). The seed is played once per run, so each path is its own test
    // and run.
    public sealed class FarSightedObjectTests
    {
        private const string Companion = "base.character.legolas";

        [UnityTest] public IEnumerator ThranduilsComponentSendsTheOldFarSightedButtonsSeparation() => Wait(Separate(farSighted: true));
        [UnityTest] public IEnumerator ThePlainGestureSendsTheOldStandardButtonsSeparation() => Wait(Separate(farSighted: false));

        // The separations the old buttons sent at 425411db (the ruler die, Legolas, his stay at Rivendell): Far-sighted's and
        // Standard's differ only in the ability.
        private const string OldFarSighted = Head + "\"kings.far-sighted\"}";
        private const string OldStandard = Head + "\"\"}";
        private const string Head = "{\"Stepwise\":false,\"DieId\":\"kings.die.fp.ruler\",\"RingId\":\"\",\"RulerTokenId\":\"\"," +
            "\"CouncilTokenId\":\"\",\"Deck\":\"\",\"Face\":null,\"Ids\":[\"base.character.legolas\"],\"NationId\":\"\",\"Placements\":[]," +
            "\"MovementCost\":1,\"Moves\":[],\"CardId\":\"\",\"CardCost\":4,\"ReactionId\":\"\",\"GuideId\":\"\",\"Route\":[\"rivendell\"]," +
            "\"CompanionMoves\":[],\"HuntResponse\":0,\"Losses\":[],\"RegionId\":\"\",\"AutoDecision\":null,\"CharacterId\":\"\",\"AbilityId\":";

        private static async Task Separate(bool farSighted)
        {
            using var room = await Connect("ui22-separate", ScenarioKind.CardPack);
            var client = room.Fp;
            var sent = new List<GameCommand>();
            client.Interaction.CommandAnswered += (command, receipt, _) =>
            {
                Assert.IsTrue(receipt.Accepted, receipt.Error);
                sent.Add(command);
            };
            var die = client.Board.View.Dice.Single(d => d.Source == "ruler" && d.Owner == Side.FreePeoples).Id;
            await ComponentGestures.Cost(room, client, die);
            if (farSighted)
                await ComponentGestures.Select(client, "far-sighted-SeparateCompanions");
            FellowshipGestures.Portrait(client, Companion);
            await room.Settle();
            foreach (var name in new[] { "travel-standard", "travel-ruler", "travel-stay" })
                Assert.IsNull(client.Board.Tray.Q<Button>(name), name + " is gone.");
            Assert.AreEqual(farSighted ? "Far-sighted · Range +1" : null, client.Board.Tray.Q<Label>("travel-far-sighted")?.text);
            if (farSighted)
                using (var viewport = new BoardCompletionLayoutTests.LayoutViewport(client, 1366, 1f))
                {
                    client.Board.Fellowship.Open();
                    await room.Settle();
                    LayoutTests.Save(viewport.Texture, "c432-far-sighted-1366-1.00.png");
                }
            var revision = client.Board.View.Revision;
            FellowshipGestures.FinishRoute(client, client.Board.View.Fellowship.RegionId);
            await room.At(revision + 1);
            Assert.AreEqual(CommandKind.SeparateCompanions, sent[0].Kind);
            Assert.AreEqual(farSighted ? OldFarSighted : OldStandard, JsonConvert.SerializeObject(sent[0].Payload),
                "The object sends what the old button sent.");
        }
    }
}
