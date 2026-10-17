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
    public class Strategy03NetworkTests
    {
        [UnityTest]
        public IEnumerator CirdanSelectsMapAndTwoRecruitsWithOneRecruitmentCommit() => Wait(Cirdan());
        [UnityTest]
        public IEnumerator RidersRecruitsArmyAndLeaderAtTheChosenCompanion() => Wait(Riders());
        [UnityTest]
        public IEnumerator ChargeShowsImmediateLossBeforeCombatRoll() => Wait(Charge());

        private static async Task Cirdan()
        {
            using (var room = await Connect("strategy-cirdan", ScenarioKind.CardPack))
            {
                room.Fp.Board.Map.Select("grey-havens");
                await room.Confirm(room.Fp);
                var view = room.Fp.Board.View;
                var option = view.Options.Single(o => o.Kind == CommandKind.ChooseEffect);
                var regular = option.EligibleIds.First(id => view.Units.Single(u => u.Id == id).Kind == UnitKind.Regular);
                var elite = option.EligibleIds.First(id => view.Units.Single(u => u.Id == id).Kind == UnitKind.Elite);
                await room.Settle();
                Click(room.Fp, "effect-choice-" + regular);
                Assert.IsFalse(room.Fp.Board.Tray.Q<Button>("confirm-decision").enabledSelf);
                Click(room.Fp, "effect-choice-" + elite);
                await room.Until(() => room.Fp.Board.Tray.Q<Button>("confirm-decision").enabledSelf);
                Assert.IsTrue(room.Fp.Board.Map.DraftOverlay.PlannedRegions.Contains("grey-havens"));
                Assert.IsNull(room.Fp.Board.Map.Q<Label>("ghost-grey-havens"));
                var revision = room.Fp.Board.View.Revision;
                Click(room.Fp, "effect-choice-" + regular);
                Assert.IsFalse(room.Fp.Board.Tray.Q<Button>("confirm-decision").enabledSelf);
                Click(room.Fp, "effect-choice-" + regular);
                var connected = room.Fp.Connected;
                room.Fp.Connection.Reconnect();
                await room.Until(() => room.Fp.Connected > connected);
                await room.Settle();
                Assert.AreEqual(revision, room.Fp.Board.View.Revision);
                await room.Confirm(room.Fp);
                Assert.AreEqual(revision + 1, room.Sh.Board.View.Revision);
                Assert.AreEqual(2, room.Sh.Board.View.Units.Count(u => u.Location == "grey-havens" && u.Nation == "elves"));
                Assert.AreEqual(1, room.Sh.Board.View.Dice.Count(d => d.Location == DieLocation.Spent));
                Assert.IsNull(room.Sh.Board.Map.Q<Label>("ghost-grey-havens"));
            }
        }

        private static async Task Riders()
        {
            using (var room = await Connect("strategy-riders", ScenarioKind.CardPack))
            {
                room.Fp.Board.Map.Select("eastemnet");
                await room.Confirm(room.Fp);
                var view = room.Fp.Board.View;
                var elite = view.Options.Single(o => o.Kind == CommandKind.ChooseEffect).EligibleIds
                    .Single(id => view.Units.Single(u => u.Id == id).Kind == UnitKind.Elite);
                await room.Settle();
                Click(room.Fp, "effect-choice-" + elite);
                Assert.IsTrue(room.Fp.Board.Map.DraftOverlay.PlannedRegions.Contains("eastemnet"));
                await room.Confirm(room.Fp);
                var recruited = room.Sh.Board.View.Units.Where(u => u.Location == "eastemnet").ToArray();
                Assert.AreEqual(2, recruited.Length);
                Assert.IsTrue(recruited.All(u => u.Nation == "rohan"));
                Assert.IsTrue(recruited.Any(u => u.Kind == UnitKind.Leader));
            }
        }

        private static async Task Charge()
        {
            using (var room = await Connect("strategy-charge", ScenarioKind.CardPack))
            {
                var battle = room.Sh.Board.View.Battle;
                Assert.AreEqual(DecisionKind.Losses, room.Sh.Board.View.Decision.Kind);
                Assert.AreEqual(1, battle.SpecialAttacks.Single().Dice.Count(d => d.Hit));
                Assert.IsEmpty(battle.Defender.Dice);
                var victim = room.Sh.Board.View.Units.First(u => u.Location == "lossarnach" && u.Kind == UnitKind.Regular);
                Click(room.Sh, "loss-" + victim.Id);
                await room.Confirm(room.Sh);
                Assert.AreEqual("reserve", room.Fp.Board.View.Units.Single(u => u.Id == victim.Id).Location);
                Assert.AreEqual(1, room.Fp.Board.View.Battle.Defender.Dice.Count);
                Assert.AreEqual(1, room.Fp.Board.View.Battle.SpecialAttacks.Count);
            }
        }
    }
}
