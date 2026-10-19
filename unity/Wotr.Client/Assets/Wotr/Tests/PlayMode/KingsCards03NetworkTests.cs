using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public class KingsCards03NetworkTests
    {
        [UnityTest] public IEnumerator ValinorChoosesRegionOrderOnTheMap() => Wait(ValinorRegion());
        [UnityTest] public IEnumerator ValinorLossesStayWithFreePeoplesAfterReconnect() => Wait(ValinorLoss());
        [UnityTest] public IEnumerator StormcrowChoosesWholeEliteWithoutRevealingItsTile() => Wait(Stormcrow());
        [UnityTest] public IEnumerator FollyUsesNamedCompanionAndOneActionDie() => Wait(Folly());
        [UnityTest] public IEnumerator KingChoosesRegularAndNazgulBonusWithoutNumberEntry() => Wait(King());
        [UnityTest] public IEnumerator PalantirDrawShowsConversionAndConsumesItOnce() => Wait(PalantirDraw());
        [UnityTest] public IEnumerator PalantirEventKeepsNativeMusterAndConvertedEventDistinct() => Wait(PalantirEvent());
        [UnityTest] public IEnumerator PalantirRemovalKeepsOrthancAndPaysTheRing() => Wait(PalantirRemove());
        [UnityTest] public IEnumerator PowerChoosesSovereignBeforeCombat() => Wait(Power());

        private static Task Layout(MatchTestRoom room, MountedMatchClient client, string name) =>
            KingsCardNetworkChecks.Layout(room, client, name, "kings03");

        private static async Task ValinorRegion()
        {
            using (var room = await Connect("kings-valinor-regions", ScenarioKind.CardPack))
            {
                var revision = room.Sh.Board.View.Revision;
                MapContourProbe.ClickRegion(room.Sh.Board.Map, "rivendell");
                await room.At(revision + 1);
                await Layout(room, room.Sh, "valinor-region");
                await KingsCardNetworkChecks.Reconnect(room, room.Sh);
                Assert.AreEqual(revision + 1, room.Fp.Board.View.Revision);
            }
        }

        private static async Task ValinorLoss()
        {
            using (var room = await Connect("kings-valinor-losses", ScenarioKind.CardPack))
            {
                var unit = room.Fp.Board.View.Units.Single(u => u.Location == "lorien" && u.Kind == UnitKind.Regular);
                Click(room.Fp, "loss-" + unit.Id);
                await Layout(room, room.Fp, "valinor-loss");
                await KingsCardNetworkChecks.Reconnect(room, room.Fp);
                await room.Confirm(room.Fp);
                Assert.AreEqual("casualties", room.Sh.Board.View.Units.Single(u => u.Id == unit.Id).Location);
            }
        }

        private static async Task Stormcrow()
        {
            using (var room = await Connect("kings-stormcrow", ScenarioKind.CardPack))
            {
                var tile = room.Fp.Board.View.Sovereigns.Single(s => s.Id == "kings.sovereign.brand").Tiles.Single();
                Assert.IsNull(tile.Value);
                Assert.AreEqual("", tile.TileId);
                var unit = room.Fp.Board.View.Units.Single(u => u.Location == "bree" && u.Kind == UnitKind.Elite);
                MapContourProbe.ClickRegion(room.Fp.Board.Map, "bree");
                Click(room.Fp, "effect-choice-" + unit.Id);
                await Layout(room, room.Fp, "stormcrow");
                await KingsCardNetworkChecks.Reconnect(room, room.Fp);
                await room.Confirm(room.Fp);
                Assert.AreEqual("casualties", room.Sh.Board.View.Units.Single(u => u.Id == unit.Id).Location);
                Assert.AreEqual(1, room.Sh.Board.View.Sovereigns.Single(s => s.Id == "kings.sovereign.brand").Tiles.Single().Value);
            }
        }

        private static async Task Folly()
        {
            using (var room = await Connect("kings-folly", ScenarioKind.CardPack))
            {
                var option = room.Fp.Board.View.Options.Single(o => o.Kind == CommandKind.DiscardDenethor);
                await ComponentGestures.Cost(room, room.Fp, option.EligibleDice[0]);
                await Layout(room, room.Fp, "folly");
                await KingsCardNetworkChecks.Reconnect(room, room.Fp);
                var revision = room.Fp.Board.View.Revision;
                await ComponentGestures.Remove(room.Fp, "kings.sh.strategy.03");
                await room.At(revision + 1);
                Assert.IsFalse(room.Sh.Board.View.Players.Single(p => p.Side == Side.Shadow).TableCards.Contains("kings.sh.strategy.03"));
            }
        }

        private static async Task King()
        {
            using (var room = await Connect("kings-king-revealed", ScenarioKind.CardPack))
            {
                var option = room.Sh.Board.View.Options.Single();
                var regular = option.EligibleIds.First(id => room.Sh.Board.View.Units.Single(u => u.Id == id).Kind == UnitKind.Regular);
                var nazgul = option.EligibleIds.First(id => room.Sh.Board.View.Units.Single(u => u.Id == id).Kind == UnitKind.Nazgul);
                Click(room.Sh, "effect-choice-" + regular);
                Click(room.Sh, "effect-choice-" + nazgul);
                await Layout(room, room.Sh, "king");
                await KingsCardNetworkChecks.Reconnect(room, room.Sh);
                await room.Confirm(room.Sh);
                Assert.AreEqual(6, room.Fp.Board.View.Units.Count(u => u.Location == "minas-morgul" && u.Kind == UnitKind.Regular));
                Assert.AreEqual(2, room.Fp.Board.View.Units.Count(u => u.Location == "minas-morgul" && u.Kind == UnitKind.Nazgul));
            }
        }

        private static async Task PalantirDraw()
        {
            using (var room = await Connect("kings-palantir-draw", ScenarioKind.CardPack))
            {
                var option = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.DrawCard);
                var die = option.DrawCosts.First(c => c.AbilityId.Length > 0).DieId;
                CardNetworkTests.Click(room.Sh.Board.OwnDice.Q<Button>(die));
                await room.Settle();
                CardNetworkTests.Click(room.Sh.Board.CardCosts.Root.Q<Button>("card-cost-Event"));
                await Layout(room, room.Sh, "palantir-draw");
                Assert.That(room.Sh.Board.CardCosts.Root.Q<Button>("card-cost-Event").tooltip, Does.Contain("Palantír"));
                await KingsCardNetworkChecks.Reconnect(room, room.Sh);
                await CardGestures.Draw(room, room.Sh, "strategy");
                Assert.AreEqual(DieLocation.Spent, room.Fp.Board.View.Dice.Single(d => d.Id == die).Location);
                Assert.AreEqual(DieFace.Muster, room.Fp.Board.View.Dice.Single(d => d.Id == die).Face);
            }
        }

        private static async Task PalantirRemove()
        {
            using (var room = await Connect("kings-palantir-removal", ScenarioKind.CardPack))
            {
                var option = room.Fp.Board.View.Options.Single(o => o.Kind == CommandKind.DiscardBaradPalantir);
                await ComponentGestures.Cost(room, room.Fp, option.EligibleDice[0]);
                await ComponentGestures.Remove(room.Fp, "kings.sh.strategy.25");
                await Layout(room, room.Fp, "palantir-remove");
                await KingsCardNetworkChecks.Reconnect(room, room.Fp);
                var revision = room.Fp.Board.View.Revision;
                CardNetworkTests.Click(room.Fp.Board.Console.Rings.For(option.EligibleIds[0]));
                await room.At(revision + 1);
                var table = room.Sh.Board.View.Players.Single(p => p.Side == Side.Shadow).TableCards;
                Assert.IsFalse(table.Contains("kings.sh.strategy.25"));
                Assert.IsTrue(table.Contains("base.sh.character.21"));
            }
        }

        private static async Task PalantirEvent()
        {
            using (var room = await Connect("kings-palantir-event", ScenarioKind.CardPack))
            {
                var die = room.Sh.Board.View.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Id;
                CardNetworkTests.Click(room.Sh.Board.OwnDice.Q<Button>(die));
                await room.Settle();
                CardNetworkTests.Click(room.Sh.Board.CardCosts.Root.Q<Button>("card-cost-Event"));
                await Layout(room, room.Sh, "palantir-event");
                await KingsCardNetworkChecks.Reconnect(room, room.Sh);
                await CardGestures.Play(room, room.Sh, "kings.sh.strategy.05");
                Assert.AreEqual("Deck", room.Sh.Board.View.Decision.Presentation);
                await CardGestures.Draw(room, room.Sh, "strategy");
                Assert.IsTrue(room.Fp.Board.View.Players.Single(p => p.Side == Side.Shadow).TableCards.Contains("kings.sh.strategy.05"));
                Assert.AreEqual(DieFace.Muster, room.Fp.Board.View.Dice.Single(d => d.Id == die).Face);
            }
        }

        private static async Task Power()
        {
            using (var room = await Connect("kings-power", ScenarioKind.CardPack))
            {
                await Layout(room, room.Sh, "power");
                await KingsCardNetworkChecks.Reconnect(room, room.Sh);
                await KingsCardNetworkChecks.ChooseKing(room, room.Sh, "kings.sovereign.denethor");
                Assert.AreNotEqual("Sovereign", room.Sh.Board.View.Decision?.Presentation);
            }
        }
    }
}
