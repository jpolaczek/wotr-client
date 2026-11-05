using System;
using System.Collections;
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
using static Wotr.PlayTests.ReturnTestFixture;

namespace Wotr.PlayTests
{
    public sealed partial class BoardMenuNetworkTests
    {
        [UnityTest]
        public IEnumerator TravelCardBattleHuntAndSetupDraftsSurviveSettingsAndGamesWithoutSubmission() => MatchTestRoom.Wait(Families());

        [UnityTest]
        public IEnumerator OfflineMenuPreservesTheDraftAndAllowsGamesAfterOnlyLocalWrites() => MatchTestRoom.Wait(Offline());

        private static async Task Families()
        {
            var fixture = Load();
            var storage = Temporary("families");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(150));
            var token = timeout.Token;
            try
            {
                foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
                {
                    var boundary = new UI10HttpBoundary();
                    var api = await fixture.Login(side, token, boundary);
                    using var enemy = await fixture.Login(side == Side.FreePeoples ? Side.Shadow : Side.FreePeoples, token);
                    using var client = new MountedFlowClient(api, storage);
                    // UI-142: a Hunt response is a complete choice and is sent at once, so it is no longer a local draft.
                    var names = side == Side.FreePeoples ? new[] { "reveal", "no-regular", "council" } : new[] { "card" };
                    foreach (var name in names)
                    {
                        var match = fixture.ReturnMatches["ui70-" + name];
                        boundary.MatchPath = "/api/matches/" + match;
                        var other = JsonConvert.SerializeObject(await enemy.Get<PlayerView>(boundary.MatchPath, token));
                        await Open(client, match, token);
                        var revision = client.Board.View.Revision;
                        await SelectDraft(client, name, token);
                        Assert.AreEqual(0, boundary.Commands, name + " must remain a local draft.");
                        Assert.AreEqual(revision, client.Board.View.Revision, name);
                        await client.LocalWrites;
                        var saved = await LocalMatchPersistence.Open(storage, api.Address.AbsoluteUri, api.AccountId, match);
                        Assert.IsNotNull(saved.Draft, name);
                        AssertSelected(name, saved.Draft.Draft);
                        var expected = JsonConvert.SerializeObject(saved.Draft);
                        if (name == "reveal")
                            await Settings(client, token);
                        await Games(client, token);
                        await Until(() => client.Board == null, token);
                        Assert.AreEqual(0, boundary.Commands, name);
                        await Open(client, match, token);
                        await client.LocalWrites;
                        var restored = await LocalMatchPersistence.Open(storage, api.Address.AbsoluteUri, api.AccountId, match);
                        Assert.AreEqual(expected, JsonConvert.SerializeObject(restored.Draft), name);
                        Assert.AreEqual(revision, client.Board.View.Revision, name);
                        Assert.AreEqual(other, JsonConvert.SerializeObject(await enemy.Get<PlayerView>(boundary.MatchPath, token)),
                            "Private drafts and the menu cannot change the opponent's projection: " + name);
                        Assert.IsNull(restored.Actions.Pending);
                        await Games(client, token);
                        await Until(() => client.Board == null, token);
                    }
                    client.Flow.Dispose();
                    await client.LocalWrites;
                }
            }
            finally { Delete(storage); }
        }

        private static async Task SelectDraft(MountedFlowClient client, string name, CancellationToken token)
        {
            var board = client.Board;
            switch (name)
            {
                case "reveal":
                    var target = board.Map.DraftOverlay.Targets.First();
                    board.Fellowship.Close();
                    board.Hunt.Close();
                    await Task.Delay(80, token);
                    // Route steps now use highlighted terrain, not floating target buttons.
                    MapContourProbe.ReachRegion(board.Map, target);
                    await Task.Delay(80, token);
                    MapContourProbe.Click(board.Map, MapContourProbe.Terrain(board.Map, target));
                    break;
                case "no-regular":
                    board.Battles.Open();
                    var elite = board.View.Battle.Attacker.Units.Single(u => u.Kind == UnitKind.Elite).Id;
                    await Press(client.Root, "loss-" + elite, token);
                    break;
                case "council":
                    board.Fellowship.Open();
                    await Press(client.Root, "fellowship-portrait-base.character.gandalf-grey", token);
                    await Press(client.Root, "version-lords.character.gandalf-grey", token);
                    break;
                case "card":
                    var die = board.View.Options.Single(o => o.Kind == CommandKind.DiscardProtection).EligibleDice[0];
                    await Press(client.Root, die, token);
                    board.TableEffects.Open();
                    await Press(client.Root, "remove-table-base.fp.strategy.02", token);
                    // UI-91: the hand is permanent; no Cards tab to open.
                    await Press(client.Root, "base.sh.strategy.01", token);
                    break;
                default:
                    Assert.Fail("Uncovered draft family: " + name);
                    break;
            }
        }

