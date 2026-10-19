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
                Assert.IsNull(client.Board.Tray.Q<Button>("cost-" + choice.Dice.Single()), "UI-239: the only die is taken for the player.");
                await Layout(room, client, dain ? "dain" : "brand", "t34");
                await Reconnect(room, client);
                await room.Confirm(client);
                Assert.AreEqual(before - (dain ? 2 : 3), room.Sh.Board.View.Units.Count(u => u.Nation == nation && u.Location == "reserve"));
                Assert.AreEqual(DieLocation.Spent, room.Sh.Board.View.Dice.Single(d => d.Id == choice.Dice.Single()).Location);
            }
        }

        // UI-271: with the paying die selected the Balrog's figure glows in his region's panel, says what its click does,
        // and its click (de)activates him at once; the panel lists no ability button.
        private static async Task Balrog(bool activate)
        {
            using (var room = await Connect(activate ? "t34-balrog-activate" : "t34-balrog-deactivate", ScenarioKind.CardPack))
            {
                var client = activate ? room.Sh : room.Fp;
                var choice = client.Board.View.Options.Single(o => o.Kind == CommandKind.UseCharacterAbility)
                    .CharacterActions.Single(c => c.AbilityId == (activate ? "activate-balrog" : "deactivate-balrog"));
                var region = client.Board.View.Characters.Single(c => c.Id == choice.CharacterId).Location;
                await ComponentGestures.Cost(room, client, choice.Dice[0]);
                var figure = await BalrogFigure(room, client, choice, region, activate);
                await Layout(room, client, activate ? "balrog-activate" : "balrog-deactivate", "ui271");
                await Reconnect(room, client);
                figure = await BalrogFigure(room, client, choice, region, activate);
                var revision = client.Board.View.Revision;
                CardNetworkTests.Click(figure);
                await room.At(revision + 1);
                Assert.AreEqual(DieLocation.Spent, room.Fp.Board.View.Dice.Single(d => d.Id == choice.Dice[0]).Location);
                Assert.AreEqual(activate ? 2 : 0, room.Fp.Board.View.Characters.Single(c => c.Id == choice.CharacterId).Level);
            }
        }

        private static async Task<Button> BalrogFigure(MatchTestRoom room, MountedMatchClient client, CharacterActionOption choice,
            string region, bool activate)
        {
            client.Board.Hover.Open(region);
            await room.Settle();
            var panel = client.Board.Hover.Panel;
            Assert.IsNull(panel.Components.Root.Q<Button>("component-" + ComponentGestures.Character(choice)),
                "UI-271: the Balrog's ability is used on his figure, not a button.");
            var figure = panel.Root.Q<Button>("hover-figure-" + choice.CharacterId);
            Assert.IsTrue(figure.ClassListContains("eligible-figure"));
            StringAssert.Contains(activate ? "Click to activate the Balrog" : "Click to deactivate the Balrog", figure.tooltip);
            return figure;
        }

        // UI-271: with a Muster die selected Gothmog's row in the Reserve panel offers a Regular (recruit) and an Elite
        // (upgrade); the token's click uses the ability at once.
        private static async Task Gothmog(bool upgrade)
        {
            using (var room = await Connect(upgrade ? "t34-gothmog-upgrade" : "t34-gothmog-recruit", ScenarioKind.CardPack))
            {
                var client = room.Sh;
                var before = client.Board.View.Units.Count(u => u.Location == "minas-morgul" && u.Kind == UnitKind.Regular);
                var choice = client.Board.View.Options.Single(o => o.Kind == CommandKind.UseCharacterAbility)
                    .CharacterActions.Single(c => c.AbilityId == (upgrade ? "gothmog-upgrade" : "gothmog-recruit"));
                await ComponentGestures.Cost(room, client, choice.Dice[0]);
                var token = await GothmogToken(room, client, upgrade);
                client.Board.Hover.Open("minas-morgul");
                await room.Settle();
                Assert.IsNull(client.Board.Hover.Panel.Components.Root.Q<Button>("component-" + ComponentGestures.Character(choice)),
                    "UI-271: Gothmog's ability stands in the Reserve panel, not the region panel.");
                await Layout(room, client, upgrade ? "gothmog-upgrade" : "gothmog-recruit", "ui271");
                await Reconnect(room, client);
                token = await GothmogToken(room, client, upgrade);
                var revision = client.Board.View.Revision;
                CardNetworkTests.Click(token);
                await room.At(revision + 1);
                Assert.AreEqual(DieLocation.Spent, room.Fp.Board.View.Dice.Single(d => d.Id == choice.Dice[0]).Location);
                Assert.AreEqual(before + (upgrade ? -1 : 1), room.Fp.Board.View.Units.Count(u => u.Location == "minas-morgul" && u.Kind == UnitKind.Regular));
            }
        }

        private static async Task<Button> GothmogToken(MatchTestRoom room, MountedMatchClient client, bool upgrade)
        {
            client.Board.Reinforcements.Open();
            await room.Settle();
            var row = client.Board.Reinforcements.Root.Q("reinforcement-gothmog");
            Assert.IsNotNull(row, "Gothmog's row stands in the Reserve panel.");
            StringAssert.StartsWith("Gothmog · Minas Morgul", row.Q<Label>("reinforcement-name").text);
            var token = row.Q<Button>("reserve-token-gothmog-" + (upgrade ? UnitKind.Elite : UnitKind.Regular));
            Assert.IsTrue(token.enabledSelf && token.ClassListContains("eligible-token"));
            StringAssert.StartsWith(upgrade ? "Elite · " : "Regular · ", token.tooltip);
            StringAssert.Contains(upgrade ? "Click to replace a Sauron Regular in Minas Morgul" : "Click to recruit a Sauron Regular in Minas Morgul",
                token.tooltip);
            return token;
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
