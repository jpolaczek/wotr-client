using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;
using static Wotr.PlayTests.ReturnTestFixture;

namespace Wotr.PlayTests
{
    public sealed partial class BoardMenuNetworkTests
    {
        [UnityTest]
        public IEnumerator FailedAndDelayedDraftWritesKeepTheBoardAliveAndRetryOnlySavesLocally() => MatchTestRoom.Wait(Storage());

        [UnityTest]
        public IEnumerator GamesDuringLostReceiptReturnsWithTheExactCommandAndNoSecondCost() => MatchTestRoom.Wait(Receipt());

        [UnityTest]
        public IEnumerator MenuReceivesLiveUpdatesAndRemainsAvailableAfterVictory() => MatchTestRoom.Wait(LiveAndFinished());

        private static async Task Storage()
        {
            var fixture = Load();
            var storage = Temporary("storage");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            var token = timeout.Token;
            var id = fixture.ReturnMatches["ui70-draft"];
            var boundary = new UI10HttpBoundary { MatchPath = "/api/matches/" + id };
            ControlledRecoveryStorage files = null;
            ControlledRecoveryStorage.WriteGate gate = null;
            try
            {
                var api = await fixture.Login(Side.FreePeoples, token, boundary);
                using var client = new MountedFlowClient(api, storage, (server, account, match) =>
                    files = new ControlledRecoveryStorage(new LocalMatchFiles(storage, server, account, match)));
                await Open(client, id, token);
                await client.LocalWrites;
                var board = client.Board;
                var revision = board.View.Revision;
                var die = board.View.Options.First(o => o.Kind == CommandKind.MoveArmies).EligibleDice.First();
                files.FailName = "draft";
                await Press(client.Root, die, token);
                var army = board.View.Options.First(o => o.Kind == CommandKind.MoveArmies).Armies.First(a => a.UnitIds.Count > 1);
                board.Hover.Open(army.RegionId);
                await Press(client.Root, "hover-figure-" + army.UnitIds[1], token);
                board.Hover.Close();
                await client.LocalWrites;
                await Games(client, token);
                await Until(() => client.Root.Q<Label>("board-menu-status")?.text.StartsWith("Draft") == true, token);
                Assert.AreSame(board, client.Board, "A settled task with an Error must not discard the session.");
                Assert.AreEqual(revision, board.View.Revision);
                Assert.AreEqual(0, boundary.Commands);
                files.FailName = "";
                gate = files.Pause("draft");
                await Press(client.Root, "board-menu-retry", token);
                await gate.Started;
                Assert.AreSame(board, client.Board);
                Assert.IsFalse(board.Menu.Panel.Q<Button>("back-to-games").enabledInHierarchy);
                Assert.AreEqual("Saving recovery data…", client.Root.Q<Label>("board-menu-status").text);
                await Press(client.Root, "board-menu-resume", token);
                gate.Release();
                await client.LocalWrites;
                await Task.Delay(120, token);
                Assert.AreSame(board, client.Board, "Resume cancels navigation while its local write finishes safely.");
                await Games(client, token);
                await Until(() => client.Board == null, token);
                var saved = await LocalMatchPersistence.Open(storage, api.Address.AbsoluteUri, api.AccountId, id);
                Assert.AreEqual(die, saved.Draft.Draft.Payload.DieId);
                CollectionAssert.Contains(saved.Draft.Draft.Payload.Moves.Single().UnitIds, army.UnitIds[1]);
                Assert.IsNull(saved.Actions.Pending);
                var exact = JsonConvert.SerializeObject(saved.Draft);
                await Open(client, id, token);
                await client.LocalWrites;
                Assert.AreEqual(die, client.Board.ActionDie.DieId);
                Assert.AreEqual(exact, JsonConvert.SerializeObject((await LocalMatchPersistence.Open(storage,
                    api.Address.AbsoluteUri, api.AccountId, id)).Draft));
                Assert.AreEqual(0, boundary.Commands);
                client.Flow.Dispose();
                await client.LocalWrites;
            }
            finally { gate?.Release(); Delete(storage); }
        }

