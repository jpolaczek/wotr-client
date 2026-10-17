using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    public class FellowshipNetworkTests
    {
        [UnityTest]
        public IEnumerator DeclarationMovementSeparationAndIndependentGroupsUseTheServer()
        {
            var fixture = Load();
            var task = Travel(fixture);
            yield return Wait(task);
        }

        [UnityTest]
        public IEnumerator RerollGuideRevealEmptyPoolAndBothFinalMordorOutcomesUseTheServer()
        {
            var fixture = Load();
            var task = Hunts(fixture);
            yield return Wait(task);
        }

        private static async Task Travel(Fixture fixture)
        {
            using (var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(110)))
            {
                var token = lifetime.Token;
                using (var room = await Room.Connect(fixture, fixture.DeclarationMatch, token))
                {
                    var panel = room.Fp.Board.Fellowship;
                    var camera = room.Fp.Board.Map.Camera.Pan;
                    Assert.IsTrue(panel.IsOpen, "A new own Fellowship decision opens once.");
                    Assert.IsFalse(room.Sh.Board.Fellowship.Pinned);
                    panel.Close();
                    await room.Fp.Connection.Refresh();
                    Assert.IsFalse(panel.Pinned, "The same projection does not undo Close.");
                    Assert.AreEqual(camera, room.Fp.Board.Map.Camera.Pan);
                    FellowshipGestures.Action(room.Fp, "fellowship-declare");
                    camera = await Route(room.Fp, token, "fords-of-bruinen");
                    panel.Close();
                    await room.Fp.Connection.Refresh();
                    Assert.IsFalse(panel.Pinned);
                    PointerGestureEvents.ClickAt(room.Fp.Board.Notice.Button);
                    Assert.IsTrue(panel.IsOpen);
                    Assert.AreEqual(camera, room.Fp.Board.Map.Camera.Pan, "Opening a decision does not Locate it.");
                    StringAssert.Contains("Fords of Bruinen", room.Fp.Board.Tray.Q<Label>("travel-summary").text);
                    camera = await Route(room.Fp, token, "hollin");
                    PointerGestureEvents.ClickAt(room.Fp.Board.Tray.Q<Button>("route-undo"));
                    StringAssert.Contains("Fords of Bruinen", room.Fp.Board.Tray.Q<Label>("travel-summary").text);
                    LayoutTests.Save(room.Fp.Viewport, "ui52-route-before-cancel.png");
                    PointerGestureEvents.ClickAt(room.Fp.Board.Tray.Q<Button>("route-cancel"));
                    Assert.AreEqual(0, room.Fp.Board.View.Revision);
                    Assert.AreEqual(camera, room.Fp.Board.Map.Camera.Pan);
                    FellowshipGestures.Action(room.Fp, "fellowship-declare");
                    // UI-219: the fifth step uses all five Progress and declares at once.
                    await Route(room.Fp, token, "fords-of-bruinen", "hollin", "moria", "dimrill-dale", "lorien");
                    await room.At(1, token);
                    Assert.AreEqual("lorien", room.Fp.Board.View.Fellowship.RegionId);
                    Assert.AreEqual(0, room.Fp.Board.View.Fellowship.Corruption);
                    Assert.IsTrue(room.Fp.Board.View.Fellowship.Hidden);
                }
                using (var room = await Room.Connect(fixture, fixture.FellowshipMatch, token))
                {
                    FellowshipGestures.Die(room.Fp);
                    FellowshipGestures.Action(room.Fp, "fellowship-progress");
                    Assert.IsNull(room.Fp.Board.Tray.Q<Label>("travel-summary"));
                    Assert.IsEmpty(room.Fp.Board.Map.DraftOverlay.Targets);
                    await room.At(1, token);
                    Assert.AreEqual(5, room.Fp.Board.View.Fellowship.Progress);
                    Assert.AreEqual("rivendell", room.Fp.Board.View.Fellowship.RegionId);
                    await Discard(room.Sh, token);
                    await room.At(2, token);
                    await Separate(room, token);
                    await MoveGroups(room, token);
                }
            }
        }

        private static async Task Separate(Room room, CancellationToken token)
        {
            FellowshipGestures.Die(room.Fp);
            await Task.Delay(80, token);
            FellowshipGestures.Portrait(room.Fp, "base.character.gimli");
            FellowshipGestures.Portrait(room.Fp, "base.character.merry");
            // UI-105: one click on a highlighted destination lays the shortest legal route and sends it.
            // TravelActionTests check that the route never passes a stop such as this Shadow stronghold.
            await Task.Delay(80, token);
            Assert.Contains("moria", room.Fp.Board.Map.DraftOverlay.Targets.ToList());
            FellowshipGestures.Destination(room.Fp, "moria");
            await room.At(3, token);
            Assert.AreEqual(DecisionKind.Guide, room.Fp.Board.View.Decision.Kind);
            Assert.IsNull(room.Sh.Board.View.Decision);
            FellowshipGestures.Portrait(room.Fp, "base.character.gandalf-grey");
            await room.At(4, token);
            Assert.AreEqual(5, room.Fp.Board.View.Fellowship.CompanionCount);
            Assert.AreEqual("moria", room.Fp.Board.View.Characters.Single(c => c.Id == "base.character.merry").Location);
            await Discard(room.Sh, token);
            await room.At(5, token);
        }

        private static async Task MoveGroups(Room room, CancellationToken token)
        {
            FellowshipGestures.Die(room.Fp);
            var paidDie = room.Fp.Board.ActionDie.DieId;
            FellowshipGestures.Figure(room.Fp, "moria", "base.character.gimli");
            await Route(room.Fp, token, "dimrill-dale");
            FellowshipGestures.FinishRoute(room.Fp, "dimrill-dale");
            await room.At(6, token);
            Assert.AreEqual(DecisionKind.ActionSequence, room.Fp.Board.View.Decision.Kind);
            Assert.AreEqual(paidDie, room.Fp.Board.View.ActionSequence.DieId);
            Assert.AreEqual(DieLocation.Resolving, room.Fp.Board.View.Dice.Single(d => d.Id == paidDie).Location);
            var connections = room.Fp.Connected;
            room.Fp.Connection.Reconnect();
            await Until(() => room.Fp.Connected > connections, token);
            await Task.Delay(80, token);
            CollectionAssert.Contains(room.Fp.Board.View.ActionSequence.UsedIds, "base.character.gimli");
            FellowshipGestures.Figure(room.Fp, "moria", "base.character.merry");
            await Route(room.Fp, token, "hollin");
            FellowshipGestures.FinishRoute(room.Fp, "hollin");
            await room.At(7, token);
            var view = room.Fp.Board.View;
            Assert.AreEqual("dimrill-dale", view.Characters.Single(c => c.Id == "base.character.gimli").Location);
            Assert.AreEqual("hollin", view.Characters.Single(c => c.Id == "base.character.merry").Location);
            Assert.AreEqual(2, view.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Spent));
            Assert.AreEqual(1, view.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Hunt));
        }

        private static async Task Hunts(Fixture fixture)
        {
            using (var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(110)))
            {
                var token = lifetime.Token;
                using (var room = await Room.Connect(fixture, fixture.HuntMatch, token))
                    await RerollAndReveal(room, token);
                using (var room = await Room.Connect(fixture, fixture.MordorEntryMatch, token))
                {
                    FellowshipGestures.Action(room.Fp, "fellowship-enter");
                    await room.At(1, token);
                    Assert.AreEqual(0, room.Fp.Board.View.Fellowship.MordorStep);
                    Assert.IsFalse(room.Fp.Board.View.Fellowship.Hidden);
                    // UI-155: entering brings the track into the view of the player who entered.
                    Assert.IsTrue(MordorLayoutTests.TrackInView(room.Fp.Board.Map));
                    CardNetworkTests.Click(room.Fp.Board.Fellowship.Root.Q<Button>("focus-fellowship"));
                    Assert.IsTrue(MordorLayoutTests.TrackInView(room.Fp.Board.Map));
                    Assert.IsTrue(room.Fp.Board.Fellowship.Root.Q<Label>("mordor-step-0").ClassListContains("selected"));
                }
                for (var index = 0; index < fixture.MordorMatches.Length; index++)
                    using (var room = await Room.Connect(fixture, fixture.MordorMatches[index], token))
                        await FinalMove(room, index == 0 ? Side.FreePeoples : Side.Shadow, token);
            }
        }

        private static async Task RerollAndReveal(Room room, CancellationToken token)
        {
            var reportId = room.Fp.Board.View.HuntReport.Id;
            var paidDie = room.Fp.Board.View.Dice.Single(d => d.Location == DieLocation.Resolving).Id;
            Assert.AreEqual(DecisionKind.HuntReroll, room.Sh.Board.View.Decision.Kind);
            Choose(room.Sh, "hunt-die-hunt-die-0");
            await Confirm(room.Sh, token);
            await room.At(2, token);
            Assert.IsNull(room.Fp.Board.Tray.Q<Button>("confirm-decision"), "Each explicit Hunt response is a complete decision.");
            Choose(room.Fp, "hunt-response-Guide");
            await room.At(3, token);
            Assert.AreEqual(DecisionKind.Guide, room.Fp.Board.View.Decision.Kind);
            Assert.AreSame(room.Fp.Board.Hunt.DecisionHost, room.Fp.Board.Tray.parent);
            Assert.IsNull(room.Sh.Board.View.Decision);
            var connected = room.Fp.Connected;
            var pan = room.Fp.Board.Map.Camera.Pan;
            room.Fp.Connection.Reconnect();
            await Until(() => room.Fp.Connected > connected, token);
            Assert.AreEqual(pan, room.Fp.Board.Map.Camera.Pan);
            FellowshipGestures.Portrait(room.Fp, "base.character.gimli");
            await room.At(4, token);
            Assert.AreSame(room.Fp.Board.Fellowship.Root, room.Fp.Board.Tray.parent);
            await Route(room.Fp, token, "dimrill-dale");
            LayoutTests.Save(room.Fp.Viewport, "ui52-route-before-cancel.png");
            PointerGestureEvents.ClickAt(room.Fp.Board.Tray.Q<Button>("route-cancel"));
            Assert.AreEqual(DecisionKind.FellowshipRoute, room.Fp.Board.View.Decision.Kind);
            StringAssert.Contains("Become revealed", room.Fp.Board.Tray.Q<Label>("fellowship-route-effect").text);
            await Route(room.Fp, token, "dimrill-dale", "north-anduin-vale");
            await Task.Delay(80, token);
            var endpoint = MapContourProbe.ReachRegion(room.Fp.Board.Map, "dol-guldur");
            pan = room.Fp.Board.Map.Camera.Pan;
            var zoom = room.Fp.Board.Map.Camera.Zoom;
            // UI-219: the third step uses all three Progress and reveals at once.
            MapContourProbe.Click(room.Fp.Board.Map, endpoint);
            MapContourProbe.Click(room.Fp.Board.Map, endpoint, 2); // Fast further clicks must not answer the following damage decision.
            await room.At(5, token);
            foreach (var source in new[] { "moria", "dol-guldur" })
            {
                Assert.AreEqual(source, room.Fp.Board.View.Hunt.SourceId);
                Assert.AreEqual(source == "moria" ? 1 : 0, room.Fp.Board.View.Hunt.PoolCount);
                Assert.AreSame(room.Fp.Board.Hunt.DecisionHost, room.Fp.Board.Tray.parent);
                Assert.IsNotNull(room.Fp.Board.Hunt.Root.Q<Button>("hunt-tile"));
                Assert.AreEqual(DieLocation.Resolving, room.Fp.Board.View.Dice.First(d => d.Owner == Side.FreePeoples).Location);
                Assert.AreEqual(reportId, room.Fp.Board.View.HuntReport.Id);
                var revision = room.Fp.Board.View.Revision;
                Choose(room.Fp, "hunt-response-Ring");
                await room.At(revision + 1, token);
            }
            Assert.IsNull(room.Fp.Board.View.Hunt);
            Assert.AreEqual(4, room.Fp.Board.View.Fellowship.Corruption);
            Assert.IsFalse(room.Fp.Board.View.Fellowship.Hidden);
            Assert.AreEqual(1, room.Fp.Board.View.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Hunt));
            var report = JsonConvert.SerializeObject(room.Fp.Board.View.HuntReport);
            Assert.IsTrue(room.Fp.Board.View.HuntReport.Completed);
            Assert.AreEqual(report, JsonConvert.SerializeObject(room.Sh.Board.View.HuntReport));
            Assert.AreEqual(3, room.Fp.Board.View.HuntReport.Entries.Count(e => e.Kind == HuntReportKind.TileDrawn));
            await Discard(room.Sh, token);
            await room.At(8, token);
            FellowshipGestures.Die(room.Fp);
            FellowshipGestures.Action(room.Fp, "fellowship-state");
            await room.At(9, token);
            Assert.IsTrue(room.Fp.Board.View.Fellowship.Hidden);
            Assert.AreEqual("dol-guldur", room.Fp.Board.View.Fellowship.RegionId);
            Assert.AreEqual(1, room.Fp.Board.View.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Spent));
            Assert.AreEqual(DieLocation.Hunt, room.Fp.Board.View.Dice.Single(d => d.Id == paidDie).Location);
            await room.Fp.Connection.Refresh();
            Assert.AreEqual(report, JsonConvert.SerializeObject(room.Fp.Board.View.HuntReport), "Hide must retain the completed Hunt report.");
            Assert.AreEqual(pan, room.Fp.Board.Map.Camera.Pan);
            Assert.AreEqual(zoom, room.Fp.Board.Map.Camera.Zoom);
        }

        private static async Task FinalMove(Room room, Side winner, CancellationToken token)
        {
            FellowshipGestures.Die(room.Fp);
            FellowshipGestures.Action(room.Fp, "fellowship-progress");
            await room.At(1, token);
            Assert.IsNull(room.Fp.Board.View.Winner);
            Assert.AreEqual(4, room.Fp.Board.View.Fellowship.MordorStep);
            Assert.AreEqual(DisplayStyle.None, room.Fp.Board.Victory.Banner.style.display.value);
            Assert.IsTrue(room.Fp.Board.Fellowship.Root.Q<Label>("mordor-step-4").ClassListContains("selected"));
            Assert.IsFalse(room.Fp.Board.Fellowship.Root.Q<Label>("mordor-step-5").ClassListContains("selected"));
            Choose(room.Fp, "hunt-response-Ring");
            await room.At(2, token);
            Assert.AreEqual(winner, room.Fp.Board.View.Winner);
            Assert.AreEqual(winner, room.Sh.Board.View.Winner);
            StringAssert.Contains(winner == Side.FreePeoples ? "destroyed" : "corrupted", room.Fp.Board.Victory.Banner.text);
            Assert.IsNull(room.Fp.Board.Tray.Q<Button>("confirm-decision"));
            Assert.IsNotNull(room.Fp.Board.Map.panel);
        }

        private static void Choose(MountedMatchClient client, string name)
        {
            if (client.Board.Tray.parent == client.Board.Hunt.DecisionHost)
            {
                client.Board.Hunt.Open();
                // UI-142: a response's click applies it; no tile click follows.
                CardNetworkTests.Click(client.Board.Hunt.Root.Q<Button>(name));
            }
            else
            {
                client.Board.Fellowship.Open();
                CardNetworkTests.Click(client.Board.Tray.Q<Button>(name));
            }
        }
        private static async Task<Vector2> Route(MountedMatchClient client, CancellationToken token, params string[] regions)
        {
            var camera = client.Board.Map.Camera.Pan;
            foreach (var id in regions)
            {
                await Task.Delay(80, token);
                MapContourProbe.ReachRegion(client.Board.Map, id);
                await Task.Delay(80, token);
                camera = client.Board.Map.Camera.Pan;
                var zoom = client.Board.Map.Camera.Zoom;
                MapContourProbe.Click(client.Board.Map, MapContourProbe.Terrain(client.Board.Map, id));
                await Task.Delay(80, token);
                Assert.AreEqual(camera, client.Board.Map.Camera.Pan, "Choosing " + id + " must not navigate.");
                Assert.AreEqual(zoom, client.Board.Map.Camera.Zoom);
            }
            return camera;
        }
        private static async Task Confirm(MountedMatchClient client, CancellationToken token)
        {
            await Until(() => client.Board.Tray.Q<Button>("confirm-decision")?.enabledSelf == true, token);
            await Task.Delay(40, token);
            Choose(client, "confirm-decision");
        }
        private static async Task Discard(MountedMatchClient client, CancellationToken token)
        {
            var die = client.Board.View.Dice.First(d => d.Owner == client.Board.View.You && d.Location == DieLocation.Available);
            CardNetworkTests.Click(client.Board.OwnDice.Q<Button>(die.Id));
            await Task.Delay(60, token);
            await ComponentGestures.Select(client, "DiscardDie");
        }
        private static async Task Until(Func<bool> condition, CancellationToken token)
        {
            while (!condition())
                await Task.Delay(30, token);
        }
        private static IEnumerator Wait(Task task)
        {
            var deadline = Time.realtimeSinceStartup + 120;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsTrue(task.IsCompleted, "Fellowship network scenario timed out.");
            task.GetAwaiter().GetResult();
        }
        private static Fixture Load()
        {
            var path = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(path))
                Assert.Ignore("Run scripts/test-client.sh for real server integration.");
            return JsonConvert.DeserializeObject<Fixture>(File.ReadAllText(path));
        }
        private sealed class Fixture
        {
            public string Address, FellowshipMatch, DeclarationMatch, HuntMatch, MordorEntryMatch, FreePeoplesUser, ShadowUser, Password;
            public string[] MordorMatches;
        }
        private sealed class Room : IDisposable
        {
            public MountedMatchClient Fp, Sh;
            public static async Task<Room> Connect(Fixture fixture, string match, CancellationToken token)
            {
                var room = new Room { Fp = new MountedMatchClient(fixture.Address, match), Sh = new MountedMatchClient(fixture.Address, match) };
                try
                {
                    await room.Fp.Api.Login(fixture.FreePeoplesUser, fixture.Password, token);
                    await room.Sh.Api.Login(fixture.ShadowUser, fixture.Password, token);
                    room.Fp.Connection.Start();
                    room.Sh.Connection.Start();
                    await Until(() => room.Fp.Connected > 0 && room.Sh.Connected > 0, token);
                    await Task.Delay(80, token);
                    return room;
                }
                catch { room.Dispose(); throw; }
            }
            public async Task At(long revision, CancellationToken token)
            {
                await Until(() => Fp.Board.View?.Revision == revision && Sh.Board.View?.Revision == revision, token);
                await Task.Delay(80, token);
            }
            public void Dispose() { Fp?.Dispose(); Sh?.Dispose(); }
        }
    }
}
