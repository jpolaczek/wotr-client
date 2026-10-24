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
    // UI-307 with two real clients on fresh test scenarios: the Shadow's Muster die recruits, or advances a nation, and the
    // Free Peoples' board presents it from the recorded facts before releasing the view.
    public sealed class OpponentRecruitmentPoliticsNetworkTests
    {
        [UnityTest]
        public IEnumerator TheShadowsRecruitsLandWithTheirBadgeBeforeTheViewIsReleased() => Wait(Recruit());

        [UnityTest]
        public IEnumerator TheShadowsPoliticalStepIsShownInTheFreePeoplesPanelBeforeTheViewIsReleased() => Wait(Politics());

        private static async Task Recruit()
        {
            var session = await TestScenarioRoom.Start("sh-muster-recruit");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                var option = sh.Board.View.Options.First(o => o.Kind == CommandKind.Recruit);
                var recruit = option.Recruitment.First(r => r.Kind == UnitKind.Regular && r.ReserveIds.Count > 0 && r.Targets.Count > 0);
                var target = recruit.Targets[0];
                var unit = recruit.ReserveIds[0];
                var before = fp.Board.View.Units.Count(u => u.Location == target && u.Side == Side.Shadow);
                await session.Send(Side.Shadow, CommandKind.Recruit, new CommandPayload
                {
                    DieId = MusterDie(sh.Board.View), Placements = { new UnitPlacementChoice { UnitId = unit, RegionId = target } }
                });
                await session.Until(() => fp.Playback.Holding && fp.Board.Map.Q("observed-delta") != null);
                var delta = fp.Board.Map.Q("observed-delta");
                StringAssert.StartsWith("+", delta.Q<Label>("observed-delta-count").text);
                Assert.IsNotNull(delta.Q(className: "nation-emblem-" + recruit.Nation), "The badge shows the recruit's nation.");
                Assert.AreEqual(before, fp.Board.View.Units.Count(u => u.Location == target && u.Side == Side.Shadow),
                    "The board's view is still the presented one.");
                await session.Until(() => !fp.Playback.Holding);
                Assert.AreEqual(target, fp.Board.View.Units.Single(u => u.Id == unit).Location);
                Assert.AreEqual(before + 1, fp.Board.Presentation.Armies.Where(a => a.RegionId == target && a.Side == Side.Shadow).Sum(a => a.Total));
            }
            finally
            {
                await session.End();
            }
        }

        private static async Task Politics()
        {
            var session = await TestScenarioRoom.Start("sh-muster-politics");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                var option = sh.Board.View.Options.First(o => o.Kind == CommandKind.AdvancePolitics);
                var nation = option.Targets.Concat(option.EligibleIds).First(id => sh.Board.View.Nations.Any(n => n.Id == id));
                var steps = fp.Board.View.Nations.Single(n => n.Id == nation).StepsToWar;
                await session.Send(Side.Shadow, CommandKind.AdvancePolitics, new CommandPayload { DieId = MusterDie(sh.Board.View), NationId = nation });
                await session.Until(() => fp.Playback.Holding && fp.Board.Nations.Drawer.style.display.value == DisplayStyle.Flex);
                await session.Until(() => PresentationGlow.IsLit(fp.Board.Nations.Drawer.Q<Button>("nation-status-" + nation)) ||
                    !fp.Playback.Holding);
                Assert.IsTrue(PresentationGlow.IsLit(fp.Board.Nations.Drawer.Q<Button>("nation-status-" + nation)), "The nation's token glows.");
                Assert.AreEqual(steps, fp.Board.View.Nations.Single(n => n.Id == nation).StepsToWar, "The board's view is still the presented one.");
                await session.Until(() => !fp.Playback.Holding);
                Assert.AreEqual(steps - 1, fp.Board.View.Nations.Single(n => n.Id == nation).StepsToWar);
                Assert.AreEqual(DisplayStyle.None, fp.Board.Nations.Drawer.style.display.value, "The panel the presentation opened closes.");
            }
            finally
            {
                await session.End();
            }
        }

        private static string MusterDie(PlayerView view) =>
            view.Dice.First(d => d.Owner == view.You && d.Face == DieFace.Muster && d.Location == DieLocation.Available).Id;
    }
}
