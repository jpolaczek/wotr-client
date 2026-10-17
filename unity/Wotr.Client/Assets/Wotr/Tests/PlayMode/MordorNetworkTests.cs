using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    public sealed class MordorNetworkTests
    {
        [UnityTest] public IEnumerator BothGatesPreserveHiddenAndRevealedAndAllowDeclining() => MatchTestRoom.Wait(Entries());
        [UnityTest] public IEnumerator StopAndZeroDiceEyeAreAttemptsWithIndependentReveal() => MatchTestRoom.Wait(Tiles());
        [UnityTest] public IEnumerator HideAndInactivityPublishTheTurnBoundary() => MatchTestRoom.Wait(Activity());
        [UnityTest] public IEnumerator BothFinalReceiptsRecoverWithoutRepeatingTheTile() => MatchTestRoom.Wait(Finals());
        [UnityTest] public IEnumerator MithrilReplacesStopBeforeAdvancingAndSpendsItsCardOnce() => MatchTestRoom.Wait(ReplaceStop());
        [UnityTest] public IEnumerator MovementAndEventEyesUseTheirOwnSourceWithoutARoll() => MatchTestRoom.Wait(EyeSources());
        [UnityTest] public IEnumerator BreakingRemovesCompanionsInsideHuntWithoutMapTargets() => MatchTestRoom.Wait(Breaking());

        private static async Task ReplaceStop()
        {
            using var room = await UI10TestRoom.Connect("ui52-mordor-replace-stop");
            var board = room.Fp.Board;
            var cost = board.View.Dice.Single(d => d.Location == DieLocation.Resolving).Id;
            Assert.AreEqual("base.hunt.special-sh.1.1.1", board.View.Hunt.TileId);
            Assert.AreEqual(2, board.View.Fellowship.MordorStep);
            AssertNoRoll(room, 0);
            room.Boundary.HoldNextCommand = true;
            PointerGestureEvents.ClickAt(board.Tray.Q<Button>("effect-choice-card.mithril-replace"));
            await room.Until(() => room.Boundary.CommandHeld);
            await room.At(2);
            Assert.AreEqual("base.hunt.standard.1.0.1", board.View.Hunt.TileId);
            Assert.IsFalse(board.View.Hunt.Stop);
            Assert.IsFalse(board.View.Hunt.Reveal);
            Assert.AreEqual(2, board.View.Fellowship.MordorStep);
            Assert.AreEqual(DieLocation.Resolving, board.View.Dice.Single(d => d.Id == cost).Location);
            room.Boundary.ReleaseCommand();
            await room.Until(() => Recovery(board) != null);
            await room.Settle();
            PointerGestureEvents.ClickAt(Recovery(board));
            await room.Until(() => Recovery(board) == null && board.Tray.enabledInHierarchy);
            await Ring(room);
            await room.At(3);
            Assert.AreEqual(3, board.View.Fellowship.MordorStep);
            Assert.IsTrue(board.View.Fellowship.Hidden);
            Assert.AreEqual(5, board.View.Fellowship.Corruption);
            Assert.AreEqual(DieLocation.Hunt, board.View.Dice.Single(d => d.Id == cost).Location);
            Assert.AreEqual(1, board.View.HuntReport.Entries.Count(e => e.Kind == HuntReportKind.TileReplaced));
            Assert.IsFalse(board.View.Players.Any(p => p.TableCards.Contains("base.fp.character.05")));
            Assert.AreEqual(2, room.Boundary.Commands);
            AssertPublic(room);
        }

        private static async Task EyeSources()
        {
            using (var room = await UI10TestRoom.Connect("ui52-mordor-eye-value"))
            {
                AssertNoRoll(room, 3);
                Assert.AreEqual(3, room.Fp.Board.View.Hunt.Damage);
                Assert.AreEqual(2, room.Fp.Board.View.Fellowship.MordorStep);
                await Ring(room);
                await room.At(2);
                Assert.AreEqual(7, room.Fp.Board.View.Fellowship.Corruption);
                Assert.AreEqual(3, room.Fp.Board.View.Fellowship.MordorStep);
                Assert.IsFalse(room.Fp.Board.View.Fellowship.Hidden);
                AssertPublic(room);
            }
            using (var room = await UI10TestRoom.Connect("ui52-mordor-event-eye"))
            {
                var board = room.Fp.Board;
                var cost = board.View.Dice.Single(d => d.Location == DieLocation.Resolving).Id;
                Assert.AreEqual(Side.Shadow, board.View.Dice.Single(d => d.Id == cost).Owner);
                AssertNoRoll(room, 0);
                Assert.AreEqual("base.sh.character.05", board.View.Hunt.SourceId);
                PointerGestureEvents.ClickAt(board.Tray.Q<Button>("action-DeclineReaction"));
                await room.At(2);
                Assert.IsNull(board.View.Hunt);
                Assert.AreEqual(4, board.View.Fellowship.Corruption);
                Assert.AreEqual(2, board.View.Fellowship.MordorStep);
                Assert.IsTrue(board.View.Fellowship.Hidden, "Orc Patrol ignores Eye tiles completely.");
                Assert.AreEqual(DieLocation.Spent, board.View.Dice.Single(d => d.Id == cost).Location);
                AssertActivity(room, false);
                AssertPublic(room);
            }
        }

        private static async Task Breaking()
        {
            using var room = await UI10TestRoom.Connect("ui52-mordor-breaking");
            var board = room.Fp.Board;
            var cost = board.View.Dice.Single(d => d.Location == DieLocation.Resolving).Id;
            Assert.AreEqual("FellowshipCompanions", board.View.Decision.Presentation);
            Assert.AreSame(board.Hunt.DecisionHost, board.Tray.parent);
            Assert.AreEqual("Remove companions from play", HuntActions.DecisionText(board.View));
            foreach (var id in new[] { "base.character.merry", "base.character.pippin" })
            {
                PointerGestureEvents.ClickAt(board.Tray.Q<Button>("effect-choice-" + id));
                await room.Settle();
            }
            Assert.IsEmpty(board.Map.DraftOverlay.Targets);
            var confirm = board.Tray.Q<Button>("confirm-decision");
            Assert.AreEqual("Remove from play", confirm.text);
            room.Boundary.HoldNextCommand = true;
            PointerGestureEvents.ClickAt(confirm);
            await room.Until(() => room.Boundary.CommandHeld);
            await room.At(2);
            Assert.IsNull(board.View.Hunt);
            Assert.AreEqual(1, board.View.Fellowship.CompanionCount);
            Assert.AreEqual("base.character.gimli", board.View.Fellowship.Guide);
            Assert.AreEqual(2, board.View.Fellowship.MordorStep);
            Assert.AreEqual(4, board.View.Fellowship.Corruption);
            Assert.IsFalse(board.View.Characters.Any(c => c.Id is "base.character.merry" or "base.character.pippin"));
            Assert.IsFalse(room.Sh.Board.View.Characters.Any(c => c.Id is "base.character.merry" or "base.character.pippin"));
            Assert.IsEmpty(room.Boundary.LastCommand.Payload.Route);
            Assert.AreEqual(DieLocation.Spent, board.View.Dice.Single(d => d.Id == cost).Location);
            room.Boundary.ReleaseCommand();
            await room.Until(() => Recovery(board) != null);
            await room.Settle();
            PointerGestureEvents.ClickAt(Recovery(board));
            await room.Until(() => Recovery(board) == null && board.Tray.enabledInHierarchy);
            Assert.AreEqual(1, room.Boundary.Commands);
            AssertPublic(room);
        }

        private static void AssertNoRoll(UI10TestRoom room, int eye)
        {
            foreach (var board in new[] { room.Fp.Board, room.Sh.Board })
            {
                Assert.IsTrue(board.View.Hunt.Mordor);
                Assert.AreEqual(eye, board.View.Hunt.EyeDamage);
                Assert.IsEmpty(board.View.Hunt.Dice);
                Assert.IsFalse(board.View.Options.Any(o => o.Kind == CommandKind.RerollHunt));
                Assert.IsFalse(board.View.HuntReport.Entries.Any(e => e.Kind is HuntReportKind.Rolled or HuntReportKind.Rerolled));
                StringAssert.Contains("No Hunt roll", board.Hunt.Root.Q<Label>("hunt-source").text);
                StringAssert.Contains("Eye " + eye, board.Hunt.Root.Q<Label>("hunt-source").text);
                Assert.IsNull(board.Hunt.Root.Q<Label>("mordor-activity"));
            }
            AssertPublic(room);
        }

        private static void AssertPublic(UI10TestRoom room) => Assert.AreEqual(
            JsonConvert.SerializeObject(room.Fp.Board.View.HuntReport), JsonConvert.SerializeObject(room.Sh.Board.View.HuntReport));

        private static async Task Entries()
        {
            foreach (var gate in new[] { "morannon", "minas-morgul" })
            {
                foreach (var hidden in new[] { true, false })
                {
                    using var room = await UI10TestRoom.Connect($"ui34-mordor-{gate}-{hidden}");
                    var board = room.Fp.Board;
                    var shadowPan = room.Sh.Board.Map.Camera.Pan;
                    if (hidden)
                        await Action(room, "fellowship-enter");
                    else
                    {
                        // UI-155: the gate on the map enters with one click.
                        board.Map.FocusOn(board.Map.Data.MordorTrack.Bounds.center, board.Map.Camera.Zoom);
                        await room.Settle();
                        var entrance = board.Map.MordorTrack.GateButton(gate);
                        Assert.IsTrue(entrance.ClassListContains(MapMordorTrack.NextClass), "The Fellowship's gate glows.");
                        Assert.IsTrue(board.Map.MordorTrack.StepButton(0).ClassListContains(MapMordorTrack.NextClass));
                        PointerGestureEvents.ClickAt(entrance);
                    }
                    await room.At(1);
                    Assert.AreEqual(shadowPan, room.Sh.Board.Map.Camera.Pan, "The Shadow's camera stays.");
                    var stepZero = board.Map.MordorTrack.StepButton(0);
                    Assert.IsTrue(board.Map.contentRect.Contains(board.Map.WorldToLocal(stepZero.worldBound.center)),
                        "The Free Peoples see the track after entering.");
                    AssertActivity(room, false);
                    Assert.AreEqual(hidden, board.View.Fellowship.Hidden);
                    Assert.AreEqual(0, board.View.Fellowship.MordorStep);
                    Assert.AreEqual("mordor-track", board.View.Fellowship.RegionId);
                    Assert.IsEmpty(room.Boundary.LastCommand.Payload.DieId);
                    Assert.IsTrue(board.Fellowship.Root.Q<Label>("mordor-step-0").ClassListContains("selected"));
                    board.Map.Camera.Pan += new Vector2(400, -300);
                    board.Map.Refresh();
                    PointerGestureEvents.ClickAt(board.Fellowship.Root.Q<Button>("focus-fellowship"));
                    await room.Settle();
                    Assert.IsTrue(MordorLayoutTests.TrackInView(board.Map), "In Mordor the Fellowship's button brings the track into view.");
                    // UI-154: the figure no longer leaves the map in Mordor; it stands on step 0 of the track.
                    Assert.IsTrue(board.Map.FellowshipMarker.OnTrack);
                    Assert.AreEqual(0f, board.Map.FellowshipMarker.TrackStep);
                    Assert.IsTrue(board.Map.MordorTrack.StepButton(0).ClassListContains(MapMordorTrack.CurrentClass));
                }
                using var declined = await UI10TestRoom.Connect("ui34-mordor-decline-" + gate);
                await Action(declined, "fellowship-continue");
                await declined.At(1);
                Assert.IsNull(declined.Fp.Board.View.Fellowship.MordorStep);
                AssertActivity(declined, null);
                Assert.AreEqual(gate, declined.Fp.Board.View.Fellowship.RegionId);
            }
        }

        private static async Task Tiles()
        {
            foreach (var tile in new[] { "stop", "eye" })
            {
                using var room = await UI10TestRoom.Connect("ui34-mordor-" + tile);
                AssertActivity(room, false);
                var die = await Cost(room);
                var board = room.Fp.Board;
                if (tile == "eye")
                {
                    // UI-155: with the die chosen the next stone glows and one click attempts the move.
                    board.Map.FocusMordorTrack();
                    await room.Settle();
                    var next = board.Map.MordorTrack.StepButton(board.View.Fellowship.MordorStep.Value + 1);
                    Assert.IsTrue(next.ClassListContains(MapMordorTrack.NextClass));
                    StringAssert.Contains("may stop it", next.tooltip);
                    PointerGestureEvents.ClickAt(next);
                }
                else
                    await Action(room, "fellowship-progress");
                await room.At(1);
                AssertActivity(room, true);
                Assert.AreEqual(die, room.Boundary.LastCommand.Payload.DieId);
                if (tile == "stop")
                {
                    Assert.AreEqual(2, board.View.Fellowship.MordorStep);
                    StringAssert.Contains("No Hunt roll", board.Hunt.Root.Q<Label>("hunt-source").text);
                    Assert.IsTrue(board.View.Hunt.Stop);
                    Assert.IsTrue(board.View.Hunt.Reveal);
                    Assert.IsEmpty(board.View.Hunt.Dice);
                    await Ring(room);
                    await room.At(2);
                }
                Assert.AreEqual(tile == "stop" ? 2 : 3, board.View.Fellowship.MordorStep);
                Assert.AreEqual(tile == "stop" ? 5 : 4, board.View.Fellowship.Corruption);
                Assert.IsFalse(board.View.Fellowship.Hidden);
                Assert.AreEqual(DieLocation.Hunt, board.View.Dice.Single(d => d.Id == die).Location);
                Assert.IsFalse(board.View.Options.Any(o => o.Kind is CommandKind.RevealFellowship or CommandKind.SeparateCompanions));
                AssertActivity(room, true);
                await room.Fp.Connection.Refresh();
                AssertActivity(room, true);
            }
        }

        private static async Task Activity()
        {
            using (var room = await UI10TestRoom.Connect("ui34-mordor-hide"))
            {
                var die = await Cost(room);
                // UI-155: the revealed figure on the track hides with one click.
                var figure = room.Fp.Board.Map.FellowshipMarker;
                room.Fp.Board.Map.FocusMordorTrack();
                await room.Settle();
                Assert.IsTrue(figure.HideOffered);
                Assert.IsTrue(figure.Root.ClassListContains(MapFellowshipMarker.HideClass));
                PointerGestureEvents.ClickAt(figure.Root);
                await room.At(1);
                AssertActivity(room, true);
                var view = room.Fp.Board.View;
                Assert.IsTrue(view.Fellowship.Hidden);
                Assert.AreEqual(2, view.Fellowship.MordorStep);
                Assert.AreEqual(4, view.Fellowship.Corruption);
                Assert.IsNull(view.Hunt);
                Assert.AreEqual(DieLocation.Spent, view.Dice.Single(d => d.Id == die).Location);
                var turn = view.Turn;
                while (room.Fp.Board.View.Turn == turn)
                    await Discard(room);
                AssertActivity(room, false);
                Assert.AreEqual(4, room.Fp.Board.View.Fellowship.Corruption);
            }
            using (var room = await UI10TestRoom.Connect("ui34-mordor-inactive"))
            {
                AssertActivity(room, false);
                var turn = room.Fp.Board.View.Turn;
                await Discard(room);
                Assert.AreEqual(turn + 1, room.Fp.Board.View.Turn);
                Assert.AreEqual(5, room.Fp.Board.View.Fellowship.Corruption);
                AssertActivity(room, false);
            }
        }

        private static async Task Finals()
        {
            foreach (var ending in new[] { "fp", "sh" })
            {
                using var room = await UI10TestRoom.Connect("ui34-mordor-final-" + ending);
                await Cost(room);
                await Action(room, "fellowship-progress");
                await room.At(1);
                Assert.AreEqual(4, room.Fp.Board.View.Fellowship.MordorStep);
                Assert.IsNull(room.Fp.Board.View.Winner);
                room.Boundary.HoldNextCommand = true;
                await Ring(room);
                await room.Until(() => room.Boundary.CommandHeld);
                await room.At(2);
                var board = room.Fp.Board;
                var report = JsonConvert.SerializeObject(board.View.HuntReport);
                var winner = ending == "fp" ? Side.FreePeoples : Side.Shadow;
                Assert.AreEqual(winner, board.View.Winner);
                Assert.AreEqual(winner, room.Sh.Board.View.Winner);
                Assert.AreEqual(ending == "fp" ? 5 : 4, board.View.Fellowship.MordorStep);
                Assert.AreEqual(DisplayStyle.None, board.Fellowship.Root.Q<Label>("mordor-activity").style.display.value);
                room.Boundary.ReleaseCommand();
                await room.Until(() => Recovery(board) != null);
                await room.Settle();
                // UI-169: the end of the game covers the board; View board uncovers the recovery.
                if (board.GameEnd.IsOpen)
                {
                    PointerGestureEvents.ClickAt(board.Root.Q<Button>("game-end-view-board"));
                    await room.Settle();
                }
                PointerGestureEvents.ClickAt(Recovery(board));
                await room.Until(() => Recovery(board) == null && board.Tray.enabledInHierarchy);
                Assert.Greater(room.Boundary.ReceiptLookups, 0);
                var connected = room.Fp.Connected;
                room.Fp.Connection.Reconnect();
                await room.Until(() => room.Fp.Connected > connected);
                Assert.AreEqual(report, JsonConvert.SerializeObject(board.View.HuntReport));
                Assert.AreEqual(report, JsonConvert.SerializeObject(room.Sh.Board.View.HuntReport));
                Assert.IsEmpty(board.View.Options);
                Assert.IsNull(board.View.Decision);
                StringAssert.Contains(ending == "fp" ? "destroyed" : "corrupted", board.Victory.Banner.text);
                board.Hunt.Open();
                await room.Settle();
                // UI-109/UI-162: after the result the window may show it, but it offers no late tile gesture at all.
                Assert.IsFalse(board.Hunt.DecisionHost.Query<Button>().ToList()
                    .Any(b => b.enabledInHierarchy && b.resolvedStyle.display == DisplayStyle.Flex), "No choice is left in the Hunt.");
                Assert.AreEqual(2, board.View.Revision, "Opening the Hunt after the result sends no command.");
                Assert.AreEqual(2, room.Boundary.Commands);
                AssertActivity(room, true);
            }
        }

        private static void AssertActivity(UI10TestRoom room, bool? expected)
        {
            Assert.AreEqual(expected, room.Fp.Board.View.Fellowship.MordorAttemptedThisTurn);
            Assert.AreEqual(expected, room.Sh.Board.View.Fellowship.MordorAttemptedThisTurn);
        }

        private static async Task<string> Cost(UI10TestRoom room)
        {
            var die = room.Fp.Board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available
                && d.Face is DieFace.Character or DieFace.WillOfTheWest).Id;
            PointerGestureEvents.ClickAt(room.Fp.Board.OwnDice.Q<Button>(die));
            await room.Settle();
            return die;
        }

        private static async Task Action(UI10TestRoom room, string name)
        {
            room.Fp.Board.Fellowship.Open();
            await room.Settle();
            PointerGestureEvents.ClickAt(room.Fp.Board.Fellowship.Root.Q<Button>(name));
        }

        private static async Task Ring(UI10TestRoom room)
        {
            room.Fp.Board.Hunt.Open();
            await room.Settle();
            // UI-142: the response's click applies it.
            PointerGestureEvents.ClickAt(room.Fp.Board.Hunt.Root.Q<Button>("hunt-response-Ring"));
            await room.Settle();
        }

        private static async Task Discard(UI10TestRoom room)
        {
            var revision = room.Fp.Board.View.Revision;
            var die = room.Fp.Board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available);
            PointerGestureEvents.ClickAt(room.Fp.Board.OwnDice.Q<Button>(die.Id));
            await room.Settle();
            await ComponentGestures.Select(room.Fp, "DiscardDie");
            await room.At(revision + 1);
        }

        private static Button Recovery(BoardScreen board) => board.Tray.Query<Button>().ToList().FirstOrDefault(b => b.text == "Check result");
    }
}
