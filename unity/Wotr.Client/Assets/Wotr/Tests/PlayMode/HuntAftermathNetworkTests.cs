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
    public sealed class HuntAftermathNetworkTests
    {
        private const string Merry = "base.character.merry";

        [UnityTest]
        public IEnumerator NewGuideAbilityEmptyPlacementAndStrongholdsKeepOnePaidContinuation() =>
            MatchTestRoom.Wait(Followups());

        private static async Task Followups()
        {
            using var room = await UI10TestRoom.Connect("ui52-hunt-aftermath");
            var board = room.Fp.Board;
            var cost = board.View.Dice.Single(d => d.Location == DieLocation.Resolving).Id;
            var reportId = board.View.HuntReport.Id;
            var camera = Camera(board);
            // UI-142: the response's click applies it.
            await Click(room, board.Hunt.Root.Q<Button>("hunt-response-Guide"));
            await room.At(2);
            Assert.AreEqual(DecisionKind.Guide, board.View.Decision.Kind);
            Assert.AreEqual(1, board.View.Hunt.Damage);
            Assert.AreEqual(0, board.View.Fellowship.Corruption);
            AssertContinuation(room, cost, reportId, true);
            board.Hunt.Close();
            await room.Fp.Connection.Refresh();
            Assert.IsFalse(board.Hunt.Pinned);
            var connected = room.Fp.Connected;
            room.Fp.Connection.Reconnect();
            await room.Until(() => room.Fp.Connected > connected);
            Assert.IsFalse(board.Hunt.Pinned);
            await Reopen(room, board);
            room.Boundary.ArmCommandLoss();
            PointerGestureEvents.ClickAt(board.Hunt.Guides.Root.Q<Button>("fellowship-portrait-" + Merry));
            await room.Until(() => room.Boundary.CommandHeld);
            await room.At(3);
            Assert.AreEqual("hunt.guide", board.View.Decision.Window);
            Assert.IsTrue(board.View.Hunt.CasualtyTaken);
            Assert.AreEqual(0, board.View.Fellowship.Corruption);
            AssertContinuation(room, cost, reportId, true);
            await Recover(room);
            await Click(room, board.Tray.Q<Button>("effect-choice-character.merry-guide"));
            await room.At(4);
            Assert.AreEqual(DecisionKind.CompanionSeparation, board.View.Decision.Kind);
            Assert.AreEqual(0, board.View.Hunt.Damage);
            AssertContinuation(room, cost, reportId, false);
            board.Hunt.Close();
            Assert.AreEqual(camera, Camera(board), "Hunt decisions and recovery must not navigate.");
            camera = await EmptyPlacement(room);
            await room.At(5);
            Assert.AreEqual(camera, Camera(board));
            Assert.AreEqual("dimrill-dale", board.View.Characters.Single(c => c.Id == Merry).Location);
            Assert.AreEqual(DecisionKind.FellowshipRoute, board.View.Decision.Kind);
            AssertContinuation(room, cost, reportId, false);
            camera = await Reveal(room);
            foreach (var source in new[] { "moria", "dol-guldur" })
            {
                Assert.AreEqual(source, board.View.Hunt.SourceId);
                AssertContinuation(room, cost, reportId, true);
                Assert.AreEqual("hunt.guide", board.View.Decision.Window);
                var revision = board.View.Revision;
                LayoutTests.Save(room.Fp.Viewport, "ui52-hunt-aftermath-" + source + ".png");
                // UI-291: the console's Pass declines the Guide's window.
                Assert.IsNull(board.Tray.Q<Button>("action-DeclineReaction"));
                await Click(room, board.Console.Actions.Pass);
                await room.At(revision + 1);
                Assert.AreEqual(DecisionKind.HuntDamage, board.View.Decision.Kind);
                if (source == "moria")
                {
                    board.Hunt.Close();
                    await room.Fp.Connection.Refresh();
                    Assert.IsFalse(board.Hunt.Pinned);
                    await Reopen(room, board);
                }
                await Click(room, board.Hunt.Root.Q<Button>("hunt-response-Ring"));
                await room.At(revision + 2);
                Assert.AreEqual(camera, Camera(board), "Stronghold Hunt must preserve the player's last map navigation.");
            }
            Assert.IsNull(board.View.Hunt);
            Assert.IsTrue(board.View.HuntReport.Completed);
            Assert.AreEqual(2, board.View.Fellowship.Corruption);
            Assert.AreEqual(DieLocation.Hunt, board.View.Dice.Single(d => d.Id == cost).Location);
            Assert.AreEqual(3, board.View.HuntReport.Entries.Count(e => e.Kind == HuntReportKind.TileDrawn));
            Assert.AreEqual(1, board.View.HuntReport.Entries.Count(e => e.Kind == HuntReportKind.Response && e.Response == HuntResponse.Guide));
            Assert.AreEqual(9, room.Boundary.Commands);
            Assert.AreEqual(camera, Camera(board));
            AssertPublic(room);
        }

        private static async Task<(Vector2 Pan, float Zoom)> EmptyPlacement(UI10TestRoom room)
        {
            var board = room.Fp.Board;
            Assert.IsFalse(board.View.Units.Any(u => u.Location == "dimrill-dale"));
            Assert.IsFalse(board.View.Characters.Any(c => c.Location == "dimrill-dale"));
            var target = MapContourProbe.ReachRegion(board.Map, "dimrill-dale");
            await room.Settle();
            target = MapContourProbe.Terrain(board.Map, "dimrill-dale");
            var camera = Camera(board);
            Assert.IsTrue(board.Map.DraftOverlay.Targets.Contains("dimrill-dale"));
            MapContourProbe.Hover(board.Root, target);
            await room.Settle();
            Assert.AreEqual(DisplayStyle.None, board.Hover.Panel.Root.resolvedStyle.display);
            // UI-105: the click on the empty destination separates Merry at once.
            MapContourProbe.Click(board.Map, target);
            await room.Settle();
            Assert.AreEqual(camera, Camera(board), "Choosing an empty placement must not navigate.");
            CollectionAssert.AreEqual(new[] { Merry }, room.Boundary.LastCommand.Payload.Ids);
            return camera;
        }

        private static async Task<(Vector2 Pan, float Zoom)> Reveal(UI10TestRoom room)
        {
            var board = room.Fp.Board;
            board.Fellowship.Close();
            await room.Fp.Connection.Refresh();
            Assert.IsFalse(board.Fellowship.Pinned);
            Assert.IsTrue(board.Fellowship.IsOpen, "Fellowship remains available in the permanent column.");
            await room.Settle();
            foreach (var region in new[] { "dimrill-dale", "north-anduin-vale" })
            {
                MapContourProbe.ReachRegion(board.Map, region);
                await room.Settle();
                var stepCamera = Camera(board);
                MapContourProbe.Click(board.Map, MapContourProbe.Terrain(board.Map, region));
                await room.Settle();
                Assert.AreEqual(stepCamera, Camera(board), "Choosing " + region + " must not navigate.");
            }
            Assert.IsTrue(board.Map.DraftOverlay.Targets.Contains("dol-guldur"));
            await HuntRoleScreenshots.Capture(room, "reveal-route", fellowship: true);
            // UI-219: the third step uses all of the Progress and reveals at once.
            MapContourProbe.ReachRegion(board.Map, "dol-guldur");
            await room.Settle();
            var camera = Camera(board);
            room.Boundary.ArmCommandLoss();
            MapContourProbe.Click(board.Map, MapContourProbe.Terrain(board.Map, "dol-guldur"));
            await room.Until(() => room.Boundary.CommandHeld);
            await room.At(6);
            await Recover(room);
            Assert.AreEqual(camera, Camera(board), "Final reveal and receipt recovery must not navigate.");
            return camera;
        }

        private static (Vector2 Pan, float Zoom) Camera(BoardScreen board) =>
            (board.Map.Camera.Pan, board.Map.Camera.Zoom);

        private static void AssertContinuation(UI10TestRoom room, string cost, string report, bool hunt)
        {
            var board = room.Fp.Board;
            Assert.AreEqual(DieLocation.Resolving, board.View.Dice.Single(d => d.Id == cost).Location);
            Assert.AreEqual(report, board.View.HuntReport.Id);
            Assert.AreSame(hunt ? board.Hunt.DecisionHost : board.Fellowship.Root, board.Tray.parent);
            Assert.IsNull(room.Sh.Board.View.Decision);
            Assert.IsEmpty(room.Sh.Board.View.Options);
            AssertPublic(room);
        }

        private static void AssertPublic(UI10TestRoom room) => Assert.AreEqual(
            JsonConvert.SerializeObject(room.Fp.Board.View.HuntReport), JsonConvert.SerializeObject(room.Sh.Board.View.HuntReport));

        private static async Task Click(UI10TestRoom room, VisualElement target)
        {
            Assert.IsNotNull(target);
            PointerGestureEvents.ClickAt(target);
            await room.Settle();
        }

        private static async Task Recover(UI10TestRoom room)
        {
            room.Boundary.ReleaseCommand();
            await room.Until(() => Recovery(room.Fp.Board) != null);
            await room.Settle();
            await Click(room, Recovery(room.Fp.Board));
            await room.Until(() => Recovery(room.Fp.Board) == null && room.Fp.Board.Tray.enabledInHierarchy);
            await room.Settle();
        }

        private static Button Recovery(BoardScreen board) =>
            board.Tray.Query<Button>().ToList().FirstOrDefault(b => b.text == "Check result");

        // UI-162: a closed Hunt window stays closed within its Hunt; the console's status brings it back.
        private static async Task Reopen(UI10TestRoom room, BoardScreen board)
        {
            Assert.IsFalse(board.Hunt.IsOpen, "A closed Hunt window stays closed within its Hunt.");
            PointerGestureEvents.ClickAt(board.Console.Status.Plaque);
            await room.Settle();
            Assert.IsTrue(board.Hunt.IsOpen, "The console's status reopens the Hunt.");
        }
    }
}
