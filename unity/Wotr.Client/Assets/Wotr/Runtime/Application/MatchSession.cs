using System;
using System.Threading.Tasks;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class MatchSession : IDisposable
    {
        private readonly BoardScreen board;
        private readonly MatchConnection connection;
        private readonly TurnInteraction turns;
        private readonly MatchSeenTracker seen;
        private readonly ActionLogController history;
        private readonly RoomChatController chat;
        private readonly BattleReportController reports;
        private readonly ActionNoticeController notices;
        private readonly GameSounds sounds;
        private readonly PlaybackBinding playback;
        // REPLAY-04: Save replay on the end screen; none without a library.
        private readonly ReplaySaver saver;
        private bool disposed;
        public BoardExit Exit { get; }
        public bool Expired { get; private set; }
        public Task LocalWrites => turns.LocalWrites;
        // UI-304: presenters of opponent actions register here (UI-305–UI-312).
        public PresentationPlayback Playback => playback.Playback;

        public MatchSession(GameApi api, string matchId, BoardScreen board, LocalMatchPersistence persistence, SoundModule sounds = null,
            ReplayLibrary replays = null)
        {
            this.board = board;

            Exit = new BoardExit(persistence);
            connection = new MatchConnection(api, matchId);
            turns = new TurnInteraction(api, connection, board, persistence);
            seen = new MatchSeenTracker(api, matchId);
            history = new ActionLogController(api, matchId, board.Log);
            reports = new BattleReportController(api, matchId, board.BattleReport, persistence?.Notifications);
            notices = new ActionNoticeController(api, matchId, board, persistence?.Notifications);
            chat = RoomChatController.ForGame(api, board.GameChat, matchId);
            playback = new PlaybackBinding(board, notices, Present, turns.Hold);
            connection.Updated += playback.Receive;
            connection.StatusChanged += ConnectionChanged;
            this.sounds = sounds == null ? null : new GameSounds(sounds, board, turns);
            if (replays != null)
                saver = new ReplaySaver(api, "api/matches/" + Uri.EscapeDataString(matchId) + "/replay", replays, board.GameEnd);
            connection.Start();
        }

        // UI-304: a view the playback released; the history feed already took it when it arrived.
        private void Present(PlayerView view)
        {
            if (disposed)
                return;
            board.Apply(view);
            turns.Update(view);
            sounds?.Update(view);
            history.Update(view);
            board.Root.schedule.Execute(() =>
            {
                if (!disposed && board.Root.panel != null)
                    seen.Displayed(view.Revision);
            });
        }

        private void ConnectionChanged(string state)
        {
            if (disposed)
                return;
            Expired = state == "SessionExpired";
            board.SetConnection(state);
            sounds?.SetConnection(state);
            turns.SetConnection(state);
            history.SetConnection(state);
            notices.SetConnection(state);
            playback.Playback.SetConnection(state);
        }

        public void Reconnect() => connection.Reconnect();

        public void Dispose() => Dispose(true);

        public void Dispose(bool saveDraft)
        {
            if (disposed)
                return;
            disposed = true;
            connection.Updated -= playback.Receive;
            playback.Dispose();
            connection.StatusChanged -= ConnectionChanged;
            sounds?.Dispose();
            saver?.Dispose();
            turns.Dispose(saveDraft);
            history.Dispose();
            reports.Dispose();
            notices.Dispose();
            chat.Dispose();
            seen.Dispose();
            connection.Dispose();
        }
    }
}
