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
    public class Strategy02NetworkTests
    {
        [UnityTest]
        public IEnumerator MarchUsesTwoMapStepsAndRetainsRouteOnReconnect() => Wait(March());
        [UnityTest]
        public IEnumerator WosesShowsGarrisonAndLossOfLeftLeader() => Wait(Woses());
        [UnityTest]
        public IEnumerator ReliefStartsBattleWithoutPayingAnotherDie() => Wait(Relief());
        [UnityTest]
        public IEnumerator ConfusionLeavesNaturalOnesUnavailableForReroll() => Wait(Confusion());

        private static async Task March()
        {
            using (var room = await Connect("strategy-march", ScenarioKind.CardPack))
            {
                room.Fp.Board.Map.Select("north-ithilien");
                await room.Settle();
                MapContourProbe.ClickRegion(room.Fp.Board.Map, "minas-morgul");
                await room.Settle();
                MapContourProbe.ClickRegion(room.Fp.Board.Map, "gorgoroth");
                await Reconnect(room);
                // UI-216: the planned route survives a reconnect on the map; no window opens.
                Assert.IsTrue(room.Fp.Board.Map.DraftOverlay.PlannedRegions.Contains("gorgoroth"));
                Assert.IsFalse(room.Fp.Board.DecisionWindow.IsOpen);
                await room.Confirm(room.Fp);
                Assert.AreEqual(Side.FreePeoples, room.Sh.Board.View.Control["minas-morgul"]);
                Assert.AreEqual("gorgoroth", room.Sh.Board.View.Characters.Single(c => c.Id == "base.character.gimli").Location);
                Assert.AreEqual(1, room.Sh.Board.View.Dice.Count(d => d.Location == DieLocation.Spent));
            }
        }

        private static async Task Woses()
        {
            using (var room = await Connect("strategy-woses", ScenarioKind.CardPack))
            {
                room.Fp.Board.Map.Select("helms-deep");
                await room.Settle();
                var leader = room.Fp.Board.View.Units.Single(u => u.Location == "helms-deep" && u.Kind == UnitKind.Leader);
                Click(room.Fp, "figure-" + leader.Id);
                Click(room.Fp, "figure-base.character.legolas");
                await room.Settle();
                MapContourProbe.ClickRegion(room.Fp.Board.Map, "minas-tirith");
                await room.Until(() => room.Fp.Board.Tray.Query<Label>().ToList().Any(l => l.text.Contains("Leaders left without an army")));
                await Reconnect(room);
                await room.Confirm(room.Fp);
                Assert.AreEqual(Side.Shadow, room.Sh.Board.View.Control["helms-deep"]);
                Assert.AreEqual("casualties", room.Sh.Board.View.Units.Single(u => u.Id == leader.Id).Location);
                Assert.AreEqual("helms-deep", room.Sh.Board.View.Characters.Single(c => c.Id == "base.character.legolas").Location);
                Assert.IsFalse(room.Sh.Board.View.Characters.Single(c => c.Id == "base.character.legolas").Garrison);
            }
        }

        private static async Task Relief()
        {
            using (var room = await Connect("strategy-relief", ScenarioKind.CardPack))
            {
                room.Fp.Board.Map.Select("osgiliath");
                await room.Settle();
                MapContourProbe.ClickRegion(room.Fp.Board.Map, "minas-tirith");
                await room.Confirm(room.Fp);
                Assert.AreEqual(BattleMode.Relief, room.Sh.Board.View.Battle.Mode);
                Assert.AreEqual(3, room.Sh.Board.View.Battle.Defender.CombatDice);
                Assert.AreEqual(2, room.Sh.Board.View.Battle.Defender.CombatDiceReduction);
                Assert.That(room.Fp.Board.Tray.Q<Button>("battle-card-defender").tooltip, Does.Contain("Help Unlooked For"));
                Assert.AreEqual(1, room.Sh.Board.View.Dice.Count(d => d.Location == DieLocation.Resolving));
                await Reconnect(room);
                Assert.AreEqual(3, room.Fp.Board.View.Battle.Defender.CombatDice);
            }
        }

        private static async Task Confusion()
        {
            using (var room = await Connect("strategy-confusion", ScenarioKind.CardPack))
            {
                Assert.AreEqual(1, room.Fp.Board.View.Battle.Attacker.Hits);
                Assert.AreEqual(1, room.Fp.Board.View.Battle.Defender.Dice.Count(d => d.CannotReroll));
                await room.Confirm(room.Fp);
                var option = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.RerollBattle);
                Assert.AreEqual(1, option.EligibleIds.Count);
                Assert.IsFalse(option.EligibleIds.Contains(room.Sh.Board.View.Battle.Defender.Dice.Single(d => d.CannotReroll).Id));
            }
        }

        private static async Task Reconnect(MatchTestRoom room)
        {
            var before = room.Fp.Connected;
            room.Fp.Connection.Reconnect();
            await room.Until(() => room.Fp.Connected > before);
            await room.Settle();
        }
    }
}
