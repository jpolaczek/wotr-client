using System;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-304: the playback coordinator of one mounted match. Every received view goes to the history feed first (its
    // cues and the read mark of its revision) and then to the coordinator, which applies it through `present` when its
    // presentation allows. The coordinator ticks with the board's scheduler and rebases when the window loses focus.
    public sealed class PlaybackBinding : IDisposable
    {
        private const long TickMilliseconds = 16;
        private readonly ActionNoticeController notices;
        private readonly IVisualElementScheduledItem ticks;
        private bool disposed;
        public PresentationPlayback Playback { get; }
        // After every tick of the coordinator (UI-305: the Skip target follows Presenting).
        public event Action Ticked;

        public PlaybackBinding(BoardScreen board, ActionNoticeController notices, Action<PlayerView> present, Action<bool> hold)
        {
            this.notices = notices;
            Playback = new PresentationPlayback(present, hold, () => Time.realtimeSinceStartupAsDouble);
            notices.CuesReady += Playback.Enqueue;
            notices.HistoryRead += Playback.HistoryRead;
            Application.focusChanged += Playback.SetFocus;
            ticks = board.Root.schedule.Execute(Tick).Every(TickMilliseconds);
        }

        private void Tick()
        {
            Playback.Tick();
            Ticked?.Invoke();
        }

        public void Receive(PlayerView view)
        {
            if (disposed)
                return;
            notices.Update(view);
            Playback.Receive(view);
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            ticks.Pause();
            Application.focusChanged -= Playback.SetFocus;
            notices.CuesReady -= Playback.Enqueue;
            notices.HistoryRead -= Playback.HistoryRead;
            Playback.Dispose();
        }
    }
}
