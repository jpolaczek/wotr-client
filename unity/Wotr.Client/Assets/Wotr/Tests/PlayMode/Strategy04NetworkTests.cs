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
    public class Strategy04NetworkTests
    {
        [UnityTest]
        public IEnumerator SwordsCommitsBothRegionsTogetherAndReconnectKeepsChoices() => Wait(Swords());
        [UnityTest]
        public IEnumerator EomerMapExcludesBesiegedGarrisonAndRecruitsAtSelectedTown() => Wait(Eomer());
        [UnityTest]
        public IEnumerator ValourShowsItsBonusAndUnmodifiedLeaderReroll() => Wait(Valour());

        private static async Task Swords()
        {
            using (var room = await Connect("strategy-swords", ScenarioKind.CardPack))
            {
                var view = room.Fp.Board.View;
                var handSize = view.Hand.Count;
                var option = view.Options.Single(o => o.Kind == CommandKind.ChooseEffect);
                var north = option.EligibleIds.Single(id => view.Units.Any(u => u.Id == id && u.Nation == "north" && u.Kind == UnitKind.Elite));
                var dwarves = option.EligibleIds.Single(id => view.Units.Any(u => u.Id == id && u.Nation == "dwarves" && u.Kind == UnitKind.Regular));
                Click(room.Fp, "effect-choice-" + north);
                Assert.IsFalse(room.Fp.Board.Tray.Q<Button>("confirm-decision").enabledSelf);
                Click(room.Fp, "effect-choice-" + dwarves);
                Assert.IsTrue(room.Fp.Board.Map.DraftOverlay.PlannedRegions.Contains("the-shire"));
                Assert.IsTrue(room.Fp.Board.Map.DraftOverlay.PlannedRegions.Contains("ered-luin"));
                var revision = view.Revision;
                var connected = room.Fp.Connected;
                room.Fp.Connection.Reconnect();
                await room.Until(() => room.Fp.Connected > connected);
                await room.Settle();
                Assert.AreEqual(revision, room.Fp.Board.View.Revision);
                await room.Confirm(room.Fp);
                Assert.AreEqual(revision + 1, room.Sh.Board.View.Revision);
                Assert.AreEqual("the-shire", room.Sh.Board.View.Units.Single(u => u.Id == north).Location);
                Assert.AreEqual("ered-luin", room.Sh.Board.View.Units.Single(u => u.Id == dwarves).Location);
                Assert.AreEqual(handSize + 1, room.Fp.Board.View.Hand.Count);
                Assert.AreEqual(1, room.Sh.Board.View.Dice.Count(d => d.Location == DieLocation.Spent));
            }
        }

        private static async Task Eomer()
        {
            using (var room = await Connect("strategy-eomer", ScenarioKind.CardPack))
            {
                var option = room.Fp.Board.View.Options.Single(o => o.Kind == CommandKind.ChooseEffect);
                CollectionAssert.DoesNotContain(option.EligibleIds, "helms-deep");
                Assert.IsFalse(room.Fp.Board.Map.DraftOverlay.Targets.Contains("helms-deep"));
                room.Fp.Board.Map.Select("westemnet");
                await room.Confirm(room.Fp);
                await room.Settle();
                var view = room.Fp.Board.View;
                var recruit = view.Options.Single(o => o.Kind == CommandKind.ChooseEffect).EligibleIds
                    .Single(id => view.Units.Single(u => u.Id == id).Kind == UnitKind.Elite);
                Click(room.Fp, "effect-choice-" + recruit);
                await room.Confirm(room.Fp);
                Assert.AreEqual(2, room.Sh.Board.View.Units.Count(u => u.Location == "westemnet" && u.Nation == "rohan"));
                Assert.IsTrue(room.Sh.Board.View.Units.Any(u => u.Location == "helms-deep" && u.Side == Side.FreePeoples && u.Garrison));
            }
        }

        private static async Task Valour()
        {
            using (var room = await Connect("strategy-valour", ScenarioKind.CardPack))
            {
                var die = room.Fp.Board.View.Battle.Attacker.Dice.Single();
                Assert.AreEqual(1, die.Modifier);
                Assert.IsFalse(die.Hit);
                Assert.That(room.Fp.Board.Tray.Q<Button>("battle-die-" + die.Id).tooltip, Does.Contain("Modifier 1"));
                Click(room.Fp, "battle-die-" + die.Id);
                await room.Confirm(room.Fp);
                await room.Confirm(room.Sh);
                die = room.Fp.Board.View.Battle.Attacker.Dice.Single();
                Assert.IsTrue(die.Reroll.HasValue);
                Assert.AreEqual(0, die.RerollModifier);
                Assert.That(room.Fp.Board.Tray.Q<Button>("battle-die-" + die.Id).tooltip, Does.Contain("Leader reroll"));
            }
        }
    }
}