        private static void AssertSelected(string name, DraftSnapshot draft)
        {
            switch (name)
            {
                case "reveal": Assert.IsNotEmpty(draft.Payload.Route); break;
                case "no-regular": Assert.AreEqual(1, draft.Payload.Losses.Count); break;
                case "council": CollectionAssert.Contains(draft.Payload.Ids, "lords.character.gandalf-grey"); break;
                case "card": CollectionAssert.Contains(draft.Payload.Ids, "base.sh.strategy.01"); break;
            }
        }

        private static async Task Settings(MountedFlowClient client, CancellationToken token)
        {
            var board = client.Board;
            var windows = board.Windows;
            windows.Remember(board.Hunt.Root.name, new Vector2(17, 21));
            await Press(client.Root, "open-board-menu", token);
            await Press(client.Root, "open-interface", token);
            Assert.IsTrue(client.Interface.Settings.IsOpen);
            foreach (var scale in new[] { 1.25f, 1f })
            {
                await Press(client.Root, scale == 1f ? "scale-100" : "scale-125", token);
                Assert.AreEqual(scale, client.Panel.scale);
            }
            var motion = client.Interface.Settings.Root.Q<Toggle>("reduced-motion");
            var previous = motion.value;
            PointerGestureEvents.ClickAt(motion);
            await Task.Delay(100, token);
            Assert.AreEqual(!previous, client.Interface.Preferences.ReducedMotion);
            await Press(client.Root, "reset-window-layout", token);
            Assert.IsFalse(windows.TryGet(board.Hunt.Root.name, out _));
            BoardMenuLayoutTests.Key(client.Interface.Settings.Root.Q<Button>("close-interface"), true);
            BoardMenuLayoutTests.Key(board.Root, false);
            await Task.Delay(80, token);
            Assert.IsFalse(client.Interface.Settings.IsOpen);
            Assert.AreSame(board.Menu.Toggle, board.Root.panel.focusController.focusedElement);
        }

        private static async Task Offline()
        {
            var fixture = Load();
            var storage = Temporary("offline");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(110));
            using var server = new RestartTestServer();
            var token = timeout.Token;
            var match = fixture.ReturnMatches["ui70-draft"];
            try
            {
                await server.Start(token);
                var boundary = new UI10HttpBoundary { MatchPath = "/api/matches/" + match };
                var api = await fixture.Login(Side.FreePeoples, token, boundary, server.Address);
                using var client = new MountedFlowClient(api, storage);
                await Open(client, match, token);
                var die = client.Board.View.Options.First(o => o.Kind == CommandKind.MoveArmies).EligibleDice.Last();
                await Press(client.Root, die, token);
                await client.LocalWrites;
                var exact = JsonConvert.SerializeObject((await LocalMatchPersistence.Open(storage, api.Address.AbsoluteUri, api.AccountId, match)).Draft);
                server.Stop();
                await Until(() => client.Board.Connection.text != "Live", token);
                Assert.AreEqual(die, client.Board.ActionDie.DieId);
                await Games(client, token);
                await Until(() => client.Board == null, token);
                Assert.AreEqual(exact, JsonConvert.SerializeObject((await LocalMatchPersistence.Open(storage, api.Address.AbsoluteUri, api.AccountId, match)).Draft));
                Assert.AreEqual(0, boundary.Commands);
                client.Flow.Dispose();
                await client.LocalWrites;
            }
            finally { Delete(storage); }
        }
    }
}
