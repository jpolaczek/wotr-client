using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.ReturnTestFixture;

namespace Wotr.PlayTests
{
    // UI-161: the Elven Ring gesture against the real server in the test scenario mode (fp-elven-ring, sh-elven-ring):
    // ring → die → result in a popover above the die, cancelling without a command, the Free Peoples ring passing
    // to the Shadow and the Shadow's captured ring turning a die into an Eye before it leaves the game.
    public sealed class RingUseNetworkTests
    {
        [UnityTest]
        public IEnumerator FreePeoplesRingChangesTheChosenDieAndPassesToTheShadow() => MatchTestRoom.Wait(FreePeoples());

        [UnityTest]
        public IEnumerator ShadowRingTurnsADieIntoAnEyeAndLeavesTheGame() => MatchTestRoom.Wait(Shadow());

        private static async Task FreePeoples()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(150));
            var token = timeout.Token;
            var fixture = Load();
            using var api = await fixture.Login(Side.FreePeoples, token);
            using var client = new MountedFlowClient(api, Path.Combine(Path.GetTempPath(), "wotr-ui161-" + Guid.NewGuid().ToString("N")));
            await Open(client, "fp-elven-ring", Side.FreePeoples, token);
            var board = client.Board;
            var view = board.View;
            var ring = view.Rings.First(r => r.Owner == Side.FreePeoples).Id;
            var die = view.Dice.First(d => d.Owner == Side.FreePeoples && d.Face == DieFace.Army).Id;
            var socket = board.Console.Rings.For(ring);
            var revision = view.Revision;

