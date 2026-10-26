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
    public class KingsCards04NetworkTests
    {
        [UnityTest] public IEnumerator EnemyChoosesSovereignWithVisibleThreshold() => Wait(Sovereign());
        [UnityTest] public IEnumerator EnemySeparatesFieldArmyFromGarrison() => Wait(Army());
        [UnityTest] public IEnumerator EnemyLossesAllowEliteDowngradeAfterReconnect() => Wait(Losses());
        [UnityTest] public IEnumerator EnemyCombatChoosesCorruptedSovereign() => Wait(Power());

        private static Task Layout(MatchTestRoom room, MountedMatchClient client, string name) =>
            KingsCardNetworkChecks.Layout(room, client, name, "kings04");

        private static async Task Sovereign()
        {
            using (var room = await Connect("kings-enemy-sovereign", ScenarioKind.CardPack))
            {
                await Layout(room, room.Sh, "sovereign");
                await KingsCardNetworkChecks.ChooseKing(room, room.Sh, "kings.sovereign.denethor", "4+");
                await KingsCardNetworkChecks.Reconnect(room, room.Sh);
                Assert.AreEqual("Region", room.Sh.Board.View.Decision.Presentation);
                Assert.AreEqual(2, room.Sh.Board.View.Options.Single().EligibleIds.Count);
            }
        }

        private static async Task Army()
        {
            using (var room = await Connect("kings-enemy-army", ScenarioKind.CardPack))
            {
                MapContourProbe.ClickRegion(room.Sh.Board.Map, "minas-tirith");
                var field = room.Sh.Board.Tray.Q<Button>("effect-choice-minas-tirith:field");
                var garrison = room.Sh.Board.Tray.Q<Button>("effect-choice-minas-tirith:garrison");
                Assert.IsTrue(field.text.Contains("Field"));
                Assert.IsTrue(garrison.text.Contains("Garrison"));
                Assert.IsFalse(room.Sh.Board.Console.Status.Trail.Query<Button>(className: "committed-choice").ToList().Any(b => b.text.Contains("kings.")));
                await Layout(room, room.Sh, "army");
                var revision = room.Sh.Board.View.Revision;
                CardNetworkTests.Click(garrison);
                await room.At(revision + 1);
                await KingsCardNetworkChecks.Reconnect(room, room.Sh);
                Assert.Greater(room.Fp.Board.View.Revision, revision);
                if (room.Fp.Board.View.EventLoss != null)
                    Assert.IsTrue(room.Fp.Board.View.EventLoss.Garrison);
            }
        }

        private static async Task Losses()
        {
            using (var room = await Connect("kings-enemy-losses", ScenarioKind.CardPack))
            {
                var elite = room.Fp.Board.View.Units.Single(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Elite);
                var garrison = room.Fp.Board.View.Units.Single(u => u.Location == "minas-tirith" && u.Garrison).Id;
                Click(room.Fp, "loss-" + elite.Id);
                await Layout(room, room.Fp, "losses");
                await KingsCardNetworkChecks.Reconnect(room, room.Fp);
                await room.Confirm(room.Fp);
                Assert.AreEqual(3, room.Sh.Board.View.Units.Count(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Regular));
                Assert.AreEqual("minas-tirith", room.Sh.Board.View.Units.Single(u => u.Id == garrison).Location);
                var tile = room.Fp.Board.View.Sovereigns.Single(s => s.Id == "kings.sovereign.dain").Tiles.Single();
                Assert.IsNull(tile.Value);
                Assert.IsEmpty(tile.TileId);
            }
        }

        private static async Task Power()
        {
            using (var room = await Connect("kings-enemy-power", ScenarioKind.CardPack))
            {
                await Layout(room, room.Sh, "power");
                await KingsCardNetworkChecks.ChooseKing(room, room.Sh, "kings.sovereign.denethor");
                await KingsCardNetworkChecks.Reconnect(room, room.Sh);
                Assert.AreNotEqual("Sovereign", room.Sh.Board.View.Decision?.Presentation);
            }
        }
    }
}
