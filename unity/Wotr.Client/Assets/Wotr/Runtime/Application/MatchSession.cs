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
        // REPLAY-04: Save replay on the end screen; none without a library.
        private readonly ReplaySaver saver;
        private bool disposed;
        public BoardExit Exit { get; }
        public bool Expired { get; private set; }
        public Task LocalWrites => turns.LocalWrites;

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
            connection.Updated += Update;
            connection.StatusChanged += ConnectionChanged;
            this.sounds = sounds == null ? null : new GameSounds(sounds, board, turns);
            if (replays != null)
                saver = new ReplaySaver(api, "api/matches/" + Uri.EscapeDataString(matchId) + "/replay", replays, board.GameEnd);
            connection.Start();
        }

        private void Update(PlayerView view)
        {
            if (disposed)
                return;
            board.Apply(view);
            turns.Update(view);
            sounds?.Update(view);
            history.Update(view);
            notices.Update(view);
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
        }

        public void Reconnect() => connection.Reconnect();

        public void Dispose() => Dispose(true);

        public void Dispose(bool saveDraft)
        {
            if (disposed)
                return;
            disposed = true;
            connection.Updated -= Update;
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
