using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;
using static Wotr.PlayTests.KingsCardNetworkChecks;

namespace Wotr.PlayTests
{
    public class KingsCards02NetworkTests
    {
        [UnityTest] public IEnumerator VillagesCommitsFourNationsWithOneClickAfterReconnect() => Wait(Villages());
        [UnityTest] public IEnumerator ServiceChoosesSovereignThenRecruitsAndCleanses() => Wait(Service());
        [UnityTest] public IEnumerator WelcomeCanBeRemovedUsingAnEligibleDie() => Wait(Welcome());
        [UnityTest] public IEnumerator WestSelectsItsTargetWithoutSpendingAnEye() => Wait(WestTarget());
        [UnityTest] public IEnumerator WestDrawsAndKeepsTheFinalChoicePrivateAcrossReconnect() => Wait(WestDraw());
        [UnityTest] public IEnumerator HopeChoosesWhichInitialSovereignAwakensDuringBattle() => Wait(Hope());
        [UnityTest] public IEnumerator VainAllowsZeroOrSelectedLeadershipWithAccuratePreview() => Wait(Vain());

        private static async Task Villages()
        {
            using (var room = await Connect("kings-villages", ScenarioKind.CardPack))
            {
                var revision = room.Fp.Board.View.Revision;
                var groups = room.Fp.Board.View.Options.Single().Recruitment.Where(g => g.Kind == UnitKind.Regular).Take(4).ToArray();
                foreach (var group in groups)
                {
                    Click(room.Fp, "event-reserve-" + group.Nation + "-Regular");
                    MapContourProbe.ClickRegion(room.Fp.Board.Map, group.Targets[0]);
                    await room.Settle();
                }
                Assert.AreEqual(revision, room.Fp.Board.View.Revision);
                await Layout(room, room.Fp, "villages");
                await Reconnect(room, room.Fp);
                await room.Confirm(room.Fp);
                foreach (var group in groups)
                    Assert.IsTrue(room.Sh.Board.View.Units.Any(u => u.Nation == group.Nation && u.Kind == UnitKind.Regular && u.Location == group.Targets[0]));
            }
        }

        private static async Task Service()
        {
            using (var room = await Connect("kings-service", ScenarioKind.CardPack))
            {
                await Layout(room, room.Fp, "service-sovereign");
                await ChooseKing(room, room.Fp, "kings.sovereign.dain");
                Click(room.Fp, "event-reserve-dwarves-Elite");
                MapContourProbe.ClickRegion(room.Fp.Board.Map, "erebor");
                await Layout(room, room.Fp, "service-recruit");
                await Reconnect(room, room.Fp);
                await room.Confirm(room.Fp);
                Assert.AreEqual(0, room.Sh.Board.View.Sovereigns.Single(s => s.Id == "kings.sovereign.dain").Tiles.Count);
                Assert.IsTrue(room.Sh.Board.View.Units.Any(u => u.Nation == "dwarves" && u.Kind == UnitKind.Elite && u.Location == "erebor"));
            }
        }

        private static async Task Welcome()
        {
            using (var room = await Connect("kings-welcome", ScenarioKind.CardPack))
            {
                await room.Settle();
                var die = room.Fp.Board.View.Options.Single(o => o.Kind == CommandKind.DiscardWelcome).EligibleDice.Single();
                await ComponentGestures.Cost(room, room.Fp, die);
                await Layout(room, room.Fp, "welcome");
                await Reconnect(room, room.Fp);
                var revision = room.Fp.Board.View.Revision;
                await ComponentGestures.Remove(room.Fp, "kings.sh.character.25");
                await room.At(revision + 1);
                Assert.IsFalse(room.Sh.Board.View.Options.Any(o => o.Kind == CommandKind.DiscardWelcome));
                Assert.AreEqual(DieLocation.Spent, room.Sh.Board.View.Dice.Single(d => d.Id == die).Location);
                Assert.IsFalse(room.Sh.Board.View.Players.Single(p => p.Side == Side.Shadow).TableCards.Contains("kings.sh.character.25"));
            }
        }

        private static async Task WestTarget()
        {
            using (var room = await Connect("kings-west", ScenarioKind.CardPack))
            {
                await Layout(room, room.Sh, "west-target");
                await ChooseKing(room, room.Sh, "kings.sovereign.dain");
                Assert.AreEqual(2, room.Sh.Board.View.Dice.Count(d => d.Location == DieLocation.Hunt && d.Face == DieFace.Eye));
                Assert.AreEqual(1, room.Sh.Board.View.CorruptionAttempt.DrawnCount);
                Assert.IsNull(room.Fp.Board.View.Decision);
            }
        }

