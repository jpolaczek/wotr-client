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
    public sealed class ExpansionCostNetworkTests
    {
        [UnityTest] public IEnumerator MouthConvertsTheSelectedMusterIntoTwoArmySteps() => Wait(Army("mouth"));
        [UnityTest] public IEnumerator GreatSorceryRequiresMouthAndPaysOnlyOneArmyStep() => Wait(Army("sorcery"));
        [UnityTest] public IEnumerator BalrogSymbolMovesOnlyAnArmyWithBalrog() => Wait(Army("balrog"));
        [UnityTest] public IEnumerator BalrogSymbolTravelsWithoutGrantingNazgulAnAction() => Wait(Travel());

        private static async Task Army(string source)
        {
            using (var room = await Connect("ui22-" + source + "-army", ScenarioKind.CardPack))
            {
                var client = room.Sh;
                var view = client.Board.View;
                var option = view.Options.Single(o => o.Kind == CommandKind.MoveArmies);
                var cost = option.MovementCosts.First(c => c.AbilityId.Length > 0);
                await ComponentGestures.Cost(room, client, cost.DieId);
                var component = "MoveArmies-" + cost.AbilityId;
                var origin = source == "balrog" ? "moria" : "osgiliath";
                var target = source == "balrog" ? "dimrill-dale" : "south-ithilien";
                Assert.AreEqual(origin, client.Board.Hover.Panel.ActionRegion(component));
                await ComponentGestures.Select(client, component);
                var soldier = view.Units.Single(u => u.Location == origin && u.Kind == UnitKind.Regular).Id;
                FellowshipGestures.Figure(client, origin, soldier);
                if (cost.RequiredCharacterId.Length > 0)
                {
                    Assert.IsFalse(client.Board.Map.DraftOverlay.Targets.Contains(target), "Missing required character must not offer a legal target.");
                    FellowshipGestures.Figure(client, origin, cost.RequiredCharacterId);
                }
                await KingsCardNetworkChecks.Reconnect(room, client);
                await ArmyGestures.Target(room, client, target);
                if (source == "mouth")
                {
                    Assert.AreEqual(1, client.Board.View.ActionSequence.Remaining);
                    Assert.AreEqual(cost.AbilityId, client.Board.View.ActionSequence.AbilityId);
                    await ArmyGestures.Finish(room, client);
                }
                Assert.AreEqual(target, room.Fp.Board.View.Units.Single(u => u.Id == soldier).Location);
                Assert.AreEqual(DieLocation.Spent, room.Fp.Board.View.Dice.Single(d => d.Id == cost.DieId).Location);
                Assert.AreEqual(view.Dice.Single(d => d.Id == cost.DieId).Face, room.Fp.Board.View.Dice.Single(d => d.Id == cost.DieId).Face);
            }
        }

        private static async Task Travel()
        {
            using (var room = await Connect("ui22-balrog-travel", ScenarioKind.CardPack))
            {
                var client = room.Sh;
                var option = client.Board.View.Options.Single(o => o.Kind == CommandKind.MoveShadowCharacters);
                var cost = option.MovementCosts.Single(c => c.AbilityId == "use-balrog");
                await ComponentGestures.Cost(room, client, cost.DieId);
                await ComponentGestures.Select(client, "MoveShadowCharacters-use-balrog");
                FellowshipGestures.Figure(client, "moria", cost.RequiredCharacterId);
                var nazgul = client.Board.View.Units.Single(u => u.Location == "moria" && u.Kind == UnitKind.Nazgul).Id;
                Assert.IsTrue(client.Board.Hover.Panel.Root.Q<Button>("hover-figure-" + nazgul).ClassListContains("eligible-figure"),
                    "The same die may still start an army selection (UI-28).");
                Assert.IsFalse(client.Board.Hover.Panel.Root.Q<Button>("hover-figure-" + nazgul).ClassListContains("selected"),
                    "The independent Balrog journey must not include the Nazgul.");
                await KingsCardNetworkChecks.Reconnect(room, client);
                Assert.IsTrue(client.Board.Map.DraftOverlay.Targets.Contains("dimrill-dale"), "The Balrog's selection survives the reconnect.");
                var revision = client.Board.View.Revision;
                FellowshipGestures.FinishRoute(client, "dimrill-dale");
                await room.At(revision + 1);
                Assert.IsNull(client.Board.View.ActionSequence);
                Assert.AreEqual("dimrill-dale", room.Fp.Board.View.Characters.Single(c => c.Id == cost.RequiredCharacterId).Location);
                Assert.AreEqual("moria", room.Fp.Board.View.Units.Single(u => u.Id == nazgul).Location);
                Assert.AreEqual(DieLocation.Spent, room.Fp.Board.View.Dice.Single(d => d.Id == cost.DieId).Location);
            }
        }
    }
}
