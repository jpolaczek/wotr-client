using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;
using static Wotr.PlayTests.KingsCardNetworkChecks;

namespace Wotr.PlayTests
{
    public class ExpansionAbilityNetworkTests
    {
        [UnityTest] public IEnumerator BrandChoosesTwoSettlementsAndHisLocalRecruit() => Wait(Recruit(false));
        [UnityTest] public IEnumerator DainChoosesAnEliteAndARecruitInAnotherSettlement() => Wait(Recruit(true));
        [UnityTest] public IEnumerator BalrogActivationUsesHisSpecificSpecialDie() => Wait(Balrog(true));
        [UnityTest] public IEnumerator BalrogDeactivationUsesAnExplicitWillOfTheWest() => Wait(Balrog(false));
        [UnityTest] public IEnumerator GothmogRecruitmentPaysMusterAndAddsARegular() => Wait(Gothmog(false));
        [UnityTest] public IEnumerator GothmogUpgradePaysMusterAndExchangesTheFigures() => Wait(Gothmog(true));
        [UnityTest] public IEnumerator AllCompanionsAndSovereignsCanPlanSeparateGroups() => Wait(Travel(Side.FreePeoples));
        [UnityTest] public IEnumerator NazgulAndAllMobileMinionsCanPlanSeparateGroups() => Wait(Travel(Side.Shadow));

        private static async Task Recruit(bool dain)
        {
            using (var room = await Connect(dain ? "t34-dain" : "t34-brand", ScenarioKind.CardPack))
            {
                var client = room.Fp;
                var id = "kings.sovereign." + (dain ? "dain" : "brand");
                var choice = client.Board.View.Options.Single(o => o.Kind == CommandKind.UseCharacterAbility)
                    .CharacterActions.Single(c => c.CharacterId == id && c.RulerRecruitment != null);
                await ComponentGestures.Cost(room, client, choice.Dice.Single());
                await ComponentGestures.Select(client, ComponentGestures.Character(choice));
                var nation = dain ? "dwarves" : "north";
                var local = choice.RulerRecruitment.LocalRegion;
                var remote = choice.RulerRecruitment.Settlements.First(r => r != local);
                foreach (var placement in dain
                    ? new[] { (local, UnitKind.Elite), (remote, UnitKind.Leader) }
                    : new[] { (local, UnitKind.Regular), (remote, UnitKind.Leader), (local, UnitKind.Regular) })
                {
                    client.Board.Map.Select(placement.Item1);
                    Click(client, "character-reserve-" + nation + "-" + placement.Item2);
                }
                var before = client.Board.View.Units.Count(u => u.Nation == nation && u.Location == "reserve");
                Click(client, "cost-" + choice.Dice.Single());
                await Layout(room, client, dain ? "dain" : "brand", "t34");
                await Reconnect(room, client);
                await room.Confirm(client);
                Assert.AreEqual(before - (dain ? 2 : 3), room.Sh.Board.View.Units.Count(u => u.Nation == nation && u.Location == "reserve"));
                Assert.AreEqual(DieLocation.Spent, room.Sh.Board.View.Dice.Single(d => d.Id == choice.Dice.Single()).Location);
            }
        }

        private static async Task Balrog(bool activate)
        {
            using (var room = await Connect(activate ? "t34-balrog-activate" : "t34-balrog-deactivate", ScenarioKind.CardPack))
            {
                var client = activate ? room.Sh : room.Fp;
                var choice = client.Board.View.Options.Single(o => o.Kind == CommandKind.UseCharacterAbility)
                    .CharacterActions.Single(c => c.AbilityId == (activate ? "activate-balrog" : "deactivate-balrog"));
                await Ability(room, client, choice, activate ? "balrog-activate" : "balrog-deactivate");
                Assert.AreEqual(activate ? 2 : 0, room.Fp.Board.View.Characters.Single(c => c.Id == choice.CharacterId).Level);
            }
        }

        private static async Task Gothmog(bool upgrade)
        {
            using (var room = await Connect(upgrade ? "t34-gothmog-upgrade" : "t34-gothmog-recruit", ScenarioKind.CardPack))
            {
                var client = room.Sh;
                var before = client.Board.View.Units.Count(u => u.Location == "minas-morgul" && u.Kind == UnitKind.Regular);
                var choice = client.Board.View.Options.Single(o => o.Kind == CommandKind.UseCharacterAbility)
                    .CharacterActions.Single(c => c.AbilityId == (upgrade ? "gothmog-upgrade" : "gothmog-recruit"));
                await Ability(room, client, choice, upgrade ? "gothmog-upgrade" : "gothmog-recruit");
                Assert.AreEqual(before + (upgrade ? -1 : 1), room.Fp.Board.View.Units.Count(u => u.Location == "minas-morgul" && u.Kind == UnitKind.Regular));
            }
        }

        private static async Task Ability(MatchTestRoom room, MountedMatchClient client, CharacterActionOption choice, string name)
        {
            await ComponentGestures.Cost(room, client, choice.Dice[0]);
            await ComponentGestures.Show(client, ComponentGestures.Character(choice));
            await Layout(room, client, name, "t34");
            await Reconnect(room, client);
            var revision = client.Board.View.Revision;
            await ComponentGestures.Select(client, ComponentGestures.Character(choice));
            await room.At(revision + 1);
            Assert.AreEqual(DieLocation.Spent, room.Fp.Board.View.Dice.Single(d => d.Id == choice.Dice[0]).Location);
        }

        private static async Task Travel(Side side)
        {
            using (var room = await Connect(side == Side.FreePeoples ? "t34-travel-fp" : "t34-travel-sh", ScenarioKind.CardPack))
            {
                var client = side == Side.FreePeoples ? room.Fp : room.Sh;
                var kind = side == Side.FreePeoples ? CommandKind.MoveCompanions : CommandKind.MoveShadowCharacters;
                var option = client.Board.View.Options.Single(o => o.Kind == kind);
                Assert.GreaterOrEqual(option.EligibleIds.Count, 12);
                await TravelGroups(room, client, option);
            }
        }
        private static async Task TravelGroups(MatchTestRoom room, MountedMatchClient client, ActionOption initial)
        {
            var fp = client.Board.View.You == Side.FreePeoples;
            var die = fp ? initial.RulerTravel.Dice.Single() : initial.EligibleDice.First(id => client.Board.View.Dice.Any(d => d.Id == id && d.Face == DieFace.Character));
            await ComponentGestures.Cost(room, client, die);
            if (fp)
                await ComponentGestures.Select(client, "far-sighted-MoveCompanions");
            for (var index = 0; index < initial.EligibleIds.Count; index++)
            {
                var id = initial.EligibleIds[index];
                var origin = initial.Travel.Single(t => t.Id == id).Origin;
                FellowshipGestures.Figure(client, origin, id);
                await room.Settle();
                var revision = client.Board.View.Revision;
                FellowshipGestures.FinishRoute(client, origin);
                await room.At(revision + 1);
                if (index < initial.EligibleIds.Count - 1)
                {
                    Assert.AreEqual(initial.EligibleIds.Count - index - 1, client.Board.View.ActionSequence.Remaining);
                    Assert.AreEqual(die, client.Board.View.ActionSequence.DieId);
                }
                if (index == 0)
                    await Reconnect(room, client);
            }
            Assert.AreEqual(DieLocation.Spent, room.Fp.Board.View.Dice.Single(d => d.Id == die).Location);
        }
    }
}
