using System;
using System.Collections;
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
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public class MovementNetworkTests
    {
        [UnityTest]
        public IEnumerator TwoMountedMatchClientsMoveRecruitAndRecoverWithoutPayingTwice()
        {
            var path = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(path))
                Assert.Ignore("Run scripts/test-client.sh for real HTTP/WSS and PostgreSQL integration.");
            var fixture = JsonConvert.DeserializeObject<Fixture>(File.ReadAllText(path));
            var task = Run(fixture);
            var deadline = Time.realtimeSinceStartup + 90;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsTrue(task.IsCompleted, "Movement integration timed out.");
            task.GetAwaiter().GetResult();
        }

        private static async Task Run(Fixture fixture)
        {
            using (var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(80)))
            {
                var token = lifetime.Token;
                var loss = new CommandBoundary();
                using (var fp = new MountedMatchClient(fixture.Address, fixture.MovementMatch, loss))
                using (var sh = new MountedMatchClient(fixture.Address, fixture.MovementMatch))
                {
                    await fp.Api.Login(fixture.FreePeoplesUser, fixture.Password, token);
                    await sh.Api.Login(fixture.ShadowUser, fixture.Password, token);
                    fp.Connection.Start();
                    sh.Connection.Start();
                    await Until(() => fp.Connected > 0 && sh.Connected > 0, token);
                    await Task.Delay(150, token);
                    AssertConsistent(fp, sh, 0);
                    await RejectIllegalTarget(fp, token);
                    await MoveAndRecover(fp, sh, loss, token);
                    await Recruit(sh, "sauron", "barad-dur", true, token);
                    await BothAt(fp, sh, 3, token);
                    Assert.AreEqual(Side.FreePeoples, fp.Board.View.DecisionOwner);
                    await Recruit(fp, "gondor", "pelargir", false, token);
                    await BothAt(fp, sh, 4, token);
                    Assert.AreEqual(Side.Shadow, sh.Board.View.DecisionOwner);
                    await Move(sh, "orthanc", "gap-of-rohan", token);
                    await BothAt(fp, sh, 5, token);
                    await Finish(sh, token);
                    await BothAt(fp, sh, 6, token);
                    foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
                        Assert.AreEqual(2, fp.Board.View.Dice.Count(d => d.Owner == side && d.Location == DieLocation.Spent));
                    Assert.IsTrue(fp.Board.View.Units.Any(u => u.Nation == "isengard" && u.Location == "gap-of-rohan"));
                    Assert.IsFalse(fp.Board.View.Units.Any(u => u.Location == "orthanc"));
                    Assert.IsTrue(fp.Board.View.Units.Any(u => u.Nation == "sauron" && u.Kind == UnitKind.Elite && u.Location == "barad-dur"));
                    Assert.IsTrue(fp.Board.View.Units.Any(u => u.Nation == "gondor" && u.Kind == UnitKind.Elite && u.Location == "pelargir"));
                    Assert.IsEmpty(fp.Board.Tray.Query<TextField>().ToList());
                    Assert.IsEmpty(sh.Board.Tray.Query<ScrollView>().ToList());
                }
            }
        }

        private static async Task RejectIllegalTarget(MountedMatchClient fp, CancellationToken token)
        {
            var view = fp.Board.View;
            var command = new GameCommand
            {
                MatchId = view.MatchId,
                CommandId = Guid.NewGuid().ToString("N"),
                ExpectedRevision = view.Revision,
                DecisionId = view.Decision.Id,
                Kind = CommandKind.MoveArmies,
                Payload = new CommandPayload
                {
                    DieId = view.Dice.First(d => d.Owner == view.You && d.Face == DieFace.Army).Id,
                    MovementCost = DieFace.Army,
                    Moves = { new ArmyMoveChoice { From = "minas-tirith", To = "rivendell",
                        UnitIds = view.Units.Where(u => u.Location == "minas-tirith").Select(u => u.Id).ToList() } }
                }
            };
            var receipt = await fp.Api.Post<CommandReceipt>($"api/matches/{view.MatchId}/commands", command, token);
            Assert.IsFalse(receipt.Accepted);
            var after = await fp.Api.Get<PlayerView>($"api/matches/{view.MatchId}", token);
            Assert.AreEqual(JsonConvert.SerializeObject(view), JsonConvert.SerializeObject(after), "Rejected targets cannot move, pay, advance or consume randomness.");
        }

        private static async Task MoveAndRecover(MountedMatchClient fp, MountedMatchClient sh, CommandBoundary loss, CancellationToken token)
        {
            var before = loss.Commands;
            loss.DropNext = true;
            await Move(fp, "minas-tirith", "osgiliath", token);
            await Until(() => loss.Dropped, token);
            await BothAt(fp, sh, 1, token);
            Assert.AreEqual(before + 1, loss.Commands);
            var committed = loss.Last;
            var camera = fp.Board.Map.Camera;
            camera.Pan = new Vector2(-270, -43);
            camera.Zoom = 1.4f;
            fp.Board.Map.Refresh();
            var pan = camera.Pan;
            var connections = fp.Connected;
            fp.Connection.Reconnect();
            await Until(() => fp.Connected > connections, token);
            Assert.AreEqual(pan, camera.Pan);
            Assert.AreEqual(1.4f, camera.Zoom);
            await Until(() => loss.ReceiptReads > 0 && RecoveryButton(fp) == null, token);
            Assert.AreEqual(committed.CommandId, loss.ReceiptId, "Reconnect recovers the original command automatically.");
            Assert.AreEqual(before + 1, loss.Commands, "Recovery reads the saved receipt instead of creating another command.");
            var retry = await fp.Api.Post<CommandReceipt>($"api/matches/{committed.MatchId}/commands", committed, token);
            Assert.IsTrue(retry.Accepted);
            Assert.AreEqual(1, retry.Revision);
            await fp.Connection.Refresh();
            AssertConsistent(fp, sh, 1);
            Assert.AreEqual(1, fp.Board.View.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Resolving));
            Assert.AreEqual(DecisionKind.ActionSequence, fp.Board.View.Decision.Kind);
            await Finish(fp, token);
            await BothAt(fp, sh, 2, token);
            Assert.IsFalse(fp.Board.View.Units.Any(u => u.Location == "minas-tirith"));
            Assert.AreEqual(Side.Shadow, sh.Board.View.DecisionOwner);
            fp.Board.Map.Fit();
        }

        private static Button RecoveryButton(MountedMatchClient client) => client.Board.Tray.Query<Button>().ToList().FirstOrDefault(b => b.text == "Check result");

        // UI-105: the force is selected on the map and moves on the click of its destination; it is never dragged.
        private static async Task Move(MountedMatchClient client, string from, string to, CancellationToken token)
        {
            var board = client.Board;
            board.Map.Focus(from);
            await Task.Delay(100, token);
            var armyDie = board.View.Dice.First(d => d.Owner == board.View.You && d.Face == DieFace.Army && d.Location == DieLocation.Available);
            Click(board.OwnDice.Q<Button>(armyDie.Id));
            await Task.Delay(100, token);
            await ArmyGestures.All(client, from);
            await Until(() => board.Map.DraftOverlay.Targets.Contains(to), token);
            await Task.Delay(100, token);
            board.Hover.Close();
            await Task.Delay(80, token);
            MapContourProbe.Click(board.Map, MapContourProbe.Terrain(board.Map, to));
        }

        private static async Task Recruit(MountedMatchClient client, string nation, string region, bool drag, CancellationToken token)
        {
            var board = client.Board;
            var before = board.View.Units.Count(u => u.Nation == nation && u.Kind == UnitKind.Elite && u.Location == region);
            var die = board.View.Dice.First(d => d.Owner == board.View.You && d.Location == DieLocation.Available &&
                d.Face is DieFace.Muster or DieFace.ArmyMuster or DieFace.WillOfTheWest);
            ArmyGestures.Die(client, die.Id, DieFace.Muster);
            board.Map.Focus(region);
            // UI-181: a Muster die lights the Reserve tab; the player opens it.
            board.Reinforcements.Open();
            await Until(() => board.Reinforcements.Root.Q<Button>($"reserve-token-{nation}-Elite")?.worldBound.width > 0, token);
            await Task.Delay(100, token);
            var reserve = board.Reinforcements.Root.Q<Button>($"reserve-token-{nation}-Elite");
            if (drag)
                await Drag(board, reserve, board.Map.Q<Button>($"army-{region}-{board.View.You}").worldBound.center, token);
            else
            {
                Click(reserve);
                await Until(() => board.Map.DraftOverlay.Targets.Contains(region), token);
                board.Hover.Close();
                await Task.Delay(80, token);
                MapContourProbe.Click(board.Map, MapContourProbe.Terrain(board.Map, region));
            }
            await Until(() => board.View.Units.Count(u => u.Nation == nation && u.Kind == UnitKind.Elite && u.Location == region) == before + 1, token);
        }

        private static async Task BothAt(MountedMatchClient fp, MountedMatchClient sh, long revision, CancellationToken token)
        {
            await Until(() => fp.Board.View?.Revision == revision && sh.Board.View?.Revision == revision, token);
            AssertConsistent(fp, sh, revision);
        }

        private static void AssertConsistent(MountedMatchClient fp, MountedMatchClient sh, long revision)
        {
            var first = fp.Board.View;
            var second = sh.Board.View;
            Assert.AreEqual(revision, first.Revision);
            Assert.AreEqual(revision, second.Revision);
            Assert.AreEqual(JsonConvert.SerializeObject(first.Units), JsonConvert.SerializeObject(second.Units));
            Assert.AreEqual(JsonConvert.SerializeObject(first.Dice), JsonConvert.SerializeObject(second.Dice));
            Assert.AreEqual(JsonConvert.SerializeObject(first.Control), JsonConvert.SerializeObject(second.Control));
            Assert.AreEqual(JsonConvert.SerializeObject(first.Nations), JsonConvert.SerializeObject(second.Nations));
            Assert.AreEqual(first.DecisionOwner, second.DecisionOwner);
        }

        private static async Task Until(Func<bool> condition, CancellationToken token)
        {
            while (!condition())
                await Task.Delay(30, token);
        }

        private static void Click(VisualElement target) => CardNetworkTests.Click(target);

        private static async Task Finish(MountedMatchClient client, CancellationToken token)
        {
            await Until(() => client.Board.Console.Actions.Confirm.enabledInHierarchy, token);
            await Task.Delay(100, token);
            Click(client.Board.Console.Actions.Confirm);
        }

        private static async Task Drag(BoardScreen board, VisualElement source, Vector2 destination, CancellationToken token)
        {
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = source.worldBound.center }))
                source.SendEvent(down);
            await Task.Delay(30, token);
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 0, mousePosition = destination }))
                source.SendEvent(move);
            await Task.Delay(30, token);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = destination }))
                board.Root.SendEvent(up);
        }

        private sealed class CommandBoundary : DelegatingHandler
        {
            public bool DropNext;
            public bool Dropped;
            public int Commands;
            public int ReceiptReads;
            public string ReceiptId;
            public GameCommand Last;
            public CommandBoundary() : base(new HttpClientHandler { AllowAutoRedirect = false }) { }
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                var command = request.RequestUri.AbsolutePath.EndsWith("/commands", StringComparison.Ordinal);
                if (command)
                {
                    Commands++;
                    Last = JsonConvert.DeserializeObject<GameCommand>(await request.Content.ReadAsStringAsync());
                }
                var response = await base.SendAsync(request, token);
                if (request.Method == HttpMethod.Get && request.RequestUri.AbsolutePath.Contains("/receipts/") && response.IsSuccessStatusCode)
                {
                    ReceiptReads++;
                    ReceiptId = request.RequestUri.Segments.Last();
                }
                if (command && DropNext)
                {
                    DropNext = false;
                    Dropped = true;
                    response.Dispose();
                    throw new HttpRequestException("Lost response after production server committed the move.");
                }
                return response;
            }
        }

        private sealed class Fixture
        {
            public string Address;
            public string MovementMatch;
            public string FreePeoplesUser;
            public string ShadowUser;
            public string Password;
        }
    }
}
