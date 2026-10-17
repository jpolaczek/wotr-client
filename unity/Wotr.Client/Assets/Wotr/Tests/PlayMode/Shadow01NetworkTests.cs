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
    public class Shadow01NetworkTests
    {
        [UnityTest]
        public IEnumerator PatrolUsesProtectionAndDamageInTheSameTray() => Wait(Patrol());
        [UnityTest]
        public IEnumerator BaneKeepsReplacementAndAppliesDirectCorruptionAfterReconnect() => Wait(Bane());
        [UnityTest]
        public IEnumerator CruelForfeitsTwoPointsFromTheChosenFigureInOneCommand() => Wait(Cruel());

        private static async Task Patrol()
        {
            using (var room = await Connect("shadow-patrol", ScenarioKind.CardPack))
            {
                Assert.IsFalse(room.Fp.Board.View.Hunt.CorruptionOnly);
                Assert.IsNull(room.Sh.Board.View.Decision);
                await room.Direct(room.Fp, "action-DeclineReaction");
                Assert.AreEqual("hunt.table", room.Fp.Board.View.Decision.Window);
                await room.Settle();
                Click(room.Fp, "effect-choice-card.axe-and-bow");
                await room.Confirm(room.Fp);
                Assert.AreEqual(2, room.Sh.Board.View.Hunt.Damage);
                var damageRevision = room.Fp.Board.View.Revision;
                Click(room.Fp, "hunt-response-Ring");
                await room.At(damageRevision + 1);
                Assert.AreEqual(2, room.Sh.Board.View.Fellowship.Corruption);
                Assert.IsTrue(room.Fp.Board.View.Fellowship.Hidden);
                Assert.IsNull(room.Sh.Board.View.Hunt);
                Assert.AreEqual(1, room.Sh.Board.View.Dice.Count(d => d.Owner == Side.Shadow && d.Location == DieLocation.Spent));
            }
        }

        private static async Task Bane()
        {
            using (var room = await Connect("shadow-bane", ScenarioKind.CardPack))
            {
                Assert.IsTrue(room.Fp.Board.View.Hunt.CorruptionOnly);
                Assert.That(room.Fp.Board.Hunt.Tile.Root.tooltip, Does.Contain("damage reduction"));
                var connected = room.Fp.Connected;
                room.Fp.Connection.Reconnect();
                await room.Until(() => room.Fp.Connected > connected);
                await room.Settle();
                Assert.AreEqual("hunt.replace", room.Fp.Board.View.Decision.Window);
                Assert.IsTrue(room.Fp.Board.View.Hunt.CorruptionOnly);
                var companions = room.Fp.Board.View.Fellowship.CompanionCount;
                await room.Direct(room.Fp, "action-DeclineReaction");
                Assert.AreEqual(3, room.Sh.Board.View.Fellowship.Corruption);
                Assert.AreEqual(companions, room.Sh.Board.View.Fellowship.CompanionCount);
                Assert.IsNull(room.Fp.Board.View.Hunt);
                Assert.AreEqual(DisplayStyle.None, room.Fp.Board.Hunt.Responses.Root.style.display.value);
            }
        }

        private static async Task Cruel()
        {
            using (var room = await Connect("shadow-cruel", ScenarioKind.CardPack))
            {
                var option = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.ChooseEffect);
                var points = option.EffectChoices.Where(c => c.SourceId == "base.character.witch-king").ToArray();
                Assert.AreEqual(2, points.Length);
                Assert.IsTrue(room.Sh.Board.Tray.Query<Label>().ToList().Any(label => label.text.Contains("Forfeit 2 Leadership")));
                Assert.IsNull(room.Fp.Board.View.Decision);
                var revision = room.Sh.Board.View.Revision;
                Click(room.Sh, "effect-choice-" + points[0].Id);
                Assert.IsFalse(room.Sh.Board.Tray.Q<Button>("confirm-decision").enabledSelf);
                Assert.AreEqual("WK ◆", room.Sh.Board.Tray.Q<Button>("effect-choice-" + points[0].Id).text);
                Click(room.Sh, "effect-choice-" + points[1].Id);
                var connected = room.Sh.Connected;
                room.Sh.Connection.Reconnect();
                await room.Until(() => room.Sh.Connected > connected);
                await room.Settle();
                Assert.AreEqual(revision, room.Sh.Board.View.Revision);
                Assert.IsTrue(room.Sh.Board.Tray.Q<Button>("effect-choice-" + points[1].Id).ClassListContains("selected"));
                await room.Confirm(room.Sh);
                Assert.AreEqual(revision + 1, room.Fp.Board.View.Revision);
                Assert.AreEqual(3, room.Fp.Board.View.Battle.Defender.Leadership);
                Assert.AreEqual(2, room.Fp.Board.View.Battle.Defender.ForfeitedLeadership["base.character.witch-king"]);
                Assert.That(room.Fp.Board.Tray.Q<Button>("battle-card-defender").tooltip, Does.Contain("2 Leadership forfeited"));
                Assert.IsTrue(room.Fp.Board.View.Battle.Defender.Dice.All(d => d.Modifier == 1));
            }
        }
    }
}
