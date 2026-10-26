using System;
using System.Threading;
using System.Threading.Tasks;
using Wotr.Contracts;

namespace Wotr.Client
{
    // OBS-05: watching one match live. The observer's routes read the chosen side (Free Peoples first) with the usual
    // update socket and reconnect, and the room's presence is renewed every 15 s. There is no command sender, draft,
    // automatic decision or read marker, so watching cannot change the game. A change of side keeps the camera and
    // the open tabs; the history and the view of the other side replace those of the previous one.
    // UI-330 (owner 2026-10-25: "players who watch a game also want to know who plays what; they should see the animations
    // for both sides, even though they watch one — only for that one do they have the hidden information"): the board
    // presents every side's actions with the player's presenters, from the watched side's history; each new point of
    // reference (the first view, a reconnect, a change of side) starts the presentation over, so it is shown quietly and
    // nothing from before it is played back.
    public sealed class ObserverSession : IDisposable
    {
        public static readonly TimeSpan PresenceInterval = TimeSpan.FromSeconds(15);
        private readonly GameApi api;
        private readonly string room;
        private readonly BoardScreen board;
        private readonly MatchConnection connection;
        private readonly ActionLogController history;
        private readonly RoomChatController chat;
        private readonly BattleReportController reports;
        private readonly ObserverInteraction interaction;
        private readonly ObserverConsole console;
        private readonly ReplaySaver saver;
        // REPLAY-06: the facts sound as on a player's board, without decision alerts or a personal result; the first
        // view, a reconnect and a change of side are silent.
        private readonly GameSounds sounds;
        private bool quietNext = true;
        private readonly MatchReadRoutes routes;
        private ActionNoticeController notices;
        private PlaybackBinding playback;
        private OpponentPresenters presenters;
        private string status = "";
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool disposed;
        public Side Perspective { get; private set; } = Side.FreePeoples;
        public bool Expired { get; private set; }
        public string Room => room;
        // How many views the board drew; tests read it to see a change of side arrive.
        public int Applied { get; private set; }
        // UI-330: whether the presentation's history has loaded, that it presents every side, and its playback.
        public bool PresentationLoaded => notices?.Loaded == true;
        public bool PresentsEverySide => notices?.Presentation?.Everyone == true;
        public PresentationPlayback Playback => playback?.Playback;

        public ObserverSession(GameApi api, RoomView room, BoardScreen board, TimeSpan? presence = null, ReplayLibrary replays = null,
            SoundModule soundModule = null)
        {
            this.api = api;
            this.room = room.Id;
            this.board = board;
            routes = MatchReadRoutes.Observer(room.MatchId, () => Perspective);
            connection = new MatchConnection(api, room.MatchId, routes);
            history = new ActionLogController(api, routes, board.Log);
            reports = new BattleReportController(api, routes, board.BattleReport);
            chat = RoomChatController.ForGame(api, board.GameChat, room.MatchId);
            interaction = new ObserverInteraction(board);
            console = new ObserverConsole(board);
            sounds = soundModule == null ? null : new GameSounds(soundModule, board, NoCommands.Instance, false);
            board.Observe(Perspective);
            board.Viewing.PerspectiveChosen += Switch;
            connection.Updated += Update;
            connection.StatusChanged += ConnectionChanged;
            // REPLAY-04: an observer still watching the finished game saves the same replay as the players.
            if (replays != null)
                saver = new ReplaySaver(api, "api/observe/" + Uri.EscapeDataString(room.MatchId) + "/replay", replays, board.GameEnd);
            connection.Start();
            _ = KeepPresence(presence ?? PresenceInterval, lifetime.Token);
        }

        // A view the connection accepted: through the presentation, which shows a new point of reference at once.
        private void Update(PlayerView view)
        {
            if (disposed)
                return;
            var fresh = quietNext;
            if (fresh)
                Restart();
            playback.Receive(view);
            if (!fresh || status != "Connected")
                return;
            notices.SetConnection(status);
            playback.Playback.SetConnection(status);
        }

        // The presentation released the view (or showed it at once).
        private void Present(PlayerView view)
        {
            if (disposed)
                return;
            // The board shows no decision of its own to an observer; the console names it.
            var decision = view.Decision;
            view.Decision = null;
            if (quietNext)
            {
                board.ApplyQuietly(view);
                sounds?.Rebase();
                quietNext = false;
            }
            else
                board.Apply(view);
            sounds?.Update(view);
            console.Apply(view, decision);
            history.Update(view);
            Applied++;
        }

        // A new presentation from here on: its history's baseline is the newest page, it is not live until its first view
        // is shown, and it holds nothing (an observer sends no command).
        private void Restart()
        {
            Stop();
            notices = new ActionNoticeController(api, routes, board);
            playback = new PlaybackBinding(board, notices, Present, _ => { });
            presenters = new OpponentPresenters(board, playback);
            presenters.Sounds.Target = sounds;
        }

        private void Stop()
        {
            presenters?.Dispose();
            playback?.Dispose();
            notices?.Dispose();
            presenters = null;
            playback = null;
            notices = null;
        }

        // OB06: the other side's hand, history and reports at the same revision; an answer for the previous side is
        // dropped by the connection's generation.
        private void Switch(Side side)
        {
            if (disposed || side == Perspective)
                return;
            Perspective = side;
            // What the previous side was presenting ends now; the next view starts over from the new side.
            Stop();
            board.Observe(side);
            quietNext = true;
            history.Reset();
            connection.Rebase();
        }

        public void Show(Side side) => Switch(side);

        private void ConnectionChanged(string state)
        {
            if (disposed)
                return;
            Expired = state == "SessionExpired";
            status = state;
            notices?.SetConnection(state);
            playback?.Playback.SetConnection(state);
            sounds?.SetConnection(state);
            if (state != "Connected")
                quietNext = true;
            if (state == MatchConnection.Deleted)
                board.ShowWatchEnded();
            else
                board.SetConnection(state);
            history.SetConnection(state);
        }

        // The place in the audience lasts while it is renewed; a refused renewal ends watching.
        private async Task KeepPresence(TimeSpan interval, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(interval, token);
                    // A delay that ended as the board closed sends nothing more.
                    if (token.IsCancellationRequested || disposed)
                        return;
                    await Renew(token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        }

        private async Task Renew(CancellationToken token)
        {
            try
            {
                await api.Post<object>("api/rooms/" + Uri.EscapeDataString(room) + "/presence", null, token);
            }
            catch (ApiException refused) when (refused.Status == 404 && !disposed)
            {
                board.ShowWatchEnded();
                lifetime.Cancel();
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            // Any other failure (network, a busy server) waits for the next renewal; the lease outlasts a few misses.
            catch (Exception) { }
        }

        public void Reconnect() => connection.Reconnect();

        // Leaving frees the place at once; the room's leave is sent by the flow, which may fail without harm.
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            lifetime.Cancel();
            board.Viewing.PerspectiveChosen -= Switch;
            connection.Updated -= Update;
            connection.StatusChanged -= ConnectionChanged;
            Stop();
            interaction.Dispose();
            saver?.Dispose();
            sounds?.Dispose();
            history.Dispose();
            reports.Dispose();
            chat.Dispose();
            connection.Dispose();
            lifetime.Dispose();
        }
    }
}
