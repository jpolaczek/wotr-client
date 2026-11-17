using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UIElements;
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
        private readonly OpponentPresenters presenters;
        // REPLAY-04: Save replay on the end screen; none without a library.
        private readonly ReplaySaver saver;
        private readonly RankedPauseController pause;
        private readonly GameApi api;
        private readonly string matchId;
        // UI-501: the same id for every attempt, so a concession that did arrive answers its retry with its first receipt.
        private readonly string concession = Guid.NewGuid().ToString("N");
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool ratingAsked;
        private bool disposed;
        // UI-540: while the CPU holds the decision, its state is read again now and then (a waiting CPU changes no revision).
        public const long CpuCheckMilliseconds = 15000;
        private PlayerView presented;
        private IVisualElementScheduledItem cpuWatch;
        private bool cpuChecking;
        public BoardExit Exit { get; }
        public bool Expired { get; private set; }
        // UI-501: an unfinished ranked game: leaving it loses it.
        public bool RankedActive { get; private set; }
        public bool Connected { get; private set; }
        public event Action RankedChanged;
        public Task LocalWrites => turns.LocalWrites;
        // UI-304: presenters of opponent actions register here (UI-305–UI-312).
        public PresentationPlayback Playback => playback.Playback;

        public MatchSession(GameApi api, string matchId, BoardScreen board, LocalMatchPersistence persistence, SoundModule sounds = null,
            ReplayLibrary replays = null)
        {
            this.board = board;
            this.api = api;
            this.matchId = matchId;
            Exit = new BoardExit(persistence);
            connection = new MatchConnection(api, matchId);
            turns = new TurnInteraction(api, connection, board, persistence);
            seen = new MatchSeenTracker(api, matchId);
            history = new ActionLogController(api, matchId, board.Log);
            reports = new BattleReportController(api, matchId, board.BattleReport, persistence?.Notifications);
            notices = new ActionNoticeController(api, matchId, board, persistence?.Notifications);
            chat = RoomChatController.ForGame(api, board.GameChat, matchId);
            playback = new PlaybackBinding(board, notices, Present, turns.Hold);
            presenters = new OpponentPresenters(board, playback);
            connection.Updated += playback.Receive;
            connection.StatusChanged += ConnectionChanged;
            this.sounds = sounds == null ? null : new GameSounds(sounds, board, turns);
            presenters.Sounds.Target = this.sounds;
            board.TopBar.Clock.Warning += WarnClock;
            pause = new RankedPauseController(api, matchId, board, connection);
            connection.ClockSeen += ShowClock;
            connection.OpponentSeen += board.TopBar.Clock.ShowOpponent;
            cpuWatch = board.Root.schedule.Execute(CheckCpu).Every(CpuCheckMilliseconds);
            if (replays != null)
                saver = new ReplaySaver(api, "api/matches/" + Uri.EscapeDataString(matchId) + "/replay", replays, board.GameEnd);
            connection.Start();
        }

        // UI-304: a view the playback released; the history feed already took it when it arrived.
        private void Present(PlayerView view)
        {
            if (disposed)
                return;
            presented = view;
            board.Apply(view);
            if (view.Winner.HasValue && !ratingAsked)
                _ = LoadRating();
            turns.Update(view);
            sounds?.Update(view);
            history.Update(view);
            board.Root.schedule.Execute(() =>
            {
                if (!disposed && board.Root.panel != null)
                    seen.Displayed(view.Revision);
            });
        }

        private static bool SameCpu(CpuSeatState a, CpuSeatState b) =>
            a == null ? b == null : b != null && a.Waiting == b.Waiting && a.Since == b.Since && a.Problem == b.Problem;

        // UI-540: the CPU's decision: read its state again (it may wait with an error, or have come back).
        private void CheckCpu()
        {
            if (disposed || cpuChecking || !board.CpuOpponent || presented == null || presented.Winner.HasValue ||
                !presented.DecisionOwner.HasValue || presented.DecisionOwner.Value == presented.You)
                return;
            _ = RefreshCpu();
        }

        private async Task RefreshCpu()
        {
            cpuChecking = true;
            try
            {
                await connection.RefreshClock();
            }
            catch (OperationCanceledException) { }
            catch (Exception) when (!disposed)
            {
                // The update loop's next notice or reconnect reads the view again.
            }
            finally
            {
                cpuChecking = false;
            }
        }

        // UI-502: how the game moved the player's rating. A normal game has none (404), and a failed read is tried again with
        // the next view.
        private async Task LoadRating()
        {
            ratingAsked = true;
            try
            {
                var result = await api.Get<RankedResultView>("api/matches/" + Uri.EscapeDataString(matchId) + "/rating", lifetime.Token);
                if (!disposed)
                    board.GameEnd.ShowRating(result);
            }
            catch (ApiException error) when (error.Status == 404) { }
            catch (OperationCanceledException) when (disposed) { }
            catch (Exception error) when (error is ApiException or System.Net.Http.HttpRequestException or OperationCanceledException)
            {
                ratingAsked = false;
            }
        }

        private void ConnectionChanged(string state)
        {
            if (disposed)
                return;
            Expired = state == "SessionExpired";
            Connected = state == "Connected";
            board.TopBar.Clock.ShowConnected(Connected);
            board.Pause.ShowConnected(Connected);
            board.SetConnection(state);
            sounds?.SetConnection(state);
            turns.SetConnection(state);
            history.SetConnection(state);
            notices.SetConnection(state);
            playback.Playback.SetConnection(state);
        }

        public void Reconnect() => connection.Reconnect();

        private void WarnClock(string decisionId) => sounds?.ClockWarning(decisionId);

        private void ShowClock(PlayerView view)
        {
            if (disposed)
                return;
            board.ShowClock(view.Clock, view.You);
            turns.SetPaused(view.Clock?.Paused == true);
            // UI-540: the CPU's waiting state rides on every view read, whatever its revision.
            if (!SameCpu(board.CpuState, view.Cpu))
            {
                board.CpuState = view.Cpu;
                turns.CpuChanged();
            }
            ShowRanked(view.Clock != null);
        }

        private void ShowRanked(bool active)
        {
            board.Menu.SetRanked(active);
            if (active == RankedActive)
                return;
            RankedActive = active;
            RankedChanged?.Invoke();
        }

        // UI-501: sends the concession and waits for the answer; the view that follows shows the end of the game.
        public async Task Concede()
        {
            var route = "api/matches/" + Uri.EscapeDataString(matchId) + "/concede";
            await api.Post<CommandReceipt>(route, new ConcedeRequest { CommandId = concession }, CancellationToken.None);
            await connection.Refresh();
        }

        public void Dispose() => Dispose(true);

        public void Dispose(bool saveDraft)
        {
            if (disposed)
                return;
            disposed = true;
            lifetime.Cancel();
            lifetime.Dispose();
            connection.Updated -= playback.Receive;
            presenters.Dispose();
            playback.Dispose();
            connection.StatusChanged -= ConnectionChanged;
            board.TopBar.Clock.Warning -= WarnClock;
            connection.ClockSeen -= ShowClock;
            cpuWatch?.Pause();
            connection.OpponentSeen -= board.TopBar.Clock.ShowOpponent;
            pause.Dispose();
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
