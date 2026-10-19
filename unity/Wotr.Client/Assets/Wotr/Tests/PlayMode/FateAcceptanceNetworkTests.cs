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
    // EXP-04: the Fate of Erebor release gate on two real clients. Two whole games through the normal lobby — Fate at
    // 100% and Fate with New Cities at 125% with reduced motion — make every decision on its object and never open the
    // generic decision window. The first game's replay is saved from the end screen and played in the library before
    // sign-in, with the Fate crest and the Shadow's Erebor, to its last step.
    public sealed class FateAcceptanceNetworkTests
    {
        private const float Bound = 690;

        [UnityTest, Timeout(700000)]
        public IEnumerator AFateGameAt100PercentAndItsReplay() => Wait(T23FullGameNetworkTests.Play(
            new MatchConfiguration { Fate = true }, false, "exp04-fate-full-game.json", true, SaveAndPlayReplay));

        [UnityTest, Timeout(700000)]
        public IEnumerator AFateGameWithNewCitiesAt125PercentWithReducedMotion() => Wait(T23FullGameNetworkTests.Play(
            new MatchConfiguration { Fate = true, NewCities = true }, true, "exp04-fate-new-cities-full-game.json", true));

        // The board of a Fate game, for a player, an observer or a replay: the crest on the phase plaque, the Fate
        // regions and the Shadow's Erebor.
        internal static void ShowsFate(BoardScreen board)
        {
            Assert.IsTrue(board.View.Configuration.Fate);
            Assert.IsNotNull(board.TopBar.Crests.Q("crest-" + ExpansionModules.Fate), "The Fate crest stands by the phase.");
            Assert.IsTrue(board.Map.Data.Modules.Contains(ExpansionModules.Fate), "The map draws the Fate board.");
            Assert.AreEqual("sauron", board.Map.Data.Regions["erebor"].Nation, "Erebor belongs to the Shadow.");
        }

        private static async Task SaveAndPlayReplay(MatchTestRoom room)
        {
            var storage = Path.Combine(Path.GetTempPath(), "wotr-fate-replay-" + Guid.NewGuid().ToString("N"));
            var library = new ReplayLibrary(Path.Combine(storage, "Replays"));
            await Save(room, library);
            var entry = library.List().Single();
            Assert.IsTrue(entry.Manifest.Configuration.Fate);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            var token = timeout.Token;
            using (var client = new MountedFlowClient(null, storage))
            {
                await ClickReady(client.Root, "auth-replays", token);
                await Until(() => client.Root.Q("replay-library-screen") != null, token);
                var key = ReplayLibraryPage.Key(entry);
                Assert.IsNotNull(client.Root.Q("replay-crests-" + key).Q("crest-" + ExpansionModules.Fate));
                await ClickReady(client.Root, "replay-play-" + key, token);
                await Until(() => client.Flow.Replay != null && client.Board?.View != null, token);
                ShowsFate(client.Board);
                var timeline = client.Board.Replay.Timeline;
                timeline.value = timeline.highValue;
                await Until(() => client.Board.GameEnd.IsOpen, token);
                Assert.AreEqual(entry.Manifest.Winner, client.Board.View.Winner);
                ShowsFate(client.Board);
            }
            Directory.Delete(storage, true);
        }

        private static async Task Save(MatchTestRoom room, ReplayLibrary library)
        {
            var board = room.Fp.Board;
            using (new ReplaySaver(room.Fp.Api, "api/matches/" + board.View.MatchId + "/replay", library, board.GameEnd))
            {
                Click(board.GameEnd.Root.Q<Button>("game-end-save-replay"));
                await room.Until(() => board.GameEnd.Replay is ReplaySaveState.Saved or ReplaySaveState.Failed);
                Assert.AreEqual(ReplaySaveState.Saved, board.GameEnd.Replay, board.GameEnd.Root.Q<Label>("game-end-replay-status").text);
            }
        }

        private static IEnumerator Wait(Task task)
        {
            var deadline = Time.realtimeSinceStartup + Bound;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsTrue(task.IsCompleted, "The Fate game exceeded its time bound.");
            task.GetAwaiter().GetResult();
        }
    }
}
