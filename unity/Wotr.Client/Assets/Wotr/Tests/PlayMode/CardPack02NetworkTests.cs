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
    public class CardPack02NetworkTests
    {
        [UnityTest]
        public IEnumerator StaffPreventsAnUnseenTileAndAdvancesMordor() => Wait(Staff());
        [UnityTest]
        public IEnumerator AnotherWayMovesWithoutPuttingItsDieInTheHunt() => Wait(AnotherWay());
        [UnityTest]
        public IEnumerator AloneUsesMapRouteAndResumesHealingAfterGuide() => Wait(Alone());
        [UnityTest]
        public IEnumerator AndurilKeepsTwoDiceSelectedAcrossReconnect() => Wait(Anduril());
        [UnityTest]
        public IEnumerator HeroicRequiresACompatibleFigureAndHitCount() => Wait(Heroic());
        [UnityTest]
        public IEnumerator FatefulEliminatesMinionAndKeepsBothHits() => Wait(Fateful());

        private static async Task Staff()
        {
            using (var room = await Connect("staff", ScenarioKind.CardPack))
            {
                Assert.IsEmpty(room.Fp.Board.View.Hunt.TileId);
                Assert.IsTrue(room.Fp.Board.View.Hunt.DamagePending);
                await room.Settle();
                LayoutTests.Save(room.Fp.Viewport, "ui47-staff-reaction.png");
                var revision = room.Fp.Board.View.Revision;
                PointerGestureEvents.ClickAt(room.Fp.Board.Tray.Q<Button>("effect-choice-card.wizards-staff"));
                await room.At(revision + 1);
                Assert.AreEqual(1, room.Sh.Board.View.Fellowship.MordorStep);
                Assert.IsNull(room.Sh.Board.View.Hunt);
                Assert.AreEqual(0, room.Sh.Board.View.Fellowship.Corruption);
            }
        }

        private static async Task AnotherWay()
        {
            using (var room = await Connect("another-way", ScenarioKind.CardPack))
            {
                Assert.AreEqual(2, room.Fp.Board.View.Fellowship.Corruption);
                // UI-214: There is Another Way's choices are console buttons.
                Assert.IsNotNull(TrayOrConsole(room.Fp, "effect-choice-fellowship.stay"));
                Assert.IsFalse(room.Fp.Board.DecisionWindow.IsOpen);
                Click(room.Fp, "effect-choice-fellowship.move");
                await room.Confirm(room.Fp);
                Assert.AreEqual(1, room.Sh.Board.View.Fellowship.Progress);
                Assert.AreEqual(1, room.Fp.Board.View.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Spent));
                Assert.IsFalse(room.Fp.Board.View.Dice.Any(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Hunt));
            }
        }

        private static async Task Alone()
        {
            using (var room = await Connect("alone", ScenarioKind.CardPack))
            {
                var revision = room.Fp.Board.View.Revision;
                Click(room.Fp, "companion-base.character.merry");
                await room.Settle();
                Assert.IsNull(room.Fp.Board.Tray.Q<Button>("confirm-decision"));
                Assert.AreEqual(revision, room.Fp.Board.View.Revision, "Choosing the companion commits nothing.");
                // UI-105: Hollin lies two regions away; its click sends Merry there at once.
                await room.MoveTo(room.Fp, "hollin");
                Assert.AreEqual(3, room.Sh.Board.View.Fellowship.Corruption);
                Assert.AreEqual(DecisionKind.Guide, room.Fp.Board.View.Decision.Kind);
                Click(room.Fp, "companion-base.character.strider");
                await room.Confirm(room.Fp);
                Assert.AreEqual(2, room.Sh.Board.View.Fellowship.Corruption);
                Assert.AreEqual("hollin", room.Sh.Board.View.Characters.Single(c => c.Id == "base.character.merry").Location);
            }
        }

        private static async Task Anduril()
        {
            using (var room = await Connect("anduril", ScenarioKind.CardPack))
            {
                var ids = room.Fp.Board.View.Options.Single(o => o.Kind == CommandKind.ChooseEffect).EligibleIds.Take(3).ToList();
                Click(room.Fp, "effect-choice-" + ids[0]);
                Click(room.Fp, "effect-choice-" + ids[1]);
                Assert.IsFalse(room.Fp.Board.Tray.Q<Button>("effect-choice-" + ids[2]).enabledSelf);
                await Reconnect(room);
                Assert.IsTrue(room.Fp.Board.Tray.Q<Button>("effect-choice-" + ids[0]).ClassListContains("selected"));
                Assert.IsTrue(room.Fp.Board.Tray.Q<Button>("effect-choice-" + ids[1]).ClassListContains("selected"));
                await room.Confirm(room.Fp);
                Assert.AreEqual(2, room.Sh.Board.View.Battle.Attacker.Hits);
                Assert.AreEqual(2, room.Sh.Board.View.Battle.Attacker.Leadership);
            }
        }

        private static async Task Heroic()
        {
            using (var room = await Connect("heroic", ScenarioKind.CardPack))
            {
                const string three = "effect-choice-heroic-death.hits.3";
                Assert.IsFalse(room.Fp.Board.Tray.Q<Button>(three).enabledSelf);
                Click(room.Fp, "effect-choice-base.character.strider");
                Click(room.Fp, three);
                Click(room.Fp, "effect-choice-base.character.gimli");
                Assert.IsFalse(room.Fp.Board.Tray.Q<Button>(three).enabledSelf);
                Assert.IsFalse(room.Fp.Board.Tray.Q<Button>("confirm-decision").enabledSelf);
                Click(room.Fp, "effect-choice-heroic-death.hits.2");
                await Reconnect(room);
                await room.Confirm(room.Fp);
                Assert.IsFalse(room.Sh.Board.View.Characters.Any(c => c.Id == "base.character.gimli" && c.Location == "minas-tirith"));
                Assert.IsTrue(room.Sh.Board.View.Characters.Any(c => c.Id == "base.character.strider" && c.Location == "minas-tirith"));
                Assert.AreEqual(1, room.Fp.Board.View.Decision.RequiredCount);
            }
        }

        private static async Task Fateful()
        {
            using (var room = await Connect("fateful", ScenarioKind.CardPack))
            {
                Click(room.Fp, "effect-choice-base.character.witch-king");
                await room.Confirm(room.Fp);
                Assert.AreEqual(2, room.Sh.Board.View.Battle.Attacker.Hits);
                Assert.AreEqual(0, room.Sh.Board.View.Battle.Attacker.SpentHits);
                Assert.IsFalse(room.Sh.Board.View.Characters.Any(c => c.DefinitionId == "base.character.witch-king" && c.Location == "lossarnach"));
            }
        }

        private static async Task Reconnect(MatchTestRoom room)
        {
            var connected = room.Fp.Connected;
            room.Fp.Connection.Reconnect();
            await room.Until(() => room.Fp.Connected > connected);
            await room.Settle();
        }
    }
}
