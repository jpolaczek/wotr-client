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
    public sealed class MapContourInteractionTests
    {
        [UnityTest]
        public IEnumerator BothSidesClickOnContoursCommitsTheArmyStepAndNeverDragsIt() => MatchTestRoom.Wait(MoveBothSides());

        [UnityTest]
        public IEnumerator BothSidesAttackOnTheEnemyContourWithoutAnotherConfirmation() => MatchTestRoom.Wait(AttackBothSides());

        [UnityTest]
        public IEnumerator WaterAndDistantTerrainNeverConsumeTheArmySelectionOrDie() => MatchTestRoom.Wait(Invalid());

        [UnityTest]
        public IEnumerator ReconnectCancelsAPressedArmyTargetAtTheSameRevision() => MatchTestRoom.Wait(Stale(false));

        [UnityTest]
        public IEnumerator AnotherDeviceCommitCancelsAPressedArmyTargetAtTheNewRevision() => MatchTestRoom.Wait(Stale(true));

        private static async Task MoveBothSides()
        {
            foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
            {
                using var room = await UI10TestRoom.Connect($"map15-{side}-click", shadowBoundary: side == Side.Shadow);
                var client = side == Side.FreePeoples ? room.Fp : room.Sh;
                var from = side == Side.FreePeoples ? "minas-tirith" : "lossarnach";
                var to = side == Side.FreePeoples ? "osgiliath" : "pelargir";
                await Select(room, client, from, .95f);
                var board = client.Board;
                // The HUD around the map decides which target stays in view; reach it like a player first.
                MapContourProbe.ReachRegion(board.Map, to);
                await room.Settle();
                LayoutTests.Save(client.Viewport, "ui323-march-" + side + ".png");
                var die = board.ActionDie.DieId;
                Assert.Contains(to, board.Map.DraftOverlay.Targets.ToArray());
                Assert.IsEmpty(board.Map.Query<Button>(className: "map-target").ToList());
                Assert.IsEmpty(board.Map.Query<Label>(className: "placement-ghost").ToList());
                Assert.IsNull(board.Tray.Q<Button>("confirm-decision"));
                // UI-105: the army token is not a drag source; only the destination click moves it.
                MapContourProbe.AttemptDrag(board, Army(board, from), MapContourProbe.Terrain(board.Map, to));
                await room.Settle();
                Assert.AreEqual(0, room.Boundary.Commands, "Dragging the army is not a move.");
                Assert.IsTrue(Army(board, from).ClassListContains("selected-force"));
                MapContourProbe.ReachRegion(board.Map, to);
                await room.Settle();
                var pan = board.Map.Camera.Pan;
                var zoom = board.Map.Camera.Zoom;
                var point = MapContourProbe.Terrain(board.Map, to);
                MapContourProbe.Press(board.Map, point);
                await room.Settle();
                Assert.AreEqual(0, room.Boundary.Commands, "A terrain press alone cannot spend the action die.");
                MapContourProbe.Release(board.Map, point);
                await room.At(1);
                Assert.AreEqual(1, room.Boundary.Commands);
                Assert.AreEqual(CommandKind.MoveArmies, room.Boundary.LastCommand.Kind);
                var command = room.Boundary.LastCommand.Payload;
                Assert.IsTrue(command.Stepwise);
                Assert.AreEqual(die, command.DieId);
                Assert.AreEqual(from, command.Moves.Single().From);
                Assert.AreEqual(to, command.Moves.Single().To);
                Assert.AreEqual(1, board.View.ActionSequence.Remaining);
                Assert.AreEqual(DieLocation.Resolving, board.View.Dice.Single(d => d.Id == die).Location);
                Assert.AreEqual(pan, board.Map.Camera.Pan);
                Assert.AreEqual(zoom, board.Map.Camera.Zoom);
                CollectionAssert.AreEquivalent(room.Fp.Board.View.Units.Where(u => u.Location == to).Select(u => u.Id),
                    room.Sh.Board.View.Units.Where(u => u.Location == to).Select(u => u.Id));
            }
        }

        private static async Task AttackBothSides()
        {
            foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
            {
                using var room = await UI10TestRoom.Connect($"map15-{side}-attack", shadowBoundary: side == Side.Shadow);
                var client = side == Side.FreePeoples ? room.Fp : room.Sh;
                var from = side == Side.FreePeoples ? "minas-tirith" : "lossarnach";
                var to = side == Side.FreePeoples ? "lossarnach" : "minas-tirith";
                await Select(room, client, from, 1.25f);
                Assert.Contains(to, client.Board.Map.DraftOverlay.AttackTargets.ToArray());
                LayoutTests.Save(client.Viewport, "ui323-attack-" + side + ".png");
                MapContourProbe.Click(client.Board.Map, MapContourProbe.Terrain(client.Board.Map, to));
                await room.At(1);
                Assert.AreEqual(1, room.Boundary.Commands);
                Assert.AreEqual(CommandKind.AttackArmy, room.Boundary.LastCommand.Kind);
                Assert.AreEqual(to, room.Fp.Board.View.Battle.RegionId);
                Assert.AreEqual(to, room.Sh.Board.View.Battle.RegionId);
                Assert.AreEqual(1, client.Board.View.Dice.Count(d => d.Owner == side && d.Location == DieLocation.Resolving));
            }
        }

        private static async Task Invalid()
        {
            using var room = await UI10TestRoom.Connect("map15-invalid");
            var board = room.Fp.Board;
            await Select(room, room.Fp, "minas-tirith", 1);
            var die = board.ActionDie.DieId;
            var state = JsonConvert.SerializeObject(board.View);
            var targets = board.Map.DraftOverlay.Targets.ToArray();
            Assert.IsTrue(board.Presentation.Geometry.Boundaries.Any(boundary =>
                boundary.Owners.Contains("minas-tirith") && boundary.Owners.Contains("lamedon")));
            Assert.IsFalse(targets.Contains("lamedon"), "A shared mountain contour is not a legal army route.");
            MapContourProbe.AttemptDrag(board, Army(board, "minas-tirith"), MapContourProbe.Terrain(board.Map, "lamedon"));
            await room.Settle();
            Assert.AreEqual(0, room.Boundary.Commands);
            MapContourProbe.Click(board.Map, MapContourProbe.Terrain(board.Map, "lamedon"));
            await room.Settle();
            Assert.AreEqual(0, room.Boundary.Commands, "A region beyond a mountain contour is not a step.");
            board.Hover.Close();
            // The existing Gorgoroth terrain hole is inside the raster and has no region token.
            var water = new Vector2(1504.875f, 988.5f);
            var ocean = board.Map.LocalToWorld(board.Map.Camera.ToScreen(water));
            board.Map.Camera.Pan += board.Map.worldBound.center - ocean;
            board.Map.Refresh();
            await room.Settle();
            ocean = board.Map.LocalToWorld(board.Map.Camera.ToScreen(water));
            Assert.IsNull(board.Map.HitTest(board.Map.WorldToLocal(ocean)));
            MapContourProbe.Press(board.Map, ocean);
            MapContourProbe.Release(board.Map, ocean);
            await room.Settle();
            board.Map.Focus("dol-amroth");
            await room.Settle();
            MapContourProbe.Click(board.Map, MapContourProbe.Terrain(board.Map, "dol-amroth"));
            await room.Settle();
            Assert.AreEqual(0, room.Boundary.Commands);
            Assert.AreEqual(state, JsonConvert.SerializeObject(board.View));
            Assert.AreEqual(die, board.ActionDie.DieId);
            CollectionAssert.AreEquivalent(targets, board.Map.DraftOverlay.Targets);
            Assert.IsTrue(Army(board, "minas-tirith").ClassListContains("selected-force"));
        }

        private static async Task Stale(bool revisionChanges)
        {
            using var room = await UI10TestRoom.Connect("map15-" + (revisionChanges ? "revision" : "reconnect"),
                secondDevice: revisionChanges);
            var board = room.Fp.Board;
            await Select(room, room.Fp, "minas-tirith", 1.2f);
            var die = board.ActionDie.DieId;
            MapContourProbe.ReachRegion(board.Map, "osgiliath");
            await room.Settle();
            var pan = board.Map.Camera.Pan;
            var zoom = board.Map.Camera.Zoom;
            var target = MapContourProbe.Terrain(board.Map, "osgiliath");
            MapContourProbe.Press(board.Map, target);
            if (revisionChanges)
            {
                await Select(room, room.OtherFp, "minas-tirith", .9f);
                MapContourProbe.Click(room.OtherFp.Board.Map, MapContourProbe.Terrain(room.OtherFp.Board.Map, "osgiliath"));
                await room.At(1);
            }
            else
            {
                var connected = room.Fp.Connected;
                room.Fp.Connection.Reconnect();
                await room.Until(() => room.Fp.Connected > connected);
                await room.Settle();
            }
            MapContourProbe.Release(board.Map, target);
            await room.Settle();
            Assert.AreEqual(0, room.Boundary.Commands, "A press from before the change must not reach the command endpoint.");
            Assert.AreEqual(revisionChanges ? 1 : 0, board.View.Revision);
            Assert.AreEqual(pan, board.Map.Camera.Pan);
            Assert.AreEqual(zoom, board.Map.Camera.Zoom);
            Assert.IsFalse(board.Map.HasPointerCapture(PointerId.mousePointerId));
            Assert.AreEqual(revisionChanges ? DieLocation.Resolving : DieLocation.Available,
                board.View.Dice.Single(d => d.Id == die).Location);
        }

        private static async Task Select(UI10TestRoom room, MountedMatchClient client, string from, float zoom)
        {
            client.Board.Map.Focus(from);
            client.Board.Map.Camera.ZoomAt(client.Board.Map.contentRect.center, zoom);
            client.Board.Map.Refresh();
            ArmyGestures.Die(client, ArmyGestures.ArmyDie(client));
            await ArmyGestures.All(client, from);
            client.Board.Hover.Close();
            await room.Settle();
            Assert.AreEqual(DisplayStyle.None, client.Board.Hover.Panel.Root.resolvedStyle.display);
            Assert.IsTrue(Army(client.Board, from).ClassListContains("selected-force"));
            CollectionAssert.AreEquivalent(new[] { from }, client.Board.Map.Renderer.SourceGlowRegions,
                "UI-323: the selected army's own region has the steady gold source edge.");
        }

        private static Button Army(BoardScreen board, string from) =>
            board.Map.Q<Button>($"army-{from}-{board.View.You}");
    }
}
