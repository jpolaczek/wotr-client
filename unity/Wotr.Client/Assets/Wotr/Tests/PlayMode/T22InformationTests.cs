using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public sealed class T22InformationTests
    {
        [UnityTest]
        public IEnumerator SlowAndFailedInitialReadNeverDisplaysLocalGameState() => MatchTestRoom.Wait(Loading());

        [UnityTest]
        public IEnumerator InformationUsesBothPrivateProjectionsAndPreservesThePlannedMove() => MatchTestRoom.Wait(Information());

        private static async Task Loading()
        {
            var config = Configuration();
            var match = config.BaseAcceptanceMatches["information-loading"];
            using var boundary = new InitialReadBoundary("/api/matches/" + match);
            using var client = new MountedMatchClient(config.Address, match, boundary);
            using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            AssertLoading(client.Board);
            client.Board.Apply(null);
            AssertLoading(client.Board);
            await client.Api.Login(config.FreePeoplesUser, config.Password, lifetime.Token);
            client.Connection.Start();
            await Until(() => boundary.Held, lifetime.Token);
            await Task.Delay(80, lifetime.Token);
            AssertLoading(client.Board);
            boundary.Release();
            await Until(() => client.Board.Connection.text == "Reconnecting…", lifetime.Token);
            AssertLoading(client.Board);
            await Until(() => client.Connected > 0 && client.Board.View != null, lifetime.Token);
            Assert.IsNotEmpty(client.Board.Presentation.Armies);
            Assert.AreEqual(DisplayStyle.None, client.Board.Root.Q<Label>("match-loading").style.display.value);
            Assert.AreEqual(client.Board.View.Units.Count(u => client.Board.Presentation.Regions.ContainsKey(u.Location)),
                client.Board.Presentation.Armies.Sum(a => a.Regular + a.Elite + a.Leaders));
            Assert.AreEqual(0, boundary.Commands);
        }

        private static void AssertLoading(BoardScreen board)
        {
            Assert.IsNull(board.View);
            Assert.IsEmpty(board.Presentation.Armies);
            Assert.IsEmpty(board.Presentation.Control);
            Assert.IsEmpty(board.Map.Query<Button>(className: "army").ToList());
            Assert.IsEmpty(board.OwnDice.Query<Button>().ToList());
            Assert.AreNotEqual(DisplayStyle.None, board.Root.Q<Label>("match-loading").style.display.value);
            Assert.IsFalse(board.Root.Q<Button>("open-log").enabledSelf);
        }

        private static async Task Information()
        {
            using var room = await MatchTestRoom.Connect("information-readonly", ScenarioKind.BaseAcceptance);
            var board = room.Fp.Board;
            var revision = board.View.Revision;
            var ownArmy = board.Map.Q<Button>("army-minas-tirith-FreePeoples");
            await MapContourProbe.ReachMarker(board.Map, ownArmy, "minas-tirith");
            PointerGestureEvents.ClickAt(ownArmy);
            await room.Settle();
            var cost = board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Army);
            CardNetworkTests.Click(board.OwnDice.Q<Button>(cost.Id));
            await room.Settle();
            await ArmyGestures.All(room.Fp, "minas-tirith");
            await room.Settle();
            var destinations = board.Map.DraftOverlay.Targets.ToArray();
            CollectionAssert.Contains(destinations, "osgiliath");
            Assert.IsNull(board.Tray.Q<Button>("confirm-decision"));
            board.Cards.Show(board.View.Hand.First(), true);
            board.Hover.Close();
            board.Map.Focus("orthanc");
            var opponentArmy = board.Map.Q<Button>("army-orthanc-Shadow");
            await MapContourProbe.ReachMarker(board.Map, opponentArmy, "orthanc");
            var selected = board.Map.Selected;
            await room.Settle();
            var pan = board.Map.Camera.Pan;
            var zoom = board.Map.Camera.Zoom;
            await ComponentGestures.Hover(opponentArmy);
            Assert.AreEqual(DisplayStyle.None, board.Information.Root.resolvedStyle.display,
                "Hovering the opponent army must not open its information window.");
            // UI-100: a right click reads the army's region without touching the planned move.
            MapContourProbe.Inspect(opponentArmy, opponentArmy.worldBound.center);
            await room.Settle();
            Assert.NotNull(board.Information.Root.Q<VisualElement>("inspected-nation-isengard"));
            Assert.That(board.Information.Root.Q<Label>("inspected-region-state").text, Does.Contain("Stronghold").And.Contain("Isengard").And.Contain("Shadow control"));
            Assert.AreEqual(selected, board.Map.Selected, "Reading an opponent army must not select a movement target.");
            Assert.IsNull(board.Tray.Q<Button>("confirm-decision"));
            Assert.Contains("osgiliath", board.Map.DraftOverlay.Targets.ToList());
            CollectionAssert.AreEquivalent(destinations, board.Map.DraftOverlay.Targets);
            Assert.IsTrue(board.OwnDice.Q<Button>(cost.Id).ClassListContains("selected"));
            Assert.AreEqual(pan, board.Map.Camera.Pan);
            Assert.AreEqual(zoom, board.Map.Camera.Zoom);
            Assert.IsTrue(board.Cards.Pinned);
            CardNetworkTests.Click(board.Fellowship.Root.Q<Button>("focus-fellowship"));
            await room.Settle();
            CollectionAssert.AreEquivalent(destinations, board.Map.DraftOverlay.Targets);
            Assert.Contains("osgiliath", board.Map.DraftOverlay.Targets.ToList());
            board.Map.Camera.Pan = pan;
            board.Map.Camera.Zoom = zoom;
            board.Map.Refresh();
            MapContourProbe.Inspect(board.Map, MapContourProbe.ReachRegion(board.Map, "minas-tirith"));
            await room.Settle();
            PointerGestureEvents.ClickAt(board.Information.Root.Q<Button>("inspect-characters"));
            await room.Settle();
            Assert.IsTrue(board.Information.Root.Query<Label>().ToList().Any(label => label.text == "Strider"));
            board.Map.Camera.Pan = pan;
            board.Map.Camera.Zoom = zoom;
            board.Map.Refresh();

            board.Nations.Open();
            await room.Settle();
            CardNetworkTests.Click(board.Nations.Drawer.Q<Button>("nation-status-gondor"));
            await room.Settle();
            Assert.That(board.Nations.Drawer.Q<Button>("nation-status-gondor").tooltip, Does.Contain("At war"));
            Assert.AreEqual(DisplayStyle.Flex, board.Information.Root.resolvedStyle.display,
                "Opening Politics preserves the explicitly opened information window (UI-32).");
            Assert.AreEqual(DisplayStyle.Flex, board.Nations.Drawer.resolvedStyle.display);
            Assert.IsNull(board.Tray.Q<Button>("confirm-decision"));
            Assert.Contains("osgiliath", board.Map.DraftOverlay.Targets.ToList());
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                CardNetworkTests.Click(client.Board.Root.Q<Button>("open-log"));
                await room.Settle();
                await room.Until(() => client.Board.Log.Model.Loaded);
                AssertHistory(client.Board);
            }
            Assert.IsNull(room.Sh.Board.View.Decision, "Public information is also available while waiting.");
            StringAssert.DoesNotContain("SH private history fixture", InformationText(board));
            StringAssert.DoesNotContain("FP private history fixture", InformationText(room.Sh.Board));
            var connected = room.Fp.Connected;
            room.Fp.Connection.Reconnect();
            AssertHistory(board);
            await room.Until(() => room.Fp.Connected > connected);
            AssertHistory(board);
            Assert.AreEqual(revision, board.View.Revision, "No information control may commit a game command.");
            Assert.AreEqual(pan, board.Map.Camera.Pan);
            Assert.AreEqual(zoom, board.Map.Camera.Zoom);
            Assert.IsTrue(board.Cards.Pinned);
            CollectionAssert.Contains(board.Map.DraftOverlay.Targets, "osgiliath");
            await ReadOnlyReserveDrag(room);
        }

        private static async Task ReadOnlyReserveDrag(MatchTestRoom room)
        {
            var board = room.Fp.Board;
            board.Log.Close();
            var revision = board.View.Revision;
            var muster = board.View.Dice.First(d => d.Owner == board.View.You && d.Face == DieFace.Muster && d.Location == DieLocation.Available);
            CardNetworkTests.Click(board.OwnDice.Q<Button>(muster.Id));
            board.Map.Focus("minas-tirith");
            await room.Settle();
            // UI-181: the player opens the Reserve tab the Muster die lit.
            board.Reinforcements.Open();
            await room.Settle();
            var reserve = board.Reinforcements.Root.Q<Button>("reserve-token-gondor-Regular");
            CardNetworkTests.Click(reserve);
            await room.Settle();
            // UI-135: a reserve chosen in Reinforcements lights its settlements before anything is sent.
            var previewRegions = board.Map.DraftOverlay.Targets.ToArray();
            CollectionAssert.Contains(previewRegions, "minas-tirith", "Selecting a recruit highlights its settlements before submission.");
            // UI-100: reading the placement region with a right click keeps the unsubmitted recruitment preview.
            MapContourProbe.Inspect(board.Map, MapContourProbe.Terrain(board.Map, "minas-tirith"));
            await room.Settle();
            Assert.AreEqual(DisplayStyle.Flex, board.Information.Root.style.display.value);
            Assert.IsTrue(reserve.ClassListContains("selected"));
            CollectionAssert.AreEquivalent(previewRegions, board.Map.DraftOverlay.Targets,
                "Read-only inspection must preserve the unsubmitted recruitment preview.");
            Assert.IsNull(board.Tray.Q<Button>("confirm-decision"));
            Assert.AreEqual(revision, board.View.Revision);
            Assert.AreEqual(revision, room.Sh.Board.View.Revision);
        }

        private static void AssertHistory(BoardScreen board) => CollectionAssert.IsSubsetOf(
            board.View.History.Select(entry => entry.Text),
            board.Log.Model.Rows.Where(r => r.Step == null).SelectMany(r => r.Group.Steps).Select(s => s.Text));

        private static string InformationText(BoardScreen board) => string.Join("\n", board.Log.Model.Rows.Where(r => r.Step == null).SelectMany(r => r.Group.Steps).Select(s => s.Text));

        [UnityTest]
        public IEnumerator DenseInformationFitsBothSizesAndScalesBesidePinnedCards()
        {
            using var client = new MountedMatchClient("https://localhost", "information-layout");
            var layout = 0;
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Panel.scale = scale;
                    // UI-112: an opened ⚔ round stays read, so each layout brings the next round.
                    var dense = DenseView();
                    dense.LastBattleRound.Round += layout++;
                    client.Apply(dense);
                    client.Board.SetPlanning(true);
                    client.Board.Cards.Show(client.Board.View.Hand[0], true);
                    yield return Settle();
                    // UI-32 lets the player arrange pinned windows beside each other.
                    var cardHeader = client.Board.Cards.Root.Q(className: FloatingWindow.HandleClass);
                    cardHeader.Focus();
                    for (var move = 0; move < 30; move++)
                    {
                        using var key = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.RightArrow });
                        cardHeader.SendEvent(key);
                        yield return null;
                    }
                    yield return Settle();
                    client.Board.Map.Fit();
                    PointerGestureEvents.ClickAt(client.Board.Root.Q<Button>("last-battle"));
                    yield return Settle();
                    Assert.AreEqual(DisplayStyle.Flex, client.Board.BattleReport.Root.style.display.value);
                    // The report may cover the city; the player pans to it before reading it.
                    MapContourProbe.Inspect(client.Board.Map, MapContourProbe.ReachRegion(client.Board.Map, "minas-tirith"));
                    yield return Settle();
                    Assert.AreEqual(DisplayStyle.None, client.Board.BattleReport.Root.style.display.value);
                    CheckInformation(client.Board);
                    foreach (var tab in client.Board.Information.Root.Query<Button>().ToList().Where(b => b.name.StartsWith("inspect-force-")).Select(b => b.name).ToList())
                    {
                        CardNetworkTests.Click(client.Board.Information.Root.Q<Button>(tab));
                        yield return Settle();
                        CheckInformation(client.Board);
                    }
                    CardNetworkTests.Click(client.Board.Information.Root.Q<Button>("inspect-characters"));
                    yield return Settle();
                    Assert.AreEqual(10, client.Board.Information.Root.Query<Label>().ToList().Count(l => l.name.StartsWith("inspected-character-")));
                    CheckInformation(client.Board);
                    LayoutTests.Save(target, $"t22-information-{width}-{scale:0.00}.png");
                    // UI-41 ActionCardLayoutTests owns the replacement Log/card geometry.
                    CardNetworkTests.Click(client.Board.Root.Q<Button>("close-information"));
                    yield return Settle();
                    Assert.AreEqual(DisplayStyle.None, client.Board.Information.Root.resolvedStyle.display);
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
        }

        private static void CheckInformation(BoardScreen board)
        {
            var panel = board.Information.Root.worldBound;
            Assert.AreEqual(8, board.Nations.Drawer.Query<Button>(className: "nation-token").ToList().Count);
            Assert.That(board.Nations.Drawer.Q("nation-track-southrons").Q<Label>(className: "nation-name").text, Is.EqualTo("S & E"));
            Assert.IsFalse(panel.Overlaps(board.Nations.Root.worldBound),
                "The information window leaves the fixed Politics tab reachable.");
            Assert.IsNull(board.Root.Q<VisualElement>(className: "map-controls"), "UI-100 removed the map control row.");
            Assert.IsFalse(board.Nations.Root.worldBound.Overlaps(board.Cards.Root.worldBound));
            Assert.LessOrEqual(panel.yMax, board.Map.worldBound.yMax);
            Assert.IsFalse(panel.Overlaps(board.Cards.Root.worldBound), "Information must not cover the pinned card.");
            foreach (var button in board.Nations.Root.Query<Button>().ToList().Concat(board.Information.Root.Query<Button>().ToList()))
            {
                Assert.GreaterOrEqual(button.resolvedStyle.height, 32, button.name);
                Assert.GreaterOrEqual(button.worldBound.xMin, board.Map.worldBound.xMin, button.name);
                Assert.LessOrEqual(button.worldBound.xMax, board.Map.worldBound.xMax, button.name);
                Assert.LessOrEqual(button.worldBound.yMax, board.Map.worldBound.yMax, button.name);
            }
            if (board.Information.Root.Q<ScrollView>() == null)
                foreach (var label in board.Information.Root.Query<Label>().ToList())
                {
                    Assert.LessOrEqual(label.worldBound.yMax, panel.yMax, label.text);
                    Assert.LessOrEqual(label.worldBound.xMax, panel.xMax, label.text);
                }
            Assert.IsEmpty(board.Tray.Query<ScrollView>().ToList());
            Assert.IsEmpty(board.Tray.Query<TextField>().ToList());
            Assert.Greater(board.Map.worldBound.height, board.Root.worldBound.height / 2);
        }

        private static PlayerView DenseView()
        {
            var fixture = new BoardFixture(true);
            var view = fixture.View;
            view.MatchId = "information-layout";
            view.LastBattleRound = BattleScreenFixture.Create(CommandKind.ContinueBattle).Battle;
            view.Characters.Clear();
            foreach (var id in new[] { "gandalf-grey", "strider", "boromir", "legolas", "gimli", "merry", "pippin", "saruman", "witch-king", "mouth-of-sauron" })
                view.Characters.Add(new CharacterView
                {
                    Id = "base.character." + id, DefinitionId = "base.character." + id, Location = "minas-tirith", Leadership = 1,
                    Side = id is "saruman" or "witch-king" or "mouth-of-sauron" ? Side.Shadow : Side.FreePeoples
                });
            fixture.Put("isengard", UnitKind.Regular, "minas-tirith", 4);
            fixture.Put("gondor", UnitKind.Elite, "minas-tirith", 2).Garrison = true;
            view.Hand = view.Hand.Take(6).ToList();
            view.Nations.First(n => n.Id == "north").Active = false;
            view.Nations.First(n => n.Id == "north").StepsToWar = 3;
            for (var i = 0; i < 50; i++)
                view.History.Add(new HistoryEntry { Revision = i, Text = "Free Peoples moved an army into Minas Tirith. The Shadow player will now choose the next action." });
            return view;
        }

        private static Fixture Configuration()
        {
            var path = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(path))
                Assert.Ignore("Run scripts/test-client.sh for real HTTP/WebSocket integration.");
            return JsonConvert.DeserializeObject<Fixture>(File.ReadAllText(path));
        }

        private static async Task Until(Func<bool> condition, CancellationToken cancellation)
        {
            while (!condition())
                await Task.Delay(20, cancellation);
        }

        private static IEnumerator Settle()
        {
            for (var frame = 0; frame < 4; frame++)
                yield return null;
        }

        private sealed class Fixture
        {
            public string Address, FreePeoplesUser, Password;
            public Dictionary<string, string> BaseAcceptanceMatches;
        }

        private sealed class InitialReadBoundary : DelegatingHandler
        {
            private readonly string path;
            private readonly TaskCompletionSource<bool> gate = new TaskCompletionSource<bool>();
            private bool first = true;
            public bool Held { get; private set; }
            public int Commands { get; private set; }
            public InitialReadBoundary(string path) : base(new HttpClientHandler { AllowAutoRedirect = false }) => this.path = path;
            public void Release() => gate.TrySetResult(true);

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
            {
                var hold = first && request.Method == HttpMethod.Get && request.RequestUri.AbsolutePath == path;
                if (hold)
                    first = false;
                if (request.RequestUri.AbsolutePath == path + "/commands")
                    Commands++;
                var response = await base.SendAsync(request, cancellation);
                if (!hold)
                    return response;
                Held = true;
                await gate.Task;
                response.Dispose();
                throw new HttpRequestException("Lost the real initial projection before delivery.");
            }

            protected override void Dispose(bool disposing)
            {
                Release();
                base.Dispose(disposing);
            }
        }
    }
}