        private static async Task WestDraw()
        {
            using (var room = await Connect("kings-west-draw", ScenarioKind.CardPack))
            {
                // UI-279: each draw is the console's Draw tiles; nothing is drawn by itself.
                await DrawTiles(room, room.Sh);
                Assert.AreEqual(2, room.Sh.Board.View.CorruptionAttempt.OwnChoices.Count);
                var choices = room.Sh.Board.View.CorruptionAttempt.OwnChoices;
                await room.Until(() => room.Fp.Board.View.Revision == room.Sh.Board.View.Revision);
                Assert.IsEmpty(room.Fp.Board.View.CorruptionAttempt.OwnChoices);
                var tile = choices.First(t => t.Value == 1).TileId;
                Assert.IsNull(room.Fp.Board.Root.Q<Button>("corruption-choice-" + tile), "The Free Peoples see the drawn tiles' backs.");
                await Layout(room, room.Sh, "west-choice");
                await Reconnect(room, room.Sh);
                var revision = room.Sh.Board.View.Revision;
                var button = await InspectedTile(room, room.Sh, "kings.sovereign.dain", tile);
                Assert.IsNotNull(button, "The drawn tile stands beside Dáin.");
                PointerGestureEvents.ClickAt(button);
                await room.At(revision + 1);
                var hidden = room.Fp.Board.View.Sovereigns.Single(s => s.Id == "kings.sovereign.dain").Tiles.Single();
                Assert.IsNull(hidden.Value);
                Assert.AreEqual("", hidden.TileId);
                Assert.IsNull(room.Sh.Board.View.CorruptionAttempt);
                Assert.AreEqual(2, room.Fp.Board.View.Dice.Count(d => d.Location == DieLocation.Hunt && d.Face == DieFace.Eye));
            }
        }

        private static async Task Hope()
        {
            using (var room = await Connect("kings-hope", ScenarioKind.CardPack))
            {
                await Layout(room, room.Fp, "hope");
                await Reconnect(room, room.Fp);
                // UI-278 (D16): both kings take part in the attack, so they glow in the battle window's army, not by
                // their portraits.
                var theoden = await KingsBranchNetworkTests.BattleKing(room, room.Fp.Board, "battle-attacker", "battle-figure-rohan-Leader");
                StringAssert.StartsWith("Théoden · Awaken here if permitted", theoden.tooltip);
                Assert.IsNotNull(await KingsBranchNetworkTests.BattleKing(room, room.Fp.Board, "battle-attacker", "battle-figure-gondor-Leader"));
                // The portrait is read without unrolling the Characters panel, which would cover the attacker's column.
                Assert.IsFalse(room.Fp.Board.Sovereigns.Root.Q<Button>("sovereign-kings.sovereign.theoden").ClassListContains("legal-target"));
                var revision = room.Fp.Board.View.Revision;
                PointerGestureEvents.ClickAt(theoden);
                await room.At(revision + 1);
                Assert.AreEqual(SovereignStatus.Awakened, room.Sh.Board.View.Sovereigns.Single(s => s.Id == "kings.sovereign.theoden").Status);
                Assert.AreEqual(SovereignStatus.Initial, room.Sh.Board.View.Sovereigns.Single(s => s.Id == "kings.sovereign.denethor").Status);
                Assert.AreEqual("lossarnach", room.Sh.Board.View.Characters.Single(c => c.Id == "kings.sovereign.theoden").Location);
            }
        }

        private static async Task Vain()
        {
            using (var room = await Connect("kings-vain", ScenarioKind.CardPack))
            {
                await room.Until(() => room.Sh.Board.Tray.Q<Button>("confirm-decision")?.enabledSelf == true);
                var options = room.Sh.Board.View.Options.Single().EligibleIds;
                foreach (var id in options.Take(2))
                    Click(room.Sh, "effect-choice-" + id);
                await Layout(room, room.Sh, "vain");
                Assert.IsTrue(room.Sh.Board.Tray.Query<Label>().ToList().Any(l => l.text.Contains("Reduce opposing Leadership by 2")));
                await Reconnect(room, room.Sh);
                await room.Confirm(room.Sh);
                Assert.AreNotEqual("NazgulLeadership", room.Sh.Board.View.Decision?.Presentation);
            }
        }

    }
}
