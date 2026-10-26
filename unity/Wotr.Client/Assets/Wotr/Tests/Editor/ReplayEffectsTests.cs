using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // REPLAY-06 (RP10, RP11): Play shows a step with its effects — the figure that moved flies, the Fellowship's tracks
    // change in two stages with their sounds, and Play waits for that presentation, never longer than its limit. The
    // first view, a manual step back or forth and a change of side only set the point of reference: nothing flies,
    // no stage, no sound, and an earlier sequence still due is dropped. A viewer never hears a personal result.
    public class ReplayEffectsTests
    {
        private string folder;
        private SoundModule module;
        private List<SoundCue> played;

        [SetUp]
        public void Create()
        {
            folder = Path.Combine(Path.GetTempPath(), "wotr-replay-effects-" + Guid.NewGuid().ToString("N"));
            module = new SoundModule();
            played = new List<SoundCue>();
            module.Policy.Played += played.Add;
        }

        [TearDown]
        public void Release()
        {
            module.Dispose();
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        // Step 1 moves the first unit to another figure's region and raises both Progress and Corruption; step 2 ends
        // the game with the Shadow's win.
        // The pause of Play waits for `pace`; looks at the presentation pass at once.
        private (ReplaySession Session, BoardScreen Board, List<TimeSpan> Waits) Open(Task pace = null)
        {
            var waits = new List<TimeSpan>();
            var board = new BoardScreen();
            var path = ReplayFixture.Build(folder, 3, _ => 1, (step, view) =>
            {
                if (step >= 1)
                {
                    view.Units[0].Location = view.Units.First(u => u.Location != view.Units[0].Location).Location;
                    view.Fellowship.Progress += 1;
                    view.Fellowship.Corruption += 1;
                }
                if (step == 2)
                {
                    view.Winner = Side.Shadow;
                    view.Victory = VictoryCondition.ShadowMilitary;
                }
            });
            var session = new ReplaySession(ReplayArchive.Open(path), board, (wait, token) =>
            {
                waits.Add(wait);
                return pace != null && Math.Abs(wait.TotalSeconds - ReplaySession.PresentationPoll) > 0.001 ? pace : Task.CompletedTask;
            }, module);
            return (session, board, waits);
        }

        [Test]
        public async Task PlayShowsTheEffectsAndAManualMoveOnlySetsThePointOfReference()
        {
            var pace = new TaskCompletionSource<bool>();
            var (session, board, waits) = Open(pace.Task);
            await session.Shown;
            Assert.IsFalse(board.Presenting, "The first view is a point of reference.");
            Assert.IsEmpty(played);
            session.Next();
            await session.Shown;
            Assert.IsFalse(board.Presenting, "A manual step moves nothing.");
            Assert.IsEmpty(played, "A manual step is silent.");
            session.Previous();
            await session.Shown;
            Assert.IsFalse(board.Presenting);
            Assert.IsEmpty(played);

            session.PlayPause();
            await Until(() => session.Position == 1);
            Assert.IsTrue(board.Map.Figures.ActiveCount > 0, "Play flies the moved figure.");
            Assert.IsTrue(board.TracksStaged, "Play shows Progress and Corruption in two stages.");
            Assert.IsNotEmpty(played, "Play sounds the step.");
            await Until(() => waits.Any(w => Math.Abs(w.TotalSeconds - 1) < 0.001));
            pace.SetResult(true);
            await Until(() => session.Position == 2 && !session.Playing);
            var polls = waits.Count(w => Math.Abs(w.TotalSeconds - ReplaySession.PresentationPoll) < 0.001);
            Assert.Greater(polls, 0, "Play waited for the step's presentation.");
            // UI-328: each step Play shows is presented to its end, the last one too — never longer than the limit per step.
            const int shownSteps = 2;
            Assert.LessOrEqual(polls * ReplaySession.PresentationPoll, shownSteps * ReplaySession.MaxPresentation + 0.001,
                "never longer than its limit");
            CollectionAssert.DoesNotContain(played, SoundCue.Victory);
            CollectionAssert.DoesNotContain(played, SoundCue.Defeat);

            module.Policy.Silence();
            played.Clear();
            board.Viewing.Choose(Side.Shadow);
            await session.Shown;
            Assert.IsFalse(board.Presenting, "A change of side stops what moved and stages nothing.");
            Assert.IsEmpty(played, "A change of side is silent.");
            session.Dispose();
        }

        [Test]
        public async Task AJumpDuringPlayDropsTheStepsPresentation()
        {
            var pace = new TaskCompletionSource<bool>();
            var (session, board, _) = Open(pace.Task);
            await session.Shown;
            session.PlayPause();
            await Until(() => session.Position == 1);
            session.Seek(0);
            await session.Shown;
            Assert.IsFalse(session.Playing);
            Assert.AreEqual(0, board.Map.Figures.ActiveCount, "The flight stops with the jump.");
            Assert.IsFalse(board.TracksStaged, "The second stage is cancelled.");
            session.Dispose();
        }

        private static async Task Until(Func<bool> condition)
        {
            for (var i = 0; i < 300 && !condition(); i++)
                await Task.Delay(10);
            Assert.IsTrue(condition());
        }
    }
}
