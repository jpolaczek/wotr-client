using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // REPLAY-07 (RP12): the replay of a whole game, downloaded from the real server by wotr-server's
    // ReplayEndToEndTests (88 moves in six turns, the Shadow wins by corruption; regenerate with WOTR_REPLAY_FULL), is
    // saved, exported, imported into a second installation and played there without an account or a server: every
    // step both ways and from both sides, the turns, the end and a Log that never shows what came later.
    public class ReplayFullGameTests
    {
        private static readonly string Fixture = Path.Combine(Application.dataPath, "Wotr/Tests/Editor/ReplayFullGame.wotrreplay");
        private string folder;

        [SetUp]
        public void CreateFolder() => folder = Path.Combine(Path.GetTempPath(), "wotr-replay-full-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void RemoveFolder()
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        [Test]
        public async Task AWholeGameMovesToASecondInstallationAndPlaysThereOffline()
        {
            var first = new ReplayLibrary(Path.Combine(folder, "First installation"));
            Assert.AreEqual(ReplayAdded.Saved, await first.AddFile(Fixture));
            var copy = Path.Combine(folder, "carried.wotrreplay");
            await first.Export(first.List().Single(), copy);
            var second = new ReplayLibrary(Path.Combine(folder, "Second installation"));
            Assert.AreEqual(ReplayAdded.Saved, await second.AddFile(copy));
            var entry = second.List().Single();
            Assert.AreEqual((Side?)Side.Shadow, entry.Manifest.Winner);
            Assert.AreEqual((VictoryCondition?)VictoryCondition.RingCorrupted, entry.Manifest.Victory);
            Assert.AreEqual(6, entry.Manifest.Turns);

            var board = new BoardScreen();
            using var session = new ReplaySession(ReplayArchive.Open(entry.Path), board, (wait, token) => Task.CompletedTask);
            await session.Shown;
            Assert.AreEqual(0, session.Position);
            var revision = board.View.Revision;
            for (var step = 1; step < session.Count; step++)
            {
                session.Next();
                await session.Shown;
                Assert.Greater(board.View.Revision, revision, "Step " + step);
                Assert.AreEqual(Side.FreePeoples, board.View.You);
                revision = board.View.Revision;
            }
            Assert.AreEqual(Side.Shadow, board.View.Winner);
            Assert.AreEqual("SHADOW WINS", board.GameEnd.State.Title);
            board.Viewing.Choose(Side.Shadow);
            await session.Shown;
            for (var step = session.Count - 2; step >= 0; step--)
            {
                session.Previous();
                await session.Shown;
                Assert.Less(board.View.Revision, revision, "Back to step " + step);
                Assert.AreEqual(Side.Shadow, board.View.You);
                revision = board.View.Revision;
            }
            Assert.LessOrEqual(session.Source.Kept, ReplaySource.ChunksKept);

            var starts = session.Source.Turns.Select(t => t.Step).ToList();
            foreach (var start in starts.Skip(1))
            {
                session.NextTurn();
                await session.Shown;
                Assert.AreEqual(start, session.Position);
            }
            foreach (var start in starts.Take(starts.Count - 1).Reverse())
            {
                session.PreviousTurn();
                await session.Shown;
                Assert.AreEqual(start, session.Position);
            }

            var middle = session.Count / 2;
            session.Seek(middle);
            await session.Shown;
            var cutoff = board.View.Revision;
            using var archive = ReplayArchive.Open(entry.Path);
            var later = Enumerable.Range(0, archive.HistoryChunks(Side.Shadow))
                .SelectMany(chunk => archive.History(Side.Shadow, chunk).Steps)
                .GroupBy(s => s.ActionId).Where(action => action.Min(s => s.Revision) > cutoff).Select(action => action.Key).ToList();
            Assert.IsNotEmpty(later, "The game goes on after the middle step.");
            foreach (var action in later)
                Assert.IsNull(board.Log.Model.Find(action), "The Log shows nothing recorded after the step.");
            Assert.LessOrEqual(board.View.History.Max(h => h.Revision), cutoff);
        }
    }
}
