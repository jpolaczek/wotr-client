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
    public class ShadowStrategy03NetworkTests
    {
        [UnityTest] public IEnumerator HalfOrcsChoosesArmyOnMapThenPrintedReserveFigure() => Wait(HalfOrcs());
        [UnityTest] public IEnumerator OlogHaiConfirmsAnEliteAtTheChosenArmy() => Wait(OlogHai());
        [UnityTest] public IEnumerator HillTrollsKeepsBothSelectionsAcrossMapRegions() => Wait(HillTrolls());
        [UnityTest] public IEnumerator NewPowerUsesOnePlanWithUndoPreviewAndReconnect() => Wait(NewPower());
        [UnityTest] public IEnumerator NewPowerShortageRequiresTheWholePossibleResult() => Wait(NewPowerShortage());
        [UnityTest] public IEnumerator ManyKingsChoosesThreeSettlementsWithoutNumericInput() => Wait(ManyKings());
        [UnityTest] public IEnumerator KingRevealedLetsShadowReturnOldExcessAfterAllRecruits() => Wait(KingExcess());
        [UnityTest] public IEnumerator WeComeToKillLeavesItsSpecialHitToFreePeoples() => Wait(SpecialLoss());

        private static async Task HalfOrcs()
        {
            using (var room = await Connect("sh-str03-half-orcs-region", ScenarioKind.CardPack))
            {
                room.Sh.Board.Map.Select("westemnet");
                await room.Confirm(room.Sh);
                var choice = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.ChooseEffect).EligibleIds;
                var id = room.Sh.Board.View.Units.Single(u => choice.Contains(u.Id) && u.Kind == UnitKind.Regular).Id;
                Click(room.Sh, "effect-choice-" + id);
                await room.Confirm(room.Sh);
                Assert.IsTrue(room.Fp.Board.View.Units.Any(u => u.Id == id && u.Nation == "isengard" && u.Location == "westemnet"));
            }
        }

        private static async Task OlogHai()
        {
            using (var room = await Connect("sh-str03-olog-reserve", ScenarioKind.CardPack))
            {
                var choices = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.ChooseEffect).EligibleIds;
                var id = room.Sh.Board.View.Units.Single(u => choices.Contains(u.Id) && u.Kind == UnitKind.Elite).Id;
                Click(room.Sh, "effect-choice-" + id);
                Assert.IsTrue(room.Fp.Board.View.Units.Any(u => u.Id == id && u.Location == "reserve"));
                await room.Confirm(room.Sh);
                Assert.IsTrue(room.Fp.Board.View.Units.Any(u => u.Id == id && u.Location == "westemnet"));
            }
        }

        private static async Task HillTrolls()
        {
            using (var room = await Connect("sh-str03-hill-trolls", ScenarioKind.CardPack))
            {
                foreach (var region in new[] { "moria", "orthanc" })
                {
                    room.Sh.Board.Map.Select(region);
                    await room.Settle();
                    var id = room.Sh.Board.View.Units.Single(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular && u.Location == region).Id;
                    Click(room.Sh, "effect-choice-" + id);
                }
                await room.Confirm(room.Sh);
                Assert.AreEqual(2, room.Fp.Board.View.Units.Count(u => u.Nation == "sauron" && u.Kind == UnitKind.Elite && (u.Location == "moria" || u.Location == "orthanc")));
                Assert.AreEqual(1, room.Fp.Board.View.Units.Count(u => u.Location == "gorgoroth" && u.Kind == UnitKind.Regular));
            }
        }

        private static async Task NewPower()
        {
            using (var room = await Connect("sh-str03-new-power", ScenarioKind.CardPack))
            {
                await Recruit(room, "isengard", "north-dunland", UnitKind.Regular, 1);
                await room.Settle();
                Click(room.Sh, "event-placement-0");
                Assert.AreEqual(DisplayStyle.None, room.Sh.Board.Reinforcements.Root.Q<Button>("event-placement-0").style.display.value,
                    "Returning the first planned figure must empty the plan before adding replacements.");
                Assert.IsFalse(room.Sh.Board.Tray.Q<Button>("confirm-decision").enabledSelf);
                await Recruit(room, "isengard", "north-dunland", UnitKind.Regular, 2);
                await Recruit(room, "isengard", "south-dunland", UnitKind.Regular, 2);
                await Recruit(room, "isengard", "orthanc", UnitKind.Elite, 2);
                Assert.IsFalse(room.Fp.Board.View.Units.Any(u => u.Nation == "isengard" && u.Location == "orthanc"));
                var connected = room.Sh.Connected;
                room.Sh.Connection.Reconnect();
                await room.Until(() => room.Sh.Connected > connected);
                await room.Confirm(room.Sh);
                Assert.AreEqual(6, room.Fp.Board.View.History.Count(h => h.Kind == "EventRecruited"));
                Assert.AreEqual(2, room.Fp.Board.View.Units.Count(u => u.Location == "orthanc" && u.Kind == UnitKind.Elite));
            }
        }

        private static async Task NewPowerShortage()
        {
            using (var room = await Connect("sh-str03-new-power-shortage", ScenarioKind.CardPack))
            {
                Assert.AreEqual(4, room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.ChooseEffect).Minimum);
                await Recruit(room, "isengard", "south-dunland", UnitKind.Regular, 2);
                Assert.IsFalse(room.Sh.Board.Tray.Q<Button>("confirm-decision").enabledSelf);
                await Recruit(room, "isengard", "orthanc", UnitKind.Elite, 2);
                await room.Confirm(room.Sh);
                Assert.AreEqual(4, room.Fp.Board.View.History.Count(h => h.Kind == "EventRecruited"));
                Assert.IsFalse(room.Fp.Board.View.Units.Any(u => u.Location == "north-dunland"));
            }
        }

        private static async Task ManyKings()
        {
            using (var room = await Connect("sh-str03-many-kings", ScenarioKind.CardPack))
            {
                foreach (var region in new[] { "far-harad", "umbar", "north-rhun" })
                    await Recruit(room, "southrons", region, UnitKind.Regular, 2);
                await room.Confirm(room.Sh);
                Assert.AreEqual(6, room.Fp.Board.View.Units.Count(u => u.Nation == "southrons" && u.Location != "reserve"));
                Assert.AreEqual(2, room.Fp.Board.View.Nations.Single(n => n.Id == "southrons").StepsToWar);
            }
        }

        private static async Task KingExcess()
        {
            using (var room = await Connect("sh-str03-king-excess", ScenarioKind.CardPack))
            {
                Assert.AreEqual(6, room.Fp.Board.View.Units.Count(u => u.Nation == "sauron" && u.Location == "minas-morgul"));
                var old = room.Sh.Board.View.Units.First(u => u.Nation == "isengard" && u.Location == "minas-morgul").Id;
                await room.ChooseFigure(room.Sh, old);
                await room.Confirm(room.Sh);
                Assert.AreEqual("reserve", room.Fp.Board.View.Units.Single(u => u.Id == old).Location);
                Assert.AreEqual(6, room.Fp.Board.View.Units.Count(u => u.Nation == "sauron" && u.Location == "minas-morgul"));
            }
        }

        private static async Task SpecialLoss()
        {
            using (var room = await Connect("sh-str03-we-come-loss", ScenarioKind.CardPack))
            {
                Assert.IsNull(room.Sh.Board.View.Decision);
                Assert.AreEqual(1, room.Fp.Board.View.Battle.PendingHits);
                var id = room.Fp.Board.View.Options.Single(o => o.Kind == CommandKind.TakeBattleLosses).EligibleIds[0];
                Click(room.Fp, "loss-" + id);
                await room.Confirm(room.Fp);
                Assert.AreEqual("casualties", room.Sh.Board.View.Units.Single(u => u.Id == id).Location);
            }
        }

        private static async Task Recruit(MatchTestRoom room, string nation, string region, UnitKind kind, int count)
        {
            room.Sh.Board.Map.Select(region);
            await room.Settle();
            for (var i = 0; i < count; i++)
                Click(room.Sh, "event-reserve-" + nation + "-" + kind);
        }
    }
}
