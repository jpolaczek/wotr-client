using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    public sealed class FellowshipGuideNetworkTests
    {
        [UnityTest]
        public IEnumerator GuideModeRequiresAnExplicitChoiceAndRecoversOnlyOneCommand() => MatchTestRoom.Wait(Guide("ui34-guide-change", false));

        [UnityTest]
        public IEnumerator FinishPhaseKeepsTheGuideEvenAfterOpeningTheChoice() => MatchTestRoom.Wait(Guide("ui34-guide-keep", true));

        [UnityTest]
        public IEnumerator CouncilVersionAppearsToTheOpponentOnlyAfterChoosingItsGuide() => MatchTestRoom.Wait(Guide("ui34-guide-council", false));

        private static async Task Guide(string scenario, bool keep)
        {
            using var room = await UI10TestRoom.Connect(scenario);
            var board = room.Fp.Board;
            var before = board.View;
            var panel = board.Fellowship.Root;
            var candidate = before.Options.Single(o => o.Kind == CommandKind.SelectGuide).EligibleIds.First(id => id != before.Fellowship.Guide);
            var target = Portrait(board, candidate);
            PointerGestureEvents.ClickAt(target);
            Assert.AreEqual(0, room.Boundary.Commands, "Reading a portrait must not finish the phase.");
            Assert.IsFalse(target.ClassListContains("legal-target"));
            if (before.Configuration.Council)
            {
                StringAssert.Contains("Narya", target.tooltip);
                StringAssert.Contains("Version unrevealed", Portrait(room.Sh.Board, "gandalf").tooltip);
                StringAssert.DoesNotContain("Narya", Portrait(room.Sh.Board, "gandalf").tooltip);
                StringAssert.Contains("Fellowship Box", room.Sh.Board.Fellowship.Root.Q<Label>("fellowship-guide").tooltip);
            }
            PointerGestureEvents.ClickAt(panel.Q<Button>("fellowship-change-guide"));
            await room.Settle();
            Assert.AreEqual("Choosing a guide ends the Fellowship phase", panel.Q<Label>("fellowship-guide-hint").text);
            Assert.IsTrue(target.ClassListContains("legal-target"));
            var nonCandidate = Portrait(board, "base.character.gimli");
            Assert.IsFalse(nonCandidate.ClassListContains("legal-target"));
            Assert.IsTrue(nonCandidate.enabledInHierarchy);
            PointerGestureEvents.ClickAt(nonCandidate);
            Assert.AreEqual(0, room.Boundary.Commands);
            var pan = board.Map.Camera.Pan;
            await room.Fp.Connection.Refresh();
            await room.Settle();
            Assert.AreSame(target, Portrait(board, candidate));
            Assert.IsTrue(target.ClassListContains("legal-target"));

            room.Boundary.HoldNextCommand = true;
            PointerGestureEvents.ClickAt(keep ? panel.Q<Button>("fellowship-continue") : target);
            await room.Until(() => room.Boundary.CommandHeld);
            await room.At(before.Revision + 1);
            Assert.AreEqual(keep ? before.Fellowship.Guide : candidate, board.View.Fellowship.Guide);
            Assert.AreEqual(GamePhase.HuntAllocation, board.View.Phase);
            Assert.IsEmpty(room.Boundary.LastCommand.Payload.DieId);
            Assert.AreEqual(keep ? CommandKind.ContinueFellowship : CommandKind.SelectGuide, room.Boundary.LastCommand.Kind);
            if (before.Configuration.Council)
                StringAssert.Contains("Narya", Portrait(room.Sh.Board, "gandalf").tooltip);
            room.Boundary.ReleaseCommand();
            await room.Until(() => Recovery(board) != null);
            await room.Settle();
            // UI-162: the Hunt allocation belongs to the console, so the uncertain receipt shows in the decision window.
            Assert.AreSame(board.DecisionWindow.Dock, board.Tray.parent);
            Assert.IsTrue(board.DecisionWindow.IsOpen, "The uncertain receipt must be reachable after ownership passes to Hunt allocation.");
            PointerGestureEvents.ClickAt(board.DecisionWindow.Root.Q<Button>("close-decision"));
            await room.Fp.Connection.Refresh();
            await room.Settle();
            Assert.IsFalse(board.DecisionWindow.IsOpen, "The same pending receipt must not reopen a window the player closed.");
            PointerGestureEvents.ClickAt(board.Console.Status.Plaque);
            await room.Settle();
            Assert.IsTrue(board.DecisionWindow.IsOpen, "The console's status reopens it.");
            PointerGestureEvents.ClickAt(Recovery(board));
            await room.Until(() => Recovery(board) == null && board.Tray.enabledInHierarchy);
            Assert.AreEqual(1, room.Boundary.Commands);
            Assert.Greater(room.Boundary.ReceiptLookups, 0);
            Assert.AreEqual(pan, board.Map.Camera.Pan);
        }

        [UnityTest]
        public IEnumerator SeparationStaysInsideTheSiegeAndRetainsThePaidGuideContinuation() => MatchTestRoom.Wait(Siege());

        private static async Task Siege()
        {
            using var room = await UI10TestRoom.Connect("ui34-separate-siege");
            var board = room.Fp.Board;
            var die = board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).Id;
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(die));
            board.Fellowship.Open();
            await room.Settle();
            foreach (var id in new[] { "base.character.gimli", "base.character.merry", "base.character.gimli", "base.character.gimli" })
            {
                PointerGestureEvents.ClickAt(Portrait(board, id));
                await room.Settle();
            }
            await room.Settle();
            Assert.AreEqual("Separated companions cannot rejoin", board.Tray.Q<Label>("fellowship-separation-hint").text);
            CollectionAssert.AreEquivalent(new[] { "minas-tirith" }, board.Map.DraftOverlay.Targets);
            // UI-105: the destination click separates the companions at once.
            room.Boundary.HoldNextCommand = true;
            MapContourProbe.ClickRegion(board.Map, "minas-tirith");
            await room.Until(() => room.Boundary.CommandHeld);
            await room.At(1);
            Assert.AreEqual(DecisionKind.Guide, board.View.Decision.Kind);
            CollectionAssert.AreEqual(new[] { "minas-tirith" }, room.Boundary.LastCommand.Payload.Route);
            Assert.AreEqual(die, room.Boundary.LastCommand.Payload.DieId);
            foreach (var id in new[] { "base.character.gimli", "base.character.merry" })
            {
                var placed = board.View.Characters.Single(c => c.Id == id);
                Assert.AreEqual("minas-tirith", placed.Location);
                Assert.IsTrue(placed.Garrison);
            }
            Assert.AreEqual(DieLocation.Resolving, board.View.Dice.Single(d => d.Id == die).Location);
            room.Boundary.ReleaseCommand();
            await room.Until(() => Recovery(board) != null);
            await room.Settle();
            PointerGestureEvents.ClickAt(Recovery(board));
            await room.Until(() => Recovery(board) == null && board.Tray.enabledInHierarchy);
            await room.Settle();
            Assert.AreEqual(1, room.Boundary.Commands);
            PointerGestureEvents.ClickAt(Portrait(board, "base.character.strider"));
            await room.At(2);
            Assert.AreEqual(DieLocation.Spent, board.View.Dice.Single(d => d.Id == die).Location);
            Assert.AreEqual(1, board.View.Dice.Count(d => d.Location == DieLocation.Spent));
        }

        [UnityTest]
        public IEnumerator ForcedMordorSeparationRemovesTheGroupAndLetsTheServerSupplyGollum() => MatchTestRoom.Wait(Mordor());

        private static async Task Mordor()
        {
            using var room = await UI10TestRoom.Connect("ui34-separate-mordor");
            var board = room.Fp.Board;
            var ids = board.View.Options.Single(o => o.Kind == CommandKind.SeparateCompanions).EligibleIds.ToArray();
            foreach (var id in ids)
                PointerGestureEvents.ClickAt(Portrait(board, id));
            await room.Until(() => board.Tray.Q<Button>("confirm-decision")?.enabledInHierarchy == true);
            await room.Settle();
            Assert.AreEqual("Remove from play", board.Tray.Q<Button>("confirm-decision").text);
            Assert.IsEmpty(board.Map.DraftOverlay.Targets);
            Assert.IsNull(board.Tray.Q<Label>("travel-summary"));
            PointerGestureEvents.ClickAt(board.Tray.Q<Button>("confirm-decision"));
            await room.At(1);
            Assert.IsEmpty(room.Boundary.LastCommand.Payload.Route);
            Assert.IsEmpty(room.Boundary.LastCommand.Payload.DieId);
            Assert.AreEqual(0, board.View.Fellowship.CompanionCount);
            Assert.AreEqual("base.character.gollum", board.View.Fellowship.Guide);
            Assert.IsFalse(board.View.Characters.Any(c => c.Id == "base.character.gollum"));
            Assert.IsFalse(board.View.Characters.Any(c => ids.Contains(c.Id) && board.Presentation.Regions.ContainsKey(c.Location)));
            StringAssert.Contains("standard numbered", board.Fellowship.Root.Q<Label>("fellowship-guide").tooltip);
        }

        private static Button Portrait(BoardScreen board, string id) => board.Fellowship.Root.Q<Button>("fellowship-portrait-" + id);
        private static Button Recovery(BoardScreen board) => board.Tray.Query<Button>().ToList().FirstOrDefault(b => b.text == "Check result");
    }
}
