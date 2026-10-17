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
    public sealed class ObserverSession : IDisposable
    {
        public static readonly TimeSpan PresenceInterval = TimeSpan.FromSeconds(15);
        private readonly GameApi api;
        private readonly string room;
        private readonly BoardScreen board;
        private readonly MatchConnection connection;
        private readonly ActionLogController history;
        private readonly BattleReportController reports;
        private readonly ObserverInteraction interaction;
        private readonly ObserverConsole console;
        private readonly ReplaySaver saver;
        // REPLAY-06: the facts sound as on a player's board, without decision alerts or a personal result; the first
        // view, a reconnect and a change of side are silent.
        private readonly GameSounds sounds;
        private bool quietNext = true;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool disposed;
        public Side Perspective { get; private set; } = Side.FreePeoples;
        public bool Expired { get; private set; }
        public string Room => room;
        // How many views the board drew; tests read it to see a change of side arrive.
        public int Applied { get; private set; }

        public ObserverSession(GameApi api, RoomView room, BoardScreen board, TimeSpan? presence = null, ReplayLibrary replays = null,
            SoundModule soundModule = null)
        {
            this.api = api;
            this.room = room.Id;
            this.board = board;
            var routes = MatchReadRoutes.Observer(room.MatchId, () => Perspective);
            connection = new MatchConnection(api, room.MatchId, routes);
            history = new ActionLogController(api, routes, board.Log);
            reports = new BattleReportController(api, routes, board.BattleReport);
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

        private void Update(PlayerView view)
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

        // OB06: the other side's hand, history and reports at the same revision; an answer for the previous side is
        // dropped by the connection's generation.
        private void Switch(Side side)
        {
            if (disposed || side == Perspective)
                return;
            Perspective = side;
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
            interaction.Dispose();
            saver?.Dispose();
            sounds?.Dispose();
            history.Dispose();
            reports.Dispose();
            connection.Dispose();
            lifetime.Dispose();
        }
    }
}
