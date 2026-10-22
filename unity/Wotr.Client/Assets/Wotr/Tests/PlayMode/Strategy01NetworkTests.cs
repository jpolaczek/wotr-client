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
    public class Strategy01NetworkTests
    {
        [UnityTest]
        public IEnumerator ProtectionUsesTwoHandCardsAndOneDieInOneCommit() => Wait(RemoveProtection());
        [UnityTest]
        public IEnumerator BookMovesIndependentGroupsAndSurvivesReconnect() => Wait(Book());
        [UnityTest]
        public IEnumerator SpiritChoosesItsArmyOnTheMap() => Wait(Spirit());
        [UnityTest]
        public IEnumerator RangersRecruitsArmyChoiceAndLeaderAfterItsRoll() => Wait(Rangers());
        [UnityTest]
        public IEnumerator ScoutsSheltersBeforeRollingAndHandsAdvanceToAttacker() => Wait(Scouts());

        private static async Task RemoveProtection()
        {
            using (var room = await Connect("strategy-protection", ScenarioKind.CardPack))
            {
                var revision = room.Sh.Board.View.Revision;
                Assert.AreEqual(6, room.Sh.Board.Presentation.ProtectedRegions.Count);
                var cost = room.Sh.Board.View.Options.Single(o => o.Kind == CommandKind.DiscardProtection).EligibleDice[0];
                await ComponentGestures.Cost(room, room.Sh, cost);
                await ComponentGestures.Remove(room.Sh, "base.fp.strategy.02");
                await CardGestures.Open(room.Sh);
                // UI-222: the second cost card completes the removal at once, without ✓.
                foreach (var card in new[] { "base.sh.strategy.01", "base.sh.character.01" })
                    CardNetworkTests.Click(room.Sh.Board.Root.Q<Button>(card));
                await room.At(revision + 1);
                Assert.AreEqual(3, room.Fp.Board.View.Effects.Count);
                Assert.IsFalse(room.Fp.Board.Presentation.ProtectedRegions.Contains("lorien"));
                Assert.IsTrue(room.Fp.Board.Presentation.ProtectedRegions.Contains("the-shire"));
                Assert.AreEqual(DieLocation.Spent, room.Fp.Board.View.Dice.Single(d => d.Id == cost).Location);
                Assert.AreEqual(1, room.Sh.Board.View.Hand.Count);
            }
        }

        private static async Task Book()
        {
            using (var room = await Connect("strategy-book", ScenarioKind.CardPack))
            {
                await room.Settle();
                var board = room.Fp.Board;
                // UI-294: the journey is made on the map; the window lists no companions, groups, Stay all or Stay here.
                foreach (var name in new[] { "action-SeparateCompanions", "companion-base.character.gimli", "travel-stay-all", "travel-stay" })
                    Assert.IsNull(board.Root.Q<Button>(name), name);
                Assert.IsNull(board.Root.Q<Label>("companion-group-progress"));
                Assert.IsFalse(board.DecisionWindow.IsOpen, "Only the console's ✓ finishes the journey.");
                Assert.IsFalse(board.Fellowship.IsOpen, "The Fellowship panel does not host it.");
                StringAssert.Contains("Click a lit companion", board.Console.Status.Hint.text);
                var revision = board.View.Revision;
                // Gimli stands alone in the Iron Hills: a click on his region chooses him; Erebor is his destination.
                FellowshipGestures.Region(room.Fp, "iron-hills");
                await room.Settle();
                StringAssert.Contains("Click a highlighted destination", board.Console.Status.Hint.text);
                FellowshipGestures.Destination(room.Fp, "erebor");
                await room.Settle();
                Assert.AreEqual(revision, board.View.Revision, "A journey of several companions waits for ✓.");
                CollectionAssert.AreEqual(new[] { "erebor" }, board.Map.PlannedJourneys.Regions.ToArray(), "Gimli stands faintly at Erebor.");
                StringAssert.Contains("Click a lit companion", board.Console.Status.Hint.text, "Gimli is released for the next pick.");
                LayoutTests.Save(room.Fp.Viewport, "ui294-book-journey.png");
                // Legolas stays in Dale without any click.
                await Reconnect(room, room.Fp);
                await room.Confirm(room.Fp);
                Assert.AreEqual("erebor", room.Sh.Board.View.Characters.Single(c => c.Id == "base.character.gimli").Location);
                Assert.AreEqual("dale", room.Sh.Board.View.Characters.Single(c => c.Id == "base.character.legolas").Location);
                Assert.AreEqual(0, room.Sh.Board.View.Nations.Single(n => n.Id == "dwarves").StepsToWar);
                Assert.AreEqual(1, room.Sh.Board.View.Dice.Count(d => d.Location == DieLocation.Spent));
            }
        }

        private static async Task Spirit()
        {
            using (var room = await Connect("strategy-spirit", ScenarioKind.CardPack))
            {
                Assert.AreEqual(2, room.Fp.Board.View.Options.Single().EligibleIds.Count);
                MapContourProbe.ClickRegion(room.Fp.Board.Map, "orthanc");
                await room.Confirm(room.Fp);
                if (room.Sh.Board.View.EventLoss != null)
                {
                    var view = room.Sh.Board.View;
                    var hits = view.EventLoss.Hits;
                    foreach (var unit in view.Units.Where(u => view.EventLoss.ParticipantIds.Contains(u.Id) && u.Kind is UnitKind.Regular or UnitKind.Elite))
                    {
                        if (hits <= 0)
                            break;
                        Click(room.Sh, "loss-" + unit.Id);
                        if (unit.Kind == UnitKind.Elite && hits >= 2)
                            Click(room.Sh, "loss-" + unit.Id);
                        hits -= unit.Kind == UnitKind.Elite && hits >= 2 ? 2 : 1;
                    }
                    await Reconnect(room, room.Sh);
                    await room.Confirm(room.Sh);
                }
                Assert.AreEqual("orthanc", room.Fp.Board.View.Characters.Single(c => c.DefinitionId == "base.character.witch-king").Location);
                Assert.IsTrue(room.Fp.Board.View.Units.Any(u => u.Location == "orthanc" && u.Kind == UnitKind.Nazgul));
            }
        }

        private static async Task Rangers()
        {
            using (var room = await Connect("strategy-rangers", ScenarioKind.CardPack))
            {
                var view = room.Fp.Board.View;
                var id = view.Options.Single().EligibleIds.Single(id => view.Units.Single(u => u.Id == id).Kind == UnitKind.Elite);
                Click(room.Fp, "effect-choice-" + id);
                await room.Confirm(room.Fp);
                Assert.AreEqual("osgiliath", room.Sh.Board.View.Units.Single(u => u.Id == id).Location);
                Assert.IsTrue(room.Sh.Board.View.Units.Any(u => u.Location == "osgiliath" && u.Kind == UnitKind.Leader));
            }
        }

        private static async Task Scouts()
        {
            using (var room = await Connect("strategy-scouts", ScenarioKind.CardPack))
            {
                Assert.IsEmpty(room.Fp.Board.View.Battle.Attacker.Dice);
                Click(room.Fp, "effect-choice-scouts.shelter");
                await Reconnect(room, room.Fp);
                await room.Confirm(room.Fp);
                Assert.AreEqual(DecisionKind.BattleAdvance, room.Sh.Board.View.Decision.Kind);
                await room.SelectAllAdvancing(room.Sh);
                Assert.IsTrue(room.Fp.Board.View.Units.Single(u => u.Location == "helms-deep" && u.Nation == "rohan").Garrison);
            }
        }

        private static async Task Reconnect(MatchTestRoom room, MountedMatchClient client)
        {
            var before = client.Connected;
            client.Connection.Reconnect();
            await room.Until(() => client.Connected > before);
            await room.Settle();
        }
    }
}
