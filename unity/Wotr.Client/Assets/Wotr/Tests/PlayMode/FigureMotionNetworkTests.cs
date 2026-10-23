using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
    public sealed class FigureMotionNetworkTests
    {
        [UnityTest]
        public IEnumerator BothSidesSeeAcceptedArmiesMoveAndRejectedOrRetriedCommandsDoNotReplay() => MatchTestRoom.Wait(Armies());

        [UnityTest]
        public IEnumerator PublicCharacterJourneyMovesTheSameFigureOnBothClients() => MatchTestRoom.Wait(Character());

        [UnityTest]
        public IEnumerator HiddenProgressAndSeparationNeverInventTheSecretFellowshipPosition() => MatchTestRoom.Wait(Fellowship());

        private static async Task Armies()
        {
            var fixture = Load();
            using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var token = lifetime.Token;
            using var fp = new MountedMatchClient(fixture.Address, fixture.MovementMatch);
            using var sh = new MountedMatchClient(fixture.Address, fixture.MovementMatch);
            await fp.Api.Login(fixture.FreePeoplesUser, fixture.Password, token);
            await sh.Api.Login(fixture.ShadowUser, fixture.Password, token);
            fp.Connection.Start();
            sh.Connection.Start();
            await Until(() => fp.Connected > 0 && sh.Connected > 0, token);
            fp.Board.Map.Fit();
            sh.Board.Map.Fit();
            Assert.AreEqual(0, fp.Board.Map.Figures.ActiveCount);
            Assert.AreEqual(0, sh.Board.Map.Figures.ActiveCount);
            var invalid = ArmyCommand(fp, "minas-tirith", "rivendell");
            var rejected = await Post(fp, invalid, token);
            Assert.IsFalse(rejected.Accepted);
            Assert.AreEqual(0, fp.Board.Map.Figures.ActiveCount);
            Assert.AreEqual(0, sh.Board.Map.Figures.ActiveCount);

            var fpBefore = fp.Board.View;
            var first = ArmyCommand(fp, "minas-tirith", "osgiliath");
            Assert.IsTrue((await Post(fp, first, token)).Accepted);
            await Until(() => fp.Board.View.Revision == 1, token);
            AssertJourney(fp, "minas-tirith", "osgiliath", Side.FreePeoples);
            var fpAfter = fp.Board.View;
            var before = fp.Board.Map.Figures.Root.Children().First().transform.position;
            await Task.Delay(240, token);
            var during = fp.Board.Map.Figures.Root.Children().First().transform.position;
            Assert.Greater(Vector2.Distance(before, during), 2);
            LayoutTests.Save(fp.Viewport, "ui76-network-army-fp-middle.png");
            // UI-305: the observer first presents the opponent's march (its die, the camera if needed); the move then
            // plays on its board when the playback releases the view.
            await Until(() => sh.Board.View.Revision == 1, token);
            AssertJourney(sh, "minas-tirith", "osgiliath", Side.FreePeoples);
            await Task.Delay(240, token);
            LayoutTests.Save(sh.Viewport, "ui76-network-army-sh-observer-middle.png");
            await Task.Delay(1100, token);
            Assert.AreEqual(0, fp.Board.Map.Figures.ActiveCount);
            Assert.AreEqual(0, sh.Board.Map.Figures.ActiveCount);
            foreach (var client in new[] { fp, sh })
            {
                Assert.IsFalse(client.Board.Map.MarkerVisibility.Detail);
                Assert.IsTrue(client.Board.Map.Figures.Reveals("osgiliath"));
                Assert.AreEqual(DisplayStyle.Flex, client.Board.Map.Q<Button>("army-osgiliath-FreePeoples").resolvedStyle.display);
            }
            Assert.IsTrue((await Post(fp, first, token)).Accepted, "Receipt retry remains accepted without a new revision.");
            await fp.Connection.Refresh();
            Assert.AreEqual(0, fp.Board.Map.Figures.ActiveCount);

            var shBefore = sh.Board.View;
            var second = ArmyCommand(sh, "orthanc", "gap-of-rohan");
            Assert.IsTrue((await Post(sh, second, token)).Accepted);
            await Until(() => sh.Board.View.Revision == 2, token);
            AssertJourney(sh, "orthanc", "gap-of-rohan", Side.Shadow);
            await Until(() => fp.Board.View.Revision == 2, token);
            AssertJourney(fp, "orthanc", "gap-of-rohan", Side.Shadow);
            Export("armies", new Dictionary<string, PlayerView>
            {
                ["motion-army-fp"] = fpBefore, ["motion-army-fp-after"] = fpAfter,
                ["motion-army-sh"] = shBefore, ["motion-army-sh-after"] = sh.Board.View
            });
            var connected = fp.Connected;
            fp.Connection.Reconnect();
            await Until(() => fp.Connected > connected, token);
            Assert.AreEqual(0, fp.Board.Map.Figures.ActiveCount, "Reconnect reads the current result without replay.");
            Assert.IsFalse(fp.Board.Map.Figures.Reveals("gap-of-rohan"));
            Assert.AreEqual("gap-of-rohan", fp.Board.View.Units.First(u => u.Id == second.Payload.Moves[0].UnitIds[0]).Location);
        }

        private static async Task Character()
        {
            using var room = await MatchTestRoom.Connect("travel-drag", ScenarioKind.Character, openPanels: false);
            var view = room.Fp.Board.View;
            var shadowBefore = room.Sh.Board.View;
            var figure = view.Characters.Single(c => c.DefinitionId == "base.character.gimli");
            var command = Command(view, CommandKind.MoveCompanions, new CommandPayload
            {
                DieId = CharacterDie(view),
                CompanionMoves = { new CompanionMoveChoice { Ids = { figure.Id }, Route = { "hollin", "fords-of-bruinen" } } }
            });
            Assert.IsTrue((await Post(room.Fp, command, room.Token)).Accepted);
            await room.Until(() => room.Fp.Board.View.Revision == 1 && room.Sh.Board.View.Revision == 1);
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                var move = client.Board.Map.Figures.Moves.Single(m => m.To.Key == "character:" + figure.Id);
                Assert.AreEqual("hollin", move.From.Region);
                Assert.AreEqual("fords-of-bruinen", move.To.Region);
                Assert.IsFalse(move.IsArrival);
            }
            await Task.Delay(220, room.Token);
            LayoutTests.Save(room.Fp.Viewport, "ui76-network-character-middle.png");
            Export("character", new Dictionary<string, PlayerView>
            {
                ["motion-character-fp"] = view, ["motion-character-fp-after"] = room.Fp.Board.View,
                ["motion-character-sh"] = shadowBefore, ["motion-character-sh-after"] = room.Sh.Board.View
            });
        }

        private static async Task Fellowship()
        {
            using var room = await MatchTestRoom.Connect("separate-drag", ScenarioKind.Character, openPanels: false);
            var initial = room.Fp.Board.View.Fellowship.RegionId;
            var fpBefore = room.Fp.Board.View;
            var shBefore = room.Sh.Board.View;
            Assert.IsTrue(room.Fp.Board.View.Fellowship.Progress > 0);
            var move = Command(room.Fp.Board.View, CommandKind.MoveFellowship,
                new CommandPayload { DieId = CharacterDie(room.Fp.Board.View) });
            Assert.IsTrue((await Post(room.Fp, move, room.Token)).Accepted);
            await room.At(1);
            var fpMoved = room.Fp.Board.View;
            var shMoved = room.Sh.Board.View;
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                Assert.AreEqual(initial, client.Board.View.Fellowship.RegionId);
                Assert.AreEqual(0, client.Board.Map.Figures.ActiveCount);
            }
            var discard = Command(room.Sh.Board.View, CommandKind.DiscardDie,
                new CommandPayload { DieId = room.Sh.Board.View.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available).Id });
            Assert.IsTrue((await Post(room.Sh, discard, room.Token)).Accepted);
            await room.At(2);
            var fpBeforeSeparation = room.Fp.Board.View;
            var shBeforeSeparation = room.Sh.Board.View;
            var separate = Command(room.Fp.Board.View, CommandKind.SeparateCompanions, new CommandPayload
            {
                DieId = CharacterDie(room.Fp.Board.View), Ids = { "base.character.gimli" },
                Route = { "rivendell", "fords-of-bruinen" }
            });
            Assert.IsTrue((await Post(room.Fp, separate, room.Token)).Accepted);
            // UI-305: each board when it shows the separation — the Free Peoples' at once, the Shadow's after presenting it.
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                await room.Until(() => client.Board.View.Revision == 3);
                var arrival = client.Board.Map.Figures.Moves.Single(m => m.To.Key == "character:base.character.gimli");
                Assert.IsTrue(arrival.IsArrival, "Unknown fellowship progress cannot become a claimed map origin.");
                Assert.AreEqual("fords-of-bruinen", arrival.From.Region);
                Assert.AreEqual("fords-of-bruinen", arrival.To.Region);
                Assert.IsTrue(client.Board.Map.Figures.Root.Children().Single().ClassListContains("figure-arrival"));
            }
            Export("fellowship", new Dictionary<string, PlayerView>
            {
                ["motion-hidden-fp"] = fpBefore, ["motion-hidden-fp-after"] = fpMoved,
                ["motion-hidden-sh"] = shBefore, ["motion-hidden-sh-after"] = shMoved,
                ["motion-separation-fp"] = fpBeforeSeparation, ["motion-separation-fp-after"] = room.Fp.Board.View,
                ["motion-separation-sh"] = shBeforeSeparation, ["motion-separation-sh-after"] = room.Sh.Board.View
            });
            await AssertSeparationProfilePreservesGuide(fpBeforeSeparation, room.Fp.Board.View, room.Token);
        }

        private static async Task AssertSeparationProfilePreservesGuide(PlayerView before, PlayerView after,
            CancellationToken token)
        {
            Assert.AreEqual(DecisionKind.Guide, after.Decision.Kind);
            var expected = JsonConvert.SerializeObject(after);
            var views = new Dictionary<string, PlayerView>
            {
                ["motion-separation-fp"] = before,
                ["motion-separation-fp-after"] = after
            };
            using var mount = new MountedMatchClient("http://localhost", "separation-profile-validation");
            mount.Board.Root.RemoveFromHierarchy();
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    mount.Panel.targetTexture = target;
                    mount.Interface.Preferences.SetScale(scale);
                    mount.Interface.Preferences.SetReducedMotion(scale > 1);
                    try
                    {
                        using var driver = new BoardProfileDriverTests.Driver(mount.Interface.Content,
                            before, "motion-separation-fp", views);
                        await Task.Delay(100, token);
                        driver.Board.Map.Fit();
                        await Task.Delay(100, token);
                        foreach (var frame in new[] { 0, 15, 30, 45, 120, 135, 150 })
                        {
                            Assert.IsTrue(driver.Tick(frame));
                            await Task.Delay(100, token);
                            driver.Verify();
                            var board = driver.Board;
                            Assert.IsFalse(board.Fellowship.Pinned, "Profiling must leave the pending Guide window closed.");
                            var inspecting = frame % 30 == 15;
                            Assert.AreEqual(inspecting ? DisplayStyle.Flex : DisplayStyle.None,
                                board.Information.Root.resolvedStyle.display,
                                $"Terrain click/Escape must work at {width}/{scale}, frame {frame}.");
                            if (inspecting)
                                Assert.AreEqual(board.Presentation.Regions["fords-of-bruinen"].Name,
                                    board.Information.Root.Q<Label>().text);
                            Assert.AreEqual(after.Decision.Id, board.View.Decision.Id);
                            Assert.AreEqual(expected, JsonConvert.SerializeObject(board.View));
                        }
                    }
                    finally
                    {
                        mount.Panel.targetTexture = mount.Viewport;
                        target.Release();
                        UnityEngine.Object.Destroy(target);
                    }
                }
            Assert.AreEqual(expected, JsonConvert.SerializeObject(after), "The real server projection stays untouched.");
        }

        private static void Export(string name, Dictionary<string, PlayerView> views)
        {
            // Only ordinary projections from the actual server enter the rendering input.
            var directory = Path.GetFullPath(".artifacts");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "ui77-motion-" + name + ".json"),
                JsonConvert.SerializeObject(views, Formatting.Indented));
        }

        private static void AssertJourney(MountedMatchClient client, string source, string target, Side side)
        {
            Assert.Greater(client.Board.Map.Figures.ActiveCount, 0);
            var moves = client.Board.Map.Figures.Moves;
            Assert.IsNotEmpty(moves);
            Assert.IsTrue(moves.All(m => m.From.Region == source && m.To.Region == target && m.To.Side == side));
            Assert.IsTrue(moves.All(m => client.Board.View.Units.Any(u => "unit:" + u.Id == m.To.Key && u.Location == target) ||
                client.Board.View.Characters.Any(c => "character:" + c.Id == m.To.Key && c.Location == target)));
        }

        private static GameCommand ArmyCommand(MountedMatchClient client, string source, string target)
        {
            var view = client.Board.View;
            return Command(view, CommandKind.MoveArmies, new CommandPayload
            {
                DieId = view.Dice.First(d => d.Owner == view.You && d.Face == DieFace.Army && d.Location == DieLocation.Available).Id,
                MovementCost = DieFace.Army,
                Moves = { new ArmyMoveChoice
                {
                    From = source, To = target,
                    UnitIds = view.Units.Where(u => u.Location == source && u.Side == view.You && !u.Garrison).Select(u => u.Id).ToList(),
                    CharacterIds = view.Characters.Where(c => c.Location == source && c.Side == view.You && !c.Garrison).Select(c => c.Id).ToList()
                } }
            });
        }

        private static string CharacterDie(PlayerView view) => view.Dice.First(d => d.Owner == view.You &&
            d.Face == DieFace.Character && d.Location == DieLocation.Available).Id;

        private static GameCommand Command(PlayerView view, CommandKind kind, CommandPayload payload) => new GameCommand
        {
            MatchId = view.MatchId, ExpectedRevision = view.Revision, DecisionId = view.Decision.Id,
            CommandId = Guid.NewGuid().ToString("N"), Kind = kind, Payload = payload
        };

        private static Task<CommandReceipt> Post(MountedMatchClient client, GameCommand command, CancellationToken token) =>
            client.Api.Post<CommandReceipt>($"api/matches/{command.MatchId}/commands", command, token);

        private static async Task Until(Func<bool> condition, CancellationToken token)
        {
            while (!condition())
                await Task.Delay(20, token);
        }

        private static Fixture Load()
        {
            var path = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(path))
                Assert.Ignore("Run scripts/test-client.sh for real HTTP/WebSocket and PostgreSQL integration.");
            return JsonConvert.DeserializeObject<Fixture>(File.ReadAllText(path));
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