            // The ring again cancels; Escape cancels the chosen die too. Neither sends a command.
            PointerGestureEvents.ClickAt(socket);
            await Task.Delay(120, token);
            CheckDice(board, true);
            Assert.IsFalse(board.RingPicker.IsOpen, "No results before a die is chosen.");
            Assert.IsFalse(board.ResourceDecision.IsOpen, "The base ring needs no resource panel.");
            StringAssert.StartsWith("Click a highlighted die", board.Console.Status.Hint.text);
            Assert.IsTrue(socket.ClassListContains("selected") && socket.ClassListContains("legal-target"));
            TurnConsoleLayoutTests.AssertRoundRingOnly(socket, "armed");
            PointerGestureEvents.ClickAt(socket);
            await Task.Delay(120, token);
            CheckDice(board, false);
            PointerGestureEvents.ClickAt(socket);
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(die));
            await Task.Delay(120, token);
            CheckPicker(board, die, DieFace.Army);
            BoardMenuLayoutTests.Key(board.Root, true);
            BoardMenuLayoutTests.Key(board.Root, false);
            await Task.Delay(120, token);
            Assert.IsFalse(board.RingPicker.IsOpen, "Escape ends the gesture.");
            CheckDice(board, false);
            Assert.AreEqual(revision, board.View.Revision, "Cancelling sends nothing.");

            PointerGestureEvents.ClickAt(socket);
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(die));
            await Task.Delay(120, token);
            PointerGestureEvents.ClickAt(board.RingPicker.Root.Q<Button>("ring-face-muster"));
            await Until(() => board.View.Revision > revision, token);
            await Task.Delay(120, token);
            Assert.AreEqual(DieFace.Muster, board.View.Dice.Single(d => d.Id == die).Face);
            Assert.AreEqual(DieLocation.Available, board.View.Dice.Single(d => d.Id == die).Location, "The changed die is not forced into an action.");
            Assert.AreEqual(Side.Shadow, board.View.Rings.Single(r => r.Id == ring).Owner);
            Assert.AreEqual(Side.FreePeoples, board.View.DecisionOwner);
            Assert.IsTrue(board.Console.Rings.For(ring).ClassListContains("ring-captured"));
            TurnConsoleLayoutTests.AssertRoundRingOnly(board.Console.Rings.For(ring), "captured");
            Assert.IsFalse(board.RingPicker.IsOpen);
            CheckDice(board, false);
            await ClickReady(client.Root, "test-exit", token);
        }

        private static async Task Shadow()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(150));
            var token = timeout.Token;
            var fixture = Load();
            using var api = await fixture.Login(Side.FreePeoples, token);
            using var client = new MountedFlowClient(api, Path.Combine(Path.GetTempPath(), "wotr-ui161-" + Guid.NewGuid().ToString("N")));
            await Open(client, "sh-elven-ring", Side.Shadow, token);
            var board = client.Board;
            var view = board.View;
            var ring = view.Rings.First(r => r.Owner == Side.Shadow).Id;
            var die = view.Dice.First(d => d.Owner == Side.Shadow && d.Face == DieFace.Army).Id;
            var eyes = board.View.Dice.Count(d => d.Location == DieLocation.Hunt);
            var revision = view.Revision;
            PointerGestureEvents.ClickAt(board.Console.Rings.For(ring));
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(die));
            await Task.Delay(120, token);
            CheckPicker(board, die, DieFace.Army);
            Assert.IsNotNull(board.RingPicker.Root.Q<Button>("ring-face-eye"), "The Shadow may change a die into an Eye.");
            PointerGestureEvents.ClickAt(board.RingPicker.Root.Q<Button>("ring-face-eye"));
            await Until(() => board.View.Revision > revision, token);
            await Task.Delay(120, token);
            Assert.AreEqual(DieLocation.Hunt, board.View.Dice.Single(d => d.Id == die).Location, "The Eye joins the Hunt Box.");
            Assert.AreEqual(eyes + 1, board.View.Dice.Count(d => d.Location == DieLocation.Hunt));
            Assert.IsNull(board.View.Rings.Single(r => r.Id == ring).Owner, "The Shadow's ring leaves the game.");
            Assert.IsTrue(board.Console.Rings.For(ring).ClassListContains("ring-removed"));
            TurnConsoleLayoutTests.AssertRoundRingOnly(board.Console.Rings.For(ring), "removed");
            Assert.AreEqual(Side.Shadow, board.View.DecisionOwner);
            await ClickReady(client.Root, "test-exit", token);
        }

        private static async Task Open(MountedFlowClient client, string scenario, Side side, CancellationToken token)
        {
            await ClickReady(client.Root, "open-test-scenarios", token);
            await ClickReady(client.Root, "test-scenario-" + scenario, token);
            await Until(() => client.Board?.View != null && client.Board.View.You == side && client.Board.Connection.text == "Live", token);
            await Task.Delay(200, token);
        }

        // Only the dice the server lets the ring change glow; the others dim while the ring is armed.
        private static void CheckDice(BoardScreen board, bool armed)
        {
            var option = board.View.Options.Single(o => o.Kind == CommandKind.UseRing);
            foreach (var button in board.OwnDice.Query<Button>(className: "die").ToList().Where(b => b.userData is DraftToken))
            {
                var eligible = option.EligibleDice.Contains(button.name);
                Assert.AreEqual(armed && eligible, button.ClassListContains(RingInteraction.CandidateClass), button.name);
                Assert.AreEqual(armed && !eligible, button.ClassListContains(RingInteraction.MutedClass), button.name);
            }
        }

        // The popover stands above the dice panel (so it hides neither the die nor the panel's header), inside the
        // board, with the current result disabled.
        private static void CheckPicker(BoardScreen board, string die, DieFace current)
        {
            var picker = board.RingPicker.Root;
            var anchor = board.OwnDice.Q<Button>(die).worldBound;
            Assert.IsTrue(board.RingPicker.IsOpen);
            Assert.LessOrEqual(picker.worldBound.yMax, anchor.yMin + .5f, "The popover never covers its die.");
            Assert.LessOrEqual(picker.worldBound.yMax, board.OwnDice.worldBound.yMin + .5f, "Nor the dice panel and its help links.");
            Assert.That(anchor.center.x, Is.InRange(picker.worldBound.xMin, picker.worldBound.xMax), "The popover stands over the die's column.");
            Assert.GreaterOrEqual(picker.worldBound.xMin + .5f, board.Root.worldBound.xMin);
            Assert.LessOrEqual(picker.worldBound.xMax, board.Root.worldBound.xMax + .5f);
            var faces = picker.Query<Button>(className: "ring-face").ToList();
            Assert.IsNotEmpty(faces);
            foreach (var face in faces)
            {
                Assert.GreaterOrEqual(face.worldBound.height + .25f, 32, face.name);
                Assert.AreEqual(face.name != "ring-face-" + DieFaceIcon.Key(current), face.enabledSelf, face.name);
                var hit = face.panel.Pick(face.worldBound.center);
                Assert.IsTrue(hit == face || face.Contains(hit), face.name + " is reachable");
            }
            StringAssert.StartsWith("Choose the new result", board.Console.Status.Hint.text);
        }
    }
}