        private static async Task Receipt()
        {
            var fixture = Load();
            var storage = Temporary("receipt");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            var token = timeout.Token;
            var id = fixture.ReturnMatches["ui70-restart"];
            var boundary = new UI10HttpBoundary { MatchPath = "/api/matches/" + id };
            try
            {
                var api = await fixture.Login(Side.FreePeoples, token, boundary);
                using var client = new MountedFlowClient(api, storage);
                await Open(client, id, token);
                var revision = client.Board.View.Revision;
                var hand = client.Board.View.Hand.Count;
                var die = client.Board.View.Options.First(o => o.Kind == CommandKind.DrawCard).EligibleDice.Last();
                await Press(client.Root, die, token);
                // UI-91: the hand is permanent; no Cards tab to open.
                boundary.HoldNextCommand = true;
                await Press(client.Root, "deck-character", token);
                await Until(() => boundary.CommandHeld, token);
                var command = JsonConvert.SerializeObject(boundary.LastCommand);
                await Games(client, token);
                await Until(() => client.Board == null, token);
                var saved = await LocalMatchPersistence.Open(storage, api.Address.AbsoluteUri, api.AccountId, id);
                Assert.AreEqual(command, JsonConvert.SerializeObject(saved.Actions.Pending));
                boundary.ReleaseCommand();
                await Open(client, id, token);
                await ReturnRecoveryTests.EmptyJournal(storage, api, id, false, token);
                Assert.AreEqual(1, boundary.Commands);
                Assert.AreEqual(1, boundary.ReceiptLookups);
                Assert.AreEqual(revision + 1, client.Board.View.Revision);
                Assert.AreEqual(hand + 1, client.Board.View.Hand.Count);
                Assert.AreEqual(DieLocation.Spent, client.Board.View.Dice.Single(d => d.Id == die).Location);
                Assert.AreEqual(command, JsonConvert.SerializeObject(boundary.LastCommand));
                client.Flow.Dispose();
                await client.LocalWrites;
            }
            finally { boundary.ReleaseCommand(); Delete(storage); }
        }

        private static async Task LiveAndFinished()
        {
            var fixture = Load();
            var storage = Temporary("live");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            var token = timeout.Token;
            try
            {
                var api = await fixture.Login(Side.FreePeoples, token);
                using var client = new MountedFlowClient(api, storage);
                var id = fixture.ReturnMatches["ui70-live"];
                await Open(client, id, token);
                var view = client.Board.View;
                await Press(client.Root, "open-board-menu", token);
                var receipt = await api.Post<CommandReceipt>("api/matches/" + id + "/commands", new GameCommand
                {
                    MatchId = id, CommandId = Guid.NewGuid().ToString("N"), ExpectedRevision = view.Revision,
                    DecisionId = view.Decision.Id, Kind = CommandKind.DiscardDie,
                    Payload = new CommandPayload { DieId = view.Options.First(o => o.Kind == CommandKind.DiscardDie).EligibleDice[0] }
                }, token);
                Assert.IsTrue(receipt.Accepted);
                await Until(() => client.Board.View.Revision == receipt.Revision, token);
                Assert.IsTrue(client.Board.Menu.IsOpen, "Opening Menu never pauses live updates.");
                // MENU-14: Quit game asks first; Cancel keeps the game and its menu.
                await Press(client.Root, "board-menu-quit", token);
                Assert.IsTrue(client.Board.Menu.Quit.IsOpen);
                await Press(client.Root, "quit-cancel", token);
                Assert.IsFalse(client.Board.Menu.Quit.IsOpen);
                Assert.IsTrue(client.Board.Menu.IsOpen);
                Assert.AreEqual(0, client.Quits);
                await Games(client, token);
                await Until(() => client.Board == null, token);
                // The main menu's Quit game closes the game once it is confirmed.
                await Press(client.Root, "quit-game", token);
                Assert.AreEqual(0, client.Quits, "Nothing closes before the confirmation.");
                await Press(client.Root, "quit-confirm", token);
                Assert.AreEqual(1, client.Quits);
                await Open(client, fixture.ReturnMatches["ui70-ring-destroyed"], token);
                Assert.IsTrue(client.Board.View.Winner.HasValue);
                // UI-169: a finished game opens on its end screen; View board leaves the Menu reachable.
                await Until(() => client.Board.GameEnd.IsOpen, token);
                await Press(client.Root, "game-end-view-board", token);
                Assert.IsFalse(client.Board.GameEnd.IsOpen);
                // MENU-14: a confirmed Quit game in a match saves the recovery data, closes the match session, then the game.
                await Press(client.Root, "open-board-menu", token);
                await Press(client.Root, "board-menu-quit", token);
                await Press(client.Root, "quit-confirm", token);
                await Until(() => client.Quits == 2, token);
                Assert.AreEqual(DisplayStyle.None, client.Root.Q<Button>("board-menu-retry").style.display.value,
                    "The recovery data was saved without an error before the game quit.");
                client.Flow.Dispose();
                await client.LocalWrites;
            }
            finally { Delete(storage); }
        }

        private static async Task Games(MountedFlowClient client, CancellationToken token)
        {
            if (!client.Board.Menu.IsOpen)
                await Press(client.Root, "open-board-menu", token);
            await Press(client.Root, "back-to-games", token);
        }

        private static async Task Press(VisualElement root, string name, CancellationToken token)
        {
            await Until(() => root.Q<Button>(name)?.enabledInHierarchy == true && root.Q<Button>(name).worldBound.height > 0, token);
            await Task.Delay(80, token);
            PointerGestureEvents.ClickAt(root.Q<Button>(name));
            await Task.Delay(80, token);
        }

        private static string Temporary(string name) => Path.Combine(Path.GetTempPath(), "wotr-ui70-" + name + "-" + Guid.NewGuid().ToString("N"));
        private static void Delete(string path) { if (Directory.Exists(path)) Directory.Delete(path, true); }
    }
}
