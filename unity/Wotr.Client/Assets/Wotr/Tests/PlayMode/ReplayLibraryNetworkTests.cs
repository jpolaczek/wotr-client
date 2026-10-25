using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.ReturnTestFixture;

namespace Wotr.PlayTests
{
    // REPLAY-04/05 (RP06, RP07, RP08): both players of a finished test game save its replay from the end screen and
    // the library keeps it once. Then, before sign-in and without asking the server anything, the library plays it,
    // steps to the end, returns, exports, deletes and imports it again.
    public sealed class ReplayLibraryNetworkTests
    {
        [UnityTest]
        public IEnumerator PlayersSaveAReplayThatPlaysBeforeSignIn()
        {
            var task = Run();
            while (!task.IsCompleted)
                yield return null;
            task.GetAwaiter().GetResult();
        }

        private static async Task Run()
        {
            var storage = Path.Combine(Path.GetTempPath(), "wotr-replay-flow-" + Guid.NewGuid().ToString("N"));
            var library = new ReplayLibrary(Path.Combine(storage, "Replays"));
            await SaveFromBothSides(library);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            var token = timeout.Token;
            var exported = Path.Combine(storage, "exported.wotrreplay");
            var picker = new FixedPicker { Import = exported, Export = exported };
            using (var client = new MountedFlowClient(null, storage))
            {
                client.Flow.FilePicker = picker;
                await ClickReady(client.Root, "auth-replays", token);
                await Until(() => client.Root.Q("replay-library-screen") != null, token);
                var key = ReplayLibraryPage.Key(library.List().Single());
                await ClickReady(client.Root, "replay-play-" + key, token);
                await Until(() => client.Flow.Replay != null && client.Board?.View != null, token);
                Assert.IsTrue(client.Board.Replay.Visible);
                await Until(() => client.Board.Replay.Bar.worldBound.width > 0, token);
                var bar = client.Board.Replay.Bar.worldBound;
                Assert.GreaterOrEqual(bar.xMin, client.Board.Root.worldBound.xMin - 1, "The replay bar stays on the board.");
                Assert.LessOrEqual(bar.xMax, client.Board.Root.worldBound.xMax + 1);
                Assert.IsFalse(bar.Overlaps(client.Board.Viewing.Bar.worldBound), "The replay bar sits under the viewing bar.");
                Assert.AreEqual(0, client.Flow.Replay.Position);
                Assert.AreEqual("Replay", client.Board.Connection.text);
                // UI-328: Play presents the last step's actions (the Shadow's die, the capture) as a live game does, then the
                // board shows the end.
                await ClickReady(client.Root, "replay-play", token);
                await Until(() => client.Flow.Replay.PresentingCue, token);
                Assert.IsFalse(client.Board.GameEnd.IsOpen, "The step's view waits for its presentation.");
                LayoutTests.Save(client.Panel.targetTexture, "ui328-replay-play.png");
                await Until(() => client.Board.GameEnd.IsOpen, token);
                Assert.AreEqual("SHADOW WINS", client.Board.GameEnd.State.Title);
                Assert.IsFalse(client.Root.Q<Button>("game-end-save-replay").enabledSelf);
                await ClickReady(client.Root, "game-end-view-board", token);
                await ClickReady(client.Root, "open-board-menu", token);
                await ClickReady(client.Root, "back-to-games", token);
                await Until(() => client.Root.Q("replay-library-screen") != null, token);

                await ClickReady(client.Root, "replay-export-" + key, token);
                await Until(() => File.Exists(exported), token);
                await ClickReady(client.Root, "replay-delete-" + key, token);
                await Until(() => client.Root.Q<Button>("replay-play-" + key) == null, token);
                await ClickReady(client.Root, "replay-import", token);
                await Until(() => client.Root.Q<Button>("replay-play-" + key) != null, token);
                Assert.AreEqual("Replay imported.", client.Root.Q<Label>("replay-status").text);
            }
            Directory.Delete(storage, true);
        }

        // The Shadow wins "sh-military" with its last die; each player's end screen saves the same replay.
        private static async Task SaveFromBothSides(ReplayLibrary library)
        {
            var session = await TestScenarioRoom.Start("sh-military");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                using var fpSaver = new ReplaySaver(fp.Api, "api/matches/" + fp.Board.View.MatchId + "/replay", library, fp.Board.GameEnd);
                using var shSaver = new ReplaySaver(sh.Api, "api/matches/" + sh.Board.View.MatchId + "/replay", library, sh.Board.GameEnd);
                var die = sh.Board.View.Dice.First(d => d.Owner == Side.Shadow && d.Location == DieLocation.Available);
                await session.Send(Side.Shadow, CommandKind.DiscardDie, new CommandPayload { DieId = die.Id });
                await session.Until(() => fp.Board.GameEnd.IsOpen && sh.Board.GameEnd.IsOpen);
                Assert.AreEqual(ReplaySaveState.Ready, fp.Board.GameEnd.Replay);
                Click(fp.Board.GameEnd.Root.Q<Button>("game-end-save-replay"));
                await session.Until(() => fp.Board.GameEnd.Replay == ReplaySaveState.Saved || fp.Board.GameEnd.Replay == ReplaySaveState.Failed);
                Assert.AreEqual(ReplaySaveState.Saved, fp.Board.GameEnd.Replay, fp.Board.GameEnd.Root.Q<Label>("game-end-replay-status").text);
                Click(sh.Board.GameEnd.Root.Q<Button>("game-end-save-replay"));
                await session.Until(() => sh.Board.GameEnd.Replay == ReplaySaveState.AlreadySaved);
                var entry = library.List().Single();
                Assert.AreEqual(Side.Shadow, entry.Manifest.Winner);
                Assert.AreEqual(2, entry.Manifest.Steps);
            }
            finally
            {
                await session.End();
            }
        }

        private sealed class FixedPicker : IReplayFilePicker
        {
            public string Import;
            public string Export;
            public Task<string> ChooseImport() => Task.FromResult(Import);
            public Task<string> ChooseExport(string suggestedName) => Task.FromResult(Export);
        }
    }
}
