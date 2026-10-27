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
    // UI-278 (EXP-15 D15-D16, owner's decisions): the Kings cards' choices on their objects against the real server. A
    // nation token activates and advances its nation, a king's portrait cleanses him, a Reserve figure recruits, and in a
    // battle a king who takes part glows in his army while a Corrupted king keeps his portrait. No generic window opens.
    public class KingsBranchNetworkTests
    {
        private const string Denethor = "kings.sovereign.denethor";
        private const string Theoden = "kings.sovereign.theoden";

        [UnityTest] public IEnumerator WisdomsNationTokenActivatesAndAdvancesItsNation() => Wait(WisdomPolitics());
        [UnityTest] public IEnumerator ItIsNotSoDarkHeresNationStepIsTheNationToken() => Wait(DarkNation());
        [UnityTest] public IEnumerator TheStewardsCleanseIsDenethorsPortrait() => Wait(StewardsCleanse());
        [UnityTest] public IEnumerator ThranduilsReserveFigureRecruitsWithOneClick() => Wait(ThranduilRecruit());
        [UnityTest] public IEnumerator TheRidersReserveFigureStartsTheirPlan() => Wait(RidersPlan());
        [UnityTest] public IEnumerator HisPowerWaxesFindsTheDefendingKingInHisArmy() => Wait(PowerDefender());

        // Wisdom's politics: Gondor's token chooses the branch and carries Gondor into the Nation step.
        private static async Task WisdomPolitics()
        {
            using (var room = await Connect("ui278-wisdom-politics", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var board = room.Fp.Board;
                var before = board.View.Nations.Single(n => n.Id == "gondor").StepsToWar;
                Assert.AreEqual(KingsCardChoices.Branch, board.View.Decision.Presentation);
                await room.Settle();
                StringAssert.Contains("a glowing nation token to activate and advance it", board.Console.Status.Hint.text);
                Assert.IsNull(board.Console.Status.Options.Q<Button>("effect-choice-" + KingsCardChoices.Politics), "No branch button.");
                var token = await NationToken(room, board, "gondor");
                StringAssert.Contains("Click to activate and advance Gondor", token.tooltip);
                var revision = board.View.Revision;
                PointerGestureEvents.ClickAt(token);
                await room.At(revision + 2);
                var gondor = board.View.Nations.Single(n => n.Id == "gondor");
                Assert.IsTrue(gondor.Active);
                Assert.AreEqual(before - 1, gondor.StepsToWar);
                Assert.AreNotEqual(KingsCardChoices.NationStep, board.View.Decision?.Presentation);
                watch.NeverOpened();
            }
        }

        // It Is Not So Dark Here's Nation step opens Politics; the North's token advances the North without activating it.
        private static async Task DarkNation()
        {
            using (var room = await Connect("ui278-dark-nation", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var board = room.Fp.Board;
                var north = board.View.Nations.Single(n => n.Id == "north");
                var (before, active) = (north.StepsToWar, north.Active);
                Assert.AreEqual(KingsCardChoices.NationStep, board.View.Decision.Presentation);
                await room.Until(() => board.Nations.Drawer.resolvedStyle.display != DisplayStyle.None);
                StringAssert.Contains("Click a glowing nation token in Politics", board.Console.Status.Hint.text);
                var token = await NationToken(room, board, "north");
                StringAssert.Contains("Click to choose The North for It Is Not So Dark Here", token.tooltip);
                var revision = board.View.Revision;
                PointerGestureEvents.ClickAt(token);
                await room.At(revision + 1);
                north = board.View.Nations.Single(n => n.Id == "north");
                Assert.AreEqual(before - 1, north.StepsToWar);
                Assert.AreEqual(active, north.Active, "It Is Not So Dark Here does not activate the nation.");
                Assert.AreEqual(SovereignChoices.Sovereign, board.View.Decision.Presentation, "A king's tiles follow.");
                watch.NeverOpened();
            }
        }

        // House of the Stewards: Denethor's portrait removes his corruption; the Reserve's Gondor figures would recruit.
        private static async Task StewardsCleanse()
        {
            using (var room = await Connect("ui278-stewards-cleanse", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var board = room.Fp.Board;
                await room.Settle();
                Assert.IsTrue(board.Reinforcements.Tab.ClassListContains("legal-target"), "The Reserve recruits.");
                StringAssert.Contains("the glowing Denethor to remove his corruption", board.Console.Status.Hint.text);
                Assert.IsEmpty(board.Console.Status.Options.Query<Button>().ToList().Where(b => b.name.StartsWith("effect-choice-")));
                var portrait = await BoardNavigation.Sovereign(board, Denethor);
                Assert.IsTrue(portrait.ClassListContains("legal-target"));
                StringAssert.StartsWith("Click to remove corruption from Denethor", portrait.tooltip);
                var revision = board.View.Revision;
                PointerGestureEvents.ClickAt(portrait);
                await room.At(revision + 1);
                Assert.IsEmpty(board.View.Sovereigns.Single(s => s.Id == Denethor).Tiles);
                Assert.AreEqual(2, board.View.Hand.Count, "The card's two Strategy cards follow.");
                watch.NeverOpened();
            }
        }

        // Thranduil's Archers: the Reserve's Elite chooses the recruit and is the recruited figure.
        private static async Task ThranduilRecruit()
        {
            using (var room = await Connect("ui278-thranduil-recruit", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var board = room.Fp.Board;
                var token = await ReserveToken(room, board, "elves", UnitKind.Elite);
                StringAssert.Contains("Click to recruit with Thranduil’s Archers", token.tooltip);
                var revision = board.View.Revision;
                PointerGestureEvents.ClickAt(token);
                await room.At(revision + 2);
                Assert.IsTrue(room.Sh.Board.View.Units.Any(u => u.Nation == "elves" && u.Kind == UnitKind.Elite && u.Location == "woodland-realm"));
                Assert.IsFalse(room.Sh.Board.View.Units.Any(u => u.Nation == "elves" && u.Kind == UnitKind.Regular && u.Location == "woodland-realm"));
                watch.NeverOpened();
            }
        }

        // Riders of Théoden: the Reserve's Elite chooses the recruit and starts the plan with that Elite; its Leader follows.
        private static async Task RidersPlan()
        {
            using (var room = await Connect("ui278-riders-branch", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var board = room.Fp.Board;
                var token = await ReserveToken(room, board, "rohan", UnitKind.Elite);
                var revision = board.View.Revision;
                PointerGestureEvents.ClickAt(token);
                await room.At(revision + 1);
                Assert.AreEqual("RecruitmentPlan", board.View.Decision.Presentation);
                await room.Settle();
                MapContourProbe.ClickRegion(board.Map, "edoras");
                await room.Settle();
                Click(room.Fp, "event-reserve-rohan-Leader");
                MapContourProbe.ClickRegion(board.Map, "edoras");
                await room.Settle();
                await room.Confirm(room.Fp);
                Assert.AreEqual(1, room.Sh.Board.View.Units.Count(u => u.Location == "edoras" && u.Kind == UnitKind.Elite));
                Assert.AreEqual(2, room.Sh.Board.View.Units.Count(u => u.Location == "edoras" && u.Kind == UnitKind.Leader));
                watch.NeverOpened();
            }
        }

        // His Power Waxes: Denethor, defending Edoras, glows in the battle window's army; the Corrupted Théoden keeps his
        // portrait; the figure's click chooses Denethor.
        private static async Task PowerDefender()
        {
            using (var room = await Connect("ui278-power-defender", ScenarioKind.CardPack))
            using (var watch = new GenericWindowWatch(room.Fp, room.Sh))
            {
                var board = room.Sh.Board;
                Assert.AreEqual(SovereignChoices.Sovereign, board.View.Decision.Presentation);
                var figure = await BattleKing(room, board, "battle-defender", "battle-figure-gondor-Leader");
                StringAssert.StartsWith("Denethor · ", figure.tooltip);
                StringAssert.Contains("battle window's army or by his portrait", board.Console.Status.Hint.text);
                var theoden = await BoardNavigation.Sovereign(board, Theoden);
                Assert.IsTrue(theoden.ClassListContains("legal-target"), "A Corrupted king has no figure: his portrait glows.");
                Assert.IsFalse((await BoardNavigation.Sovereign(board, Denethor)).ClassListContains("legal-target"),
                    "Denethor glows in his army instead.");
                var revision = board.View.Revision;
                PointerGestureEvents.ClickAt(figure);
                await room.At(revision + 1);
                Assert.AreNotEqual(SovereignChoices.Sovereign, board.View.Decision?.Presentation);
                watch.NeverOpened();
            }
        }

        internal static async Task<VisualElement> BattleKing(MatchTestRoom room, BoardScreen board, string force, string figure)
        {
            VisualElement found = null;
            await room.Until(() => (found = board.Root.Query<VisualElement>(force).ToList().Select(f => f.Q(figure))
                .FirstOrDefault(f => f != null && f.ClassListContains(ChoiceGlow.ClassName) && f.worldBound.width > 0)) != null);
            return found;
        }

        private static async Task<Button> NationToken(MatchTestRoom room, BoardScreen board, string nation)
        {
            board.Nations.Open();
            await room.Settle();
            var token = board.Nations.Drawer.Q<Button>("nation-status-" + nation);
            Assert.IsNotNull(token, nation);
            Assert.IsTrue(token.ClassListContains("legal-target"), nation + " glows for the card.");
            return token;
        }

        private static async Task<Button> ReserveToken(MatchTestRoom room, BoardScreen board, string nation, UnitKind kind)
        {
            await room.Settle();
            Assert.IsTrue(board.Reinforcements.Tab.ClassListContains("legal-target"), "The Reserve's tab glows for the recruit.");
            board.Reinforcements.Open();
            await room.Settle();
            var token = board.Reinforcements.Root.Q<Button>("branch-reserve-" + nation + "-" + kind);
            Assert.IsNotNull(token, nation + " " + kind);
            Assert.IsTrue(token.ClassListContains("eligible-token"), "The figure glows for the recruit.");
            return token;
        }
    }
}
