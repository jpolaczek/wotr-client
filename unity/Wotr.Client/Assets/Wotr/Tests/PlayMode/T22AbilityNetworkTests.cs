using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class T22AbilityNetworkTests
    {
        [UnityTest]
        public IEnumerator GimliAdvancesDwarvesWithTheChosenArmyDie() => Wait(Gimli());

        [UnityTest]
        public IEnumerator SarumanRecruitsAtExplicitSettlementsWithFullAndScarceReserves() => Wait(Recruit());

        [UnityTest]
        public IEnumerator SarumanUpgradeUsesTwoPairsOrTheOnlyAvailablePair() => Wait(Upgrade());

        private static async Task Gimli()
        {
            using (var room = await Connect("ability-gimli", ScenarioKind.BaseAcceptance))
            {
                const string character = "base.character.gimli";
                const string ability = "politics-dwarves-at-erebor";
                var die = room.Fp.Board.View.Dice.Single(d => d.Owner == Side.FreePeoples && d.Face == DieFace.Army).Id;
                await T22NetworkActions.Reject(room, room.Fp, CommandKind.UseCharacterAbility,
                    Payload(character, ability, die, new[] { "lorien" }));
                Assert.AreEqual(2, room.Sh.Board.View.Nations.Single(n => n.Id == "dwarves").StepsToWar);
                Assert.AreEqual(DieLocation.Available, room.Sh.Board.View.Dice.Single(d => d.Id == die).Location);
                await Choose(room, room.Fp, character, ability, die);
                foreach (var client in new[] { room.Fp, room.Sh })
                {
                    Assert.AreEqual(1, client.Board.View.Nations.Single(n => n.Id == "dwarves").StepsToWar);
                    Assert.AreEqual("erebor", client.Board.View.Characters.Single(c => c.Id == character).Location);
                    Assert.AreEqual(DieFace.Army, client.Board.View.Dice.Single(d => d.Id == die).Face);
                }
                T22NetworkActions.Spent(room, Side.FreePeoples, die);
            }
        }

        private static async Task Recruit()
        {
            foreach (var scarce in new[] { false, true })
                using (var room = await Connect("ability-voice-recruit-" + (scarce ? "scarce" : "full"), ScenarioKind.BaseAcceptance))
                {
                    var before = room.Sh.Board.View;
                    var choice = before.Options.Single(o => o.Kind == CommandKind.UseCharacterAbility)
                        .CharacterActions.Single(c => c.AbilityId == "voice-recruit");
                    var targets = scarce ? new[] { choice.Targets.Last() } : choice.Targets.ToArray();
                    Assert.AreEqual(scarce ? 1 : choice.Targets.Count, choice.TargetCount);
                    Assert.Greater(choice.Targets.Count, 1, "Scarcity must leave a strategic settlement choice.");
                    var die = before.Dice.Single(d => d.Owner == Side.Shadow && d.Face == DieFace.Muster).Id;
                    await T22NetworkActions.Reject(room, room.Sh, CommandKind.UseCharacterAbility,
                        Payload(choice.CharacterId, choice.AbilityId, die, new[] { "minas-tirith" }));
                    Assert.AreEqual(JsonConvert.SerializeObject(before.Units), JsonConvert.SerializeObject(room.Fp.Board.View.Units));
                    await Choose(room, room.Sh, choice.CharacterId, choice.AbilityId, die);
                    if (scarce)
                    {
                        await Reconnect(room, room.Sh);
                        room.Sh.Board.Map.Select("minas-tirith");
                        await room.Settle();
                        Assert.AreEqual(before.Revision, room.Sh.Board.View.Revision);
                        await T22NetworkActions.Region(room, room.Sh, targets[0]);
                        await room.At(before.Revision + 1);
                    }
                    AssertRecruitment(room, before, choice.Targets, targets);
                    T22NetworkActions.Spent(room, Side.Shadow, die);
                }
        }

        private static void AssertRecruitment(MatchTestRoom room, PlayerView before, IEnumerable<string> candidates, string[] targets)
        {
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                foreach (var region in candidates)
                {
                    var expected = Regulars(before, region) + (targets.Contains(region) ? 1 : 0);
                    Assert.AreEqual(expected, Regulars(client.Board.View, region), region);
                }
                Assert.AreEqual(Regulars(before, "reserve") - targets.Length, Regulars(client.Board.View, "reserve"));
                Assert.AreEqual(Side.FreePeoples, client.Board.View.DecisionOwner);
            }
        }

        private static async Task Upgrade()
        {
            foreach (var scarce in new[] { false, true })
                using (var room = await Connect("ability-voice-upgrade-" + (scarce ? "scarce" : "full"), ScenarioKind.BaseAcceptance))
                {
                    const string character = "base.character.saruman";
                    const string ability = "voice-upgrade";
                    var before = room.Sh.Board.View;
                    var pairs = scarce ? 1 : 2;
                    var die = before.Dice.Single(d => d.Owner == Side.Shadow && d.Face == DieFace.Muster).Id;
                    var regulars = before.Units.Where(u => u.Nation == "isengard" && u.Kind == UnitKind.Regular && u.Location == "orthanc").Select(u => u.Id).ToArray();
                    var command = T22NetworkActions.Command(before, CommandKind.UseCharacterAbility,
                        Payload(character, ability, die, new[] { "orthanc" }));
                    var preview = await room.Sh.Api.Post<ActionPreview>("api/matches/" + before.MatchId + "/preview", command, room.Token);
                    Assert.IsTrue(preview.Legal);
                    Assert.AreEqual(pairs, preview.RemovedIds.Count);
                    Assert.AreEqual(pairs, preview.Placements.Count);
                    Assert.IsTrue(preview.RemovedIds.All(regulars.Contains));
                    Assert.AreEqual(JsonConvert.SerializeObject(before.Units), JsonConvert.SerializeObject(room.Fp.Board.View.Units));
                    await Choose(room, room.Sh, character, ability, die);
                    foreach (var client in new[] { room.Fp, room.Sh })
                    {
                        Assert.AreEqual(2 - pairs, Regulars(client.Board.View, "orthanc"));
                        Assert.AreEqual(pairs, client.Board.View.Units.Count(u => regulars.Contains(u.Id) && u.Location == "reserve"));
                        Assert.AreEqual(pairs, client.Board.View.Units.Count(u => u.Nation == "isengard" && u.Kind == UnitKind.Elite && u.Location == "orthanc"));
                    }
                    T22NetworkActions.Spent(room, Side.Shadow, die);
                }
        }

        private static async Task Choose(MatchTestRoom room, MountedMatchClient client, string character, string ability, string die)
        {
            await ComponentGestures.Cost(room, client, die);
            Assert.IsTrue(client.Board.OwnDice.Q<Button>(die).ClassListContains("selected"));
            var choice = client.Board.View.Options.Single(o => o.Kind == CommandKind.UseCharacterAbility)
                .CharacterActions.Single(c => c.CharacterId == character && c.AbilityId == ability);
            var revision = client.Board.View.Revision;
            await ComponentGestures.Select(client, ComponentGestures.Character(choice));
            if (choice.Targets.Count == choice.TargetCount)
                await room.At(revision + 1);
            else
                await room.Settle();
        }

        private static async Task Reconnect(MatchTestRoom room, MountedMatchClient client)
        {
            var previous = client.Connected;
            client.Connection.Reconnect();
            await room.Until(() => client.Connected > previous);
            await room.Settle();
        }

        private static CommandPayload Payload(string character, string ability, string die, IEnumerable<string> targets) => new CommandPayload
        {
            CharacterId = character,
            AbilityId = ability,
            DieId = die,
            Ids = targets.ToList()
        };

        private static int Regulars(PlayerView view, string region) => view.Units
            .Count(u => u.Nation == "isengard" && u.Kind == UnitKind.Regular && u.Location == region);
    }
}
