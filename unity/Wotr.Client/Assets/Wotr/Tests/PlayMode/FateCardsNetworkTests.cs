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
    public class FateCardsNetworkTests
    {
        [UnityTest] public IEnumerator ThereSeparatesThenChangesGuideBeforePolitics() => Wait(There());
        [UnityTest] public IEnumerator BookSelectsCompanionRegionAndEliteAfterReconnect() => Wait(Book());
        [UnityTest] public IEnumerator BardingsUsesOneRecruitmentPlanAndPrivatelyDraws() => Wait(Recruit("fate-bardings", "north"));
        [UnityTest] public IEnumerator DwarvesUsesOneRecruitmentPlanAndPrivatelyDraws() => Wait(Recruit("fate-dwarves", "dwarves"));
        [UnityTest] public IEnumerator OrcsSharesTheReserveBetweenDolGuldurAndErebor() => Wait(Orcs());
        [UnityTest] public IEnumerator MonstersPreservesBranchEliteRegionAndPartialPlan() => Wait(Monsters());

        private static async Task There()
        {
            using (var room = await Connect("fate-there", ScenarioKind.CardPack))
            {
                var before = room.Fp.Board.View.Nations.Single(n => n.Id == "north").StepsToWar;
                Click(room.Fp, "companion-base.character.merry");
                await room.Settle();
                // UI-105: the highlighted destination is the whole gesture; its click sends Merry at once.
                await Layout(room, room.Fp, "there-route", "fate");
                await room.MoveTo(room.Fp, "fords-of-bruinen");
                Assert.AreEqual(before, room.Sh.Board.View.Nations.Single(n => n.Id == "north").StepsToWar);
                Click(room.Fp, "companion-base.character.strider");
                await Reconnect(room, room.Fp);
                await room.Confirm(room.Fp);
                Assert.AreEqual(before - 1, room.Sh.Board.View.Nations.Single(n => n.Id == "north").StepsToWar);
                Assert.IsTrue(room.Sh.Board.View.Nations.Single(n => n.Id == "north").Active);
            }
        }

        private static async Task Book()
        {
            using (var room = await Connect("fate-book", ScenarioKind.CardPack))
            {
                var revision = room.Fp.Board.View.Revision;
                await Target(room, room.Fp, "iron-hills");
                await room.At(revision + 1);
                await Layout(room, room.Fp, "book-region", "fate");
                var view = room.Fp.Board.View;
                var id = view.Options.Single().EligibleIds.Single(i => view.Units.Single(u => u.Id == i).Kind == UnitKind.Elite);
                Click(room.Fp, "effect-choice-" + id);
                await Reconnect(room, room.Fp);
                await room.Confirm(room.Fp);
                Assert.AreEqual("iron-hills", room.Sh.Board.View.Units.Single(u => u.Id == id).Location);
                Assert.AreEqual(0, room.Sh.Board.View.Nations.Single(n => n.Id == "dwarves").StepsToWar);
            }
        }

        private static async Task Recruit(string scenario, string nation)
        {
            using (var room = await Connect(scenario, ScenarioKind.CardPack))
            {
                var revision = room.Fp.Board.View.Revision;
                await RecruitAt(room, room.Fp, nation, UnitKind.Elite, "iron-hills");
                await RecruitAt(room, room.Fp, nation, UnitKind.Leader, "iron-hills");
                Assert.AreEqual(revision, room.Fp.Board.View.Revision);
                await Layout(room, room.Fp, scenario, "fate");
                await Reconnect(room, room.Fp);
                await room.Confirm(room.Fp);
                Assert.IsTrue(room.Sh.Board.View.Units.Any(u => u.Nation == nation && u.Kind == UnitKind.Elite && u.Location == "iron-hills"));
                Assert.AreEqual(1, room.Fp.Board.View.Hand.Count);
                Assert.IsEmpty(room.Sh.Board.View.Hand);
                Assert.AreEqual(1, room.Sh.Board.View.Players.Single(p => p.Side == Side.FreePeoples).HandCount);
                Assert.AreEqual("sauron", room.Sh.Board.Map.Data.Regions["erebor"].Nation);
                Assert.AreEqual("City", room.Fp.Board.Map.Data.Regions["ered-luin"].Settlement);
            }
        }

        private static async Task Orcs()
        {
            using (var room = await Connect("fate-orcs", ScenarioKind.CardPack))
            {
                var revision = room.Sh.Board.View.Revision;
                await Target(room, room.Sh, "erebor");
                await room.At(revision + 1);
                await RecruitAt(room, room.Sh, "sauron", UnitKind.Regular, "dol-guldur");
                for (var i = 0; i < 3; i++)
                    await RecruitAt(room, room.Sh, "sauron", UnitKind.Regular, "erebor");
                await Layout(room, room.Sh, "orcs-plan", "fate");
                await Reconnect(room, room.Sh);
                await room.Confirm(room.Sh);
                Assert.AreEqual(5, room.Fp.Board.View.Units.Count(u => u.Location == "erebor" && u.Kind == UnitKind.Regular));
                Assert.IsFalse(room.Sh.Board.View.Units.Any(u => u.Nation == "sauron" && u.Kind == UnitKind.Regular && u.Location == "reserve"));
            }
        }

        private static async Task Monsters()
        {
            using (var room = await Connect("fate-monsters", ScenarioKind.CardPack))
            {
                Click(room.Sh, "effect-choice-fate.monsters.east");
                await Layout(room, room.Sh, "monsters-branch", "fate");
                await room.Confirm(room.Sh);
                var revision = room.Sh.Board.View.Revision;
                await Target(room, room.Sh, "withered-heath");
                await room.At(revision + 1);
                await RecruitAt(room, room.Sh, "sauron", UnitKind.Regular, "northern-mirkwood");
                await RecruitAt(room, room.Sh, "sauron", UnitKind.Elite, "withered-heath");
                await Reconnect(room, room.Sh);
                await room.Confirm(room.Sh);
                Assert.IsTrue(room.Fp.Board.View.Units.Any(u => u.Location == "withered-heath" && u.Kind == UnitKind.Elite));
                Assert.IsTrue(room.Fp.Board.View.Units.Any(u => u.Location == "northern-mirkwood" && u.Kind == UnitKind.Regular));
            }
        }

        private static async Task RecruitAt(MatchTestRoom room, MountedMatchClient client, string nation, UnitKind kind, string region)
        {
            await Target(room, client, region);
            Click(client, "event-reserve-" + nation + "-" + kind);
            await room.Settle();
        }

        private static async Task Target(MatchTestRoom room, MountedMatchClient client, string region)
        {
            MapContourProbe.ClickRegion(client.Board.Map, region);
            await room.Settle();
        }
    }
}
