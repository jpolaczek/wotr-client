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
    public class ShadowStrategy01NetworkTests
    {
        [UnityTest] public IEnumerator ValinorChoosesTheNextStrongholdOnTheMap() => Wait(Valinor());
        [UnityTest] public IEnumerator ValinorOffersOnlyElvesAndAllowsEliteDowngrade() => Wait(ValinorLoss());
        [UnityTest] public IEnumerator UrukHaiExplainsThreeRoundsAndRequiresNoSecondDie() => Wait(UrukHai());
        [UnityTest] public IEnumerator DenethorLetsShadowChooseEitherNationsLeader() => Wait(DenethorLeader());
        [UnityTest] public IEnumerator RemovingDenethorSpendsTheExplicitDieOnTheTableCard() => Wait(DenethorRemove());
        [UnityTest] public IEnumerator StormcrowFocusesARegionThenEliminatesTheChosenWholeElite() => Wait(Stormcrow());
        [UnityTest] public IEnumerator RelentlessAssaultAllowsAnExplicitEmptySacrifice() => Wait(EmptySacrifice());
        [UnityTest] public IEnumerator OnslaughtDowngradeSurvivesReconnectWithoutAnEarlyCasualty() => Wait(Onslaught());
        [UnityTest] public IEnumerator GreatHostLeavesItsHitToTheDefender() => Wait(GreatHost());

        private static async Task Valinor()
        {
            using (var room = await Connect("sh-str01-valinor", ScenarioKind.CardPack))
            {
                room.Sh.Board.Map.Select("rivendell");
                Assert.IsFalse(room.Sh.Board.View.History.Any(h => h.Kind == "ReturnToValinor"));
                await room.Confirm(room.Sh);
                Assert.IsTrue(room.Fp.Board.View.History.Any(h => h.Kind == "ReturnToValinor" && h.Text.Contains("rivendell")));
                Assert.IsNull(room.Sh.Board.View.Battle);
            }
        }

        private static async Task ValinorLoss()
        {
            using (var room = await Connect("sh-str01-valinor-loss", ScenarioKind.CardPack))
            {
                var view = room.Fp.Board.View;
                var eligible = view.Options.Single(o => o.Kind == CommandKind.TakeEventLosses).EligibleIds;
                Assert.IsTrue(view.Units.Where(u => eligible.Contains(u.Id)).All(u => u.Nation == "elves"));
                var elite = view.Units.Single(u => eligible.Contains(u.Id) && u.Kind == UnitKind.Elite).Id;
                Click(room.Fp, "loss-" + elite);
                Assert.IsTrue(room.Fp.Board.View.Units.Any(u => u.Id == elite && u.Location == "rivendell"));
                await room.Confirm(room.Fp);
                Assert.IsFalse(room.Sh.Board.View.Units.Any(u => u.Id == elite && u.Location == "rivendell"));
                Assert.AreEqual(3, room.Sh.Board.View.Units.Count(u => u.Nation == "elves" && u.Kind == UnitKind.Regular && u.Location == "rivendell"));
                Assert.IsTrue(room.Sh.Board.View.Units.Any(u => u.Nation == "north" && u.Location == "rivendell"));
            }
        }

        private static async Task UrukHai()
        {
            using (var room = await Connect("sh-str01-uruk-hai", ScenarioKind.CardPack))
            {
                room.Sh.Board.Map.Select("minas-tirith");
                await room.Settle();
                MapContourProbe.ClickRegion(room.Sh.Board.Map, "minas-tirith");
                await room.Confirm(room.Sh);
                Assert.AreEqual(3, room.Fp.Board.View.Battle.MandatorySiegeRounds);
                Assert.That(room.Fp.Board.Tray.Q<Button>("battle-card-defender").text, Does.Contain("No Companion · No card this round"));
                Assert.AreEqual(1, room.Sh.Board.View.Dice.Count(d => d.Location == DieLocation.Resolving));
                await room.Direct(room.Sh, "battle-no-card");
                Assert.IsFalse(room.Fp.Board.View.Decision?.Kind == DecisionKind.CombatCard && room.Fp.Board.View.Battle.Round == 1);
            }
        }

        private static async Task DenethorLeader()
        {
            using (var room = await Connect("sh-str01-denethor-leader", ScenarioKind.CardPack))
            {
                var view = room.Sh.Board.View;
                Assert.IsNull(room.Fp.Board.View.Decision);
                Assert.AreEqual("ArmyFigure", view.Decision.Presentation);
                var eligible = view.Options.Single(o => o.Kind == CommandKind.ChooseEffect).EligibleIds;
                Assert.AreEqual(2, eligible.Count);
                var leader = view.Units.Single(u => eligible.Contains(u.Id) && u.Nation == "rohan").Id;
                await room.ChooseFigure(room.Sh, leader);
                await room.Confirm(room.Sh);
                Assert.IsFalse(room.Sh.Board.View.Units.Any(u => u.Id == leader && u.Location == "minas-tirith"));
                Assert.IsTrue(room.Sh.Board.View.Units.Any(u => u.Nation == "gondor" && u.Kind == UnitKind.Leader && u.Location == "minas-tirith"));
                Assert.IsTrue(room.Sh.Board.View.Effects.Any(e => e.SourceId == "base.sh.strategy.03"));
            }
        }

        private static async Task DenethorRemove()
        {
            using (var room = await Connect("sh-str01-denethor-remove", ScenarioKind.CardPack))
            {

                var die = room.Fp.Board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Army);
                await ComponentGestures.Cost(room, room.Fp, die.Id);
                Assert.AreEqual(DieLocation.Available, room.Fp.Board.View.Dice.Single(d => d.Id == die.Id).Location);
                Assert.IsTrue(room.Sh.Board.View.Effects.Any(e => e.SourceId == "base.sh.strategy.03"));
                var revision = room.Fp.Board.View.Revision;
                await ComponentGestures.Remove(room.Fp, "base.sh.strategy.03");
                await room.At(revision + 1);
                Assert.AreEqual(DieLocation.Spent, room.Sh.Board.View.Dice.Single(d => d.Id == die.Id).Location);
                Assert.IsFalse(room.Sh.Board.View.Effects.Any(e => e.SourceId == "base.sh.strategy.03"));
            }
        }

        private static async Task Stormcrow()
        {
            using (var room = await Connect("sh-str01-stormcrow", ScenarioKind.CardPack))
            {
                Assert.IsNull(room.Sh.Board.View.Decision);
                Assert.AreEqual(2, room.Fp.Board.View.Nations.Single(n => n.Id == "rohan").StepsToWar);
                room.Fp.Board.Map.Select("helms-deep");
                await room.Settle();
                var view = room.Fp.Board.View;
                var elite = view.Units.Single(u => u.Location == "helms-deep" && u.Kind == UnitKind.Elite).Id;
                var regulars = view.Units.Count(u => u.Location == "helms-deep" && u.Kind == UnitKind.Regular);
                Click(room.Fp, "effect-choice-" + elite);
                await room.Confirm(room.Fp);
                Assert.IsFalse(room.Sh.Board.View.Units.Any(u => u.Id == elite && u.Location == "helms-deep"));
                Assert.AreEqual(regulars, room.Sh.Board.View.Units.Count(u => u.Location == "helms-deep" && u.Kind == UnitKind.Regular));
                Assert.IsTrue(room.Sh.Board.View.Characters.Any(c => c.DefinitionId == "base.character.gandalf-grey" && c.Location == "edoras"));
            }
        }

        private static async Task EmptySacrifice()
        {
            using (var room = await Connect("sh-str01-relentless", ScenarioKind.CardPack))
            {
                Assert.IsTrue(room.Sh.Board.View.Battle.VoluntaryLosses);
                Assert.AreEqual(0, room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.TakeBattleLosses).Minimum);
                await room.Confirm(room.Sh);
                Assert.IsTrue(room.Fp.Board.View.History.Any(h => h.Kind == "BattleLosses" && h.Text.Contains("resolved 0 hit(s) from base.sh.strategy.04")));
            }
        }

        private static async Task Onslaught()
        {
            using (var room = await Connect("sh-str01-onslaught", ScenarioKind.CardPack))
            {
                var elite = room.Sh.Board.View.Battle.Attacker.Units.Single(u => u.Kind == UnitKind.Elite).Id;
                Click(room.Sh, "loss-" + elite);
                await room.Until(() => room.Sh.Board.Tray.Query<Label>().ToList().Any(l => l.text.Contains("Roll 1 die · Hits on 4+")));
                var connected = room.Sh.Connected;
                room.Sh.Connection.Reconnect();
                await room.Until(() => room.Sh.Connected > connected);
                Assert.IsTrue(room.Fp.Board.View.Units.Any(u => u.Id == elite && u.Location == "osgiliath"));
                await room.Confirm(room.Sh);
                Assert.IsFalse(room.Fp.Board.View.Units.Any(u => u.Id == elite && u.Location == "osgiliath"));
                Assert.IsTrue(room.Fp.Board.View.History.Any(h => h.Kind == "BattleLosses" && h.Text.Contains("resolved 1 hit(s) from base.sh.strategy.02")));
            }
        }

        private static async Task GreatHost()
        {
            using (var room = await Connect("sh-str01-great-host", ScenarioKind.CardPack))
            {
                Assert.IsNull(room.Sh.Board.View.Decision);
                Assert.AreEqual(1, room.Fp.Board.View.Battle.PendingHits);
                var unit = room.Fp.Board.View.Options.Single(o => o.Kind == CommandKind.TakeBattleLosses).EligibleIds[0];
                Click(room.Fp, "loss-" + unit);
                await room.Confirm(room.Fp);
                Assert.IsFalse(room.Sh.Board.View.Units.Any(u => u.Id == unit && u.Location == "north-ithilien"));
            }
        }
    }
}
