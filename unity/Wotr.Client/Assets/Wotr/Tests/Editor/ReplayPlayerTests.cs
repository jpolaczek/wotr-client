using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // REPLAY-05 (RP08, RP09, RP13): a replay opens at its start, paused; steps and whole turns go both ways with the
    // ways past the ends greyed out; a change of side shows the same step from the other side; the Log holds nothing
    // recorded later; Play runs to the end at the chosen pace and any manual move pauses it; a later read wins over an
    // earlier one; at most three chunks of frames are kept. REPLAY-04: Save replay goes through the library.
    public class ReplayPlayerTests
    {
        private string folder;

        [SetUp]
        public void CreateFolder() => folder = Path.Combine(Path.GetTempPath(), "wotr-replay-player-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void RemoveFolder()
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        // Turn 1: steps 0–19, turn 2: 20–39, turn 3: 40–59.
        private (ReplaySession Session, BoardScreen Board, List<TimeSpan> Waits) Open(Func<TimeSpan, CancellationToken, Task> delay = null)
        {
            var waits = new List<TimeSpan>();
            var board = new BoardScreen();
            var archive = ReplayArchive.Open(ReplayFixture.Build(folder, 60, step => 1 + step / 20));
            var session = new ReplaySession(archive, board, delay ?? ((wait, token) =>
            {
                waits.Add(wait);
                return Task.CompletedTask;
            }));
            return (session, board, waits);
        }

        [Test]
        public async Task StepsAndTurnsGoBothWaysAndTheEndsAreGreyedOut()
        {
            var (session, board, _) = Open();
            await session.Shown;
            Assert.AreEqual(0, session.Position);
            Assert.IsFalse(session.Playing);
            Assert.AreEqual(100, board.View.Revision);
            Assert.IsFalse(board.Replay.Root.Q<Button>("replay-previous").enabledSelf);
            Assert.IsFalse(board.Replay.Root.Q<Button>("replay-previous-turn").enabledSelf);
            Assert.AreEqual("Step 1/60 · Turn 1 · Actions", board.Replay.Root.Q<Label>("replay-position").text);
            session.Next();
            await session.Shown;
            Assert.AreEqual(101, board.View.Revision);
            session.Seek(25);
            await session.Shown;
            session.NextTurn();
            await session.Shown;
            Assert.AreEqual(40, session.Position, "The start of the next turn.");
            Assert.IsFalse(board.Replay.Root.Q<Button>("replay-next-turn").enabledSelf);
            session.PreviousTurn();
            await session.Shown;
            Assert.AreEqual(20, session.Position, "The start of the turn before.");
            session.PreviousTurn();
            await session.Shown;
            Assert.AreEqual(0, session.Position);
            session.Seek(59);
            await session.Shown;
            Assert.IsFalse(board.Replay.Root.Q<Button>("replay-next").enabledSelf);
            Assert.IsFalse(board.Replay.Root.Q<Button>("replay-play").enabledSelf, "Play rests at the last step.");
            session.Dispose();
        }

        [Test]
        public async Task TheOtherSideShowsTheSameStepAndTheLogHoldsNothingLater()
        {
            var (session, board, _) = Open();
            await session.Shown;
            session.Seek(30);
            await session.Shown;
            board.Viewing.Choose(Side.Shadow);
            await session.Shown;
            Assert.AreEqual(30, session.Position);
            Assert.AreEqual(Side.Shadow, board.View.You);
            Assert.AreEqual(130, board.View.Revision);
            Assert.AreEqual(31, board.Log.Model.StepCount, "Steps 0–30 of the Shadow's history, nothing later.");
            Assert.IsNotNull(board.Log.Model.Find("Shadow-a30"));
            Assert.IsNull(board.Log.Model.Find("Shadow-a31"), "Nothing recorded after the step shown.");
            Assert.LessOrEqual(board.View.History.Max(h => h.Revision), 130);
            session.Dispose();
        }

        [Test]
        public async Task PlayRunsToTheEndAtThePaceAndAManualMovePausesIt()
        {
            var (session, board, waits) = Open();
            await session.Shown;
            session.Choose(ReplaySpeed.Fast);
            session.PlayPause();
            await Until(() => session.Position == 59 && !session.Playing);
            Assert.AreEqual(58, waits.Count, "One pause between every two steps; nothing moves in these steps.");
            Assert.IsTrue(waits.All(w => Math.Abs(w.TotalSeconds - 0.25) < 0.001));
            session.Seek(10);
            await session.Shown;
            var gate = new TaskCompletionSource<bool>();
            var paced = Open((wait, token) => gate.Task);
            await paced.Session.Shown;
            paced.Session.PlayPause();
            await Until(() => paced.Session.Position == 1);
            Assert.IsTrue(paced.Session.Playing);
            paced.Session.Previous();
            Assert.IsFalse(paced.Session.Playing, "A manual step pauses Play.");
            gate.SetResult(true);
            await paced.Session.Shown;
            Assert.AreEqual(0, paced.Session.Position);
            session.Dispose();
            paced.Session.Dispose();
        }

        [Test]
        public async Task ALaterReadWinsAndOnlyThreeChunksAreKept()
        {
            var (session, board, _) = Open();
            await session.Shown;
            session.Seek(50);
            var late = session.Shown;
            session.Seek(5);
            await Task.WhenAll(late, session.Shown);
            Assert.AreEqual(5, session.Position);
            Assert.AreEqual(105, board.View.Revision);
            foreach (var step in new[] { 30, 55, 10, 45 })
            {
                session.Seek(step);
                await session.Shown;
            }
            Assert.LessOrEqual(session.Source.Kept, ReplaySource.ChunksKept);
            session.Dispose();
        }

        [Test]
        public async Task SaveReplayGoesThroughTheLibraryAndSaysWhatHappened()
        {
            using var server = new ReplayServer();
            using var api = new GameApi("https://example.invalid", server);
            var library = new ReplayLibrary(Path.Combine(folder, "Replays"));
            var screen = new GameEndScreen();
            using var saver = new ReplaySaver(api, "api/observe/m/replay", library, screen);
            Assert.AreEqual(ReplaySaveState.Ready, screen.Replay);
            server.Fail = true;
            screen.RequestSave();
            await Until(() => screen.Replay == ReplaySaveState.Failed);
            Assert.AreEqual("Retry saving", screen.Root.Q<Button>("game-end-save-replay").text);
            server.Fail = false;
            Save(screen);
            await Until(() => screen.Replay == ReplaySaveState.Saved);
            Assert.AreEqual(1, library.List().Count);
            Assert.AreEqual("/api/observe/m/replay", server.Asked.Last());
            var second = new GameEndScreen();
            using var again = new ReplaySaver(api, "api/observe/m/replay", library, second);
            Save(second);
            await Until(() => second.Replay == ReplaySaveState.AlreadySaved);
            Assert.AreEqual(1, library.List().Count);
            server.Unrecorded = true;
            var old = new GameEndScreen();
            using var unrecorded = new ReplaySaver(api, "api/observe/m/replay", library, old);
            Save(old);
            await Until(() => old.Replay == ReplaySaveState.Unavailable);
            Assert.IsFalse(old.Root.Q<Button>("game-end-save-replay").enabledSelf);
        }

        private static void Save(GameEndScreen screen) => screen.RequestSave();

        private static async Task Until(Func<bool> condition)
        {
            for (var i = 0; i < 300 && !condition(); i++)
                await Task.Delay(10);
            Assert.IsTrue(condition());
        }

        // Gives the sample replay, or a busy server, or a match without recording.
        private sealed class ReplayServer : HttpMessageHandler
        {
            public bool Fail;
            public bool Unrecorded;
            public readonly List<string> Asked = new List<string>();

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                Asked.Add(request.RequestUri.AbsolutePath);
                if (Fail)
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("{\"error\":\"ServerBusy\"}") });
                if (Unrecorded)
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict) { Content = new StringContent("{\"error\":\"ReplayUnavailable\"}") });
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(File.ReadAllBytes(ReplayFixture.Sample)) });
            }
        }
    }
}
