using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    // REPLAY-05: a saved replay on the board. It opens at the beginning, paused. A manual step, turn or timeline move
    // pauses Play; Play goes on by itself without the players' thinking time — 2, 1 or 0.25 s after a step's
    // presentation ends — and stops at the last step. A change of side shows the same step from the other side, and
    // losing the window's focus pauses. Later reads replace earlier ones; the board never shows a step out of order.
    // REPLAY-06: Play shows each step with its flights, tracks and sounds; the first view, a manual move and a change
    // of side only set the point of reference, silently, and stop whatever was still moving or sounding.
    public sealed class ReplaySession : IDisposable
    {
        public static readonly IReadOnlyDictionary<ReplaySpeed, double> Pauses = new Dictionary<ReplaySpeed, double>
        {
            [ReplaySpeed.Slow] = 2, [ReplaySpeed.Normal] = 1, [ReplaySpeed.Fast] = 0.25
        };

        // Play waits for a step's presentation in short looks, never longer than MaxPresentation (so a stuck animation,
        // muted sound or reduced motion never holds it up).
        public const double PresentationPoll = 0.05;
        public const double MaxPresentation = 3;
        private readonly ReplayArchive archive;
        private readonly BoardScreen board;
        private readonly ReplaySource source;
        private readonly ReplayHistory history;
        private readonly ReplayReports reports;
        private readonly ObserverInteraction interaction;
        private readonly ObserverConsole console;
        private readonly GameSounds sounds;
        private readonly Func<TimeSpan, CancellationToken, Task> delay;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private CancellationTokenSource playing;
        private int generation;
        private bool disposed;
        public int Position { get; private set; } = -1;
        public bool Playing => playing != null;
        public ReplaySpeed Speed { get; private set; } = ReplaySpeed.Normal;
        public Side Perspective { get; private set; } = Side.FreePeoples;
        public int Count => source.Count;
        public ReplaySource Source => source;
        public Task Shown { get; private set; } = Task.CompletedTask;

        public ReplaySession(ReplayArchive archive, BoardScreen board, Func<TimeSpan, CancellationToken, Task> delay = null,
            SoundModule soundModule = null)
        {
            this.archive = archive;
            this.board = board;
            this.delay = delay ?? ((wait, token) => Task.Delay(wait, token));
            source = new ReplaySource(archive);
            history = new ReplayHistory(archive, board.Log);
            reports = new ReplayReports(archive, board.BattleReport, board.Log);
            interaction = new ObserverInteraction(board);
            console = new ObserverConsole(board);
            sounds = soundModule == null ? null : new GameSounds(soundModule, board, NoCommands.Instance, false);
            board.Observe(Perspective);
            board.GameEnd.SetReplay(ReplaySaveState.Unavailable, "You are watching a saved replay.");
            board.SetConnection("Replay");
            board.Viewing.PerspectiveChosen += Turn;
            var bar = board.Replay;
            bar.PreviousTurn += PreviousTurn;
            bar.Previous += Previous;
            bar.PlayPause += PlayPause;
            bar.Next += Next;
            bar.NextTurn += NextTurn;
            bar.Seek += Seek;
            bar.SpeedChosen += Choose;
            Application.focusChanged += Focus;
            Shown = Show(0, true);
        }

        public void Next() => Go(Position + 1);
        public void Previous() => Go(Position - 1);
        public void NextTurn() => Go(source.NextTurn(Position) ?? Position);
        public void PreviousTurn() => Go(source.PreviousTurn(Position) ?? Position);
        public void Seek(int step) => Go(step);

        // Every manual move pauses Play first.
        private void Go(int step)
        {
            Pause();
            if (step >= 0 && step < Count && step != Position)
                Shown = Show(step, true);
        }

        private void Turn(Side side)
        {
            Perspective = side;
            board.Observe(side);
            if (Position >= 0)
                Shown = Show(Position, true);
        }

        public void Choose(ReplaySpeed speed)
        {
            Speed = speed;
            Refresh();
        }

        public void PlayPause()
        {
            if (Playing)
                Pause();
            else
                Play();
        }

        public void Pause()
        {
            if (playing == null)
                return;
            playing.Cancel();
            playing.Dispose();
            playing = null;
            Refresh();
        }

        private async void Play()
        {
            if (disposed || Position >= Count - 1)
                return;
            playing = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            var token = playing.Token;
            Refresh();
            try
            {
                while (!token.IsCancellationRequested && Position < Count - 1)
                {
                    Shown = Show(Position + 1, false);
                    await Shown;
                    if (Position >= Count - 1)
                        break;
                    await Presented(token);
                    await delay(TimeSpan.FromSeconds(Pauses[Speed]), token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
            if (!token.IsCancellationRequested)
                Pause();
        }

        private async Task Presented(CancellationToken token)
        {
            var looks = (int)Math.Round(MaxPresentation / PresentationPoll);
            for (var look = 0; board.Presenting && look < looks; look++)
                await delay(TimeSpan.FromSeconds(PresentationPoll), token);
        }

        private void Focus(bool focused)
        {
            if (!focused)
                Pause();
        }

        // Reads a step off the main thread; a later read made meanwhile wins. `quiet` sets a new point of reference.
        private async Task Show(int step, bool quiet)
        {
            var ticket = ++generation;
            ReplayFrame frame;
            try
            {
                frame = await source.Frame(step);
            }
            catch (ReplayFormatException damaged)
            {
                // A damaged part stops Play; what was shown stays.
                if (!disposed)
                {
                    playing?.Cancel();
                    board.Connection.text = damaged.Message;
                }
                return;
            }
            if (disposed || ticket != generation)
                return;
            var view = Perspective == Side.FreePeoples ? frame.FreePeoples : frame.Shadow;
            var decision = view.Decision;
            view.Decision = null;
            history.Show(Perspective, view.Revision);
            view.History = history.Recent(50);
            reports.Cut(view.Revision);
            if (quiet)
            {
                board.ApplyQuietly(view);
                sounds?.Rebase();
            }
            else
                board.Apply(view);
            sounds?.Update(view);
            console.Apply(view, decision);
            Position = step;
            Refresh();
        }

        private void Refresh()
        {
            if (disposed || Position < 0)
                return;
            var step = source.Steps[Position];
            board.Replay.Apply(Position, Count, step.Turn, step.Phase, Playing,
                source.PreviousTurn(Position).HasValue, source.NextTurn(Position).HasValue, Speed);
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            Application.focusChanged -= Focus;
            playing?.Cancel();
            playing?.Dispose();
            lifetime.Cancel();
            lifetime.Dispose();
            board.Viewing.PerspectiveChosen -= Turn;
            interaction.Dispose();
            sounds?.Dispose();
            archive.Dispose();
        }
    }
}
