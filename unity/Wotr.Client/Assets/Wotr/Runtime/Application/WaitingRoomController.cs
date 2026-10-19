using System;
using System.Threading;
using System.Threading.Tasks;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class WaitingRoomController : IDisposable
    {
        private readonly GameApi api;
        private readonly WaitingRoomScreen screen;
        private readonly Func<string, Task> openMatch;
        private readonly Func<Task> menu;
        // MENU-25: where a player removed by the host goes (the main menu with the reason); the menu by default.
        private readonly Func<Task> removed;
        // OBS-04: what an observer's waiting room does once the game starts (the observer board, OBS-05).
        private readonly Func<RoomView, Task> observe;
        private readonly Action expired;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly RoomChatController chat;
        private readonly CancellationToken token;
        private readonly TimeSpan pollInterval;
        private bool busy;
        private bool disposed;
        // The status shows a failure of the background refresh, which the next successful one clears.
        private bool pollFailed;
        private bool refused;

        // pollInterval: how often the room is refreshed in the background (2 s; tests may shorten it).
        public WaitingRoomController(GameApi api, WaitingRoomScreen screen, RoomView room,
            Func<string, Task> openMatch, Func<Task> menu, Action expired, TimeSpan? pollInterval = null, Func<Task> removed = null,
            Func<RoomView, Task> observe = null)
        {
            this.removed = removed ?? menu;
            this.observe = observe ?? (_ => Task.CompletedTask);
            this.pollInterval = pollInterval ?? TimeSpan.FromSeconds(2);
            this.api = api;
            this.screen = screen;
            this.openMatch = openMatch;
            this.menu = menu;
            this.expired = expired;
            token = lifetime.Token;
            screen.Update(room);
            // MENU-26: the chat of this room lives as long as its waiting room.
            var chatPanel = new RoomChatPanel();
            screen.ChatHost.Add(chatPanel.Root);
            chat = new RoomChatController(api, chatPanel, screen.Status, room.Id);
            screen.Ready += Ready;
            screen.Configure += Configure;
            screen.Invite += Invite;
            screen.Start += Start;
            screen.Demo += Demo;
            screen.Remove += Remove;
            screen.TakeSeat += TakeSeat;
            screen.Observe += Observe;
            screen.Back += Back;
            screen.Refresh += Refresh;
            _ = Poll();
        }
        private string Path => "api/rooms/" + screen.Room.Id;
        private void Ready(bool value) => Run(async () => screen.Update(await api.Put<RoomView>(Path + "/ready",
            new RoomReadyRequest { ExpectedVersion = screen.Room.Version, Ready = value }, token)));
        // UI-263: the host's rules; the server resets both players' Ready and answers with the room.
        private void Configure(MatchConfiguration configuration) => Run(async () => screen.Update(await api.Put<RoomView>(
            Path + "/configuration", new ConfigureRoomRequest { ExpectedVersion = screen.Room.Version, Configuration = configuration },
            token)));
        private void Invite() => Run(async () => screen.Update(await api.Post<RoomView>(Path + "/invitation", null, token)));
        private void Start() => Run(async () =>
        {
            var room = await api.Post<RoomView>(Path + "/start",
                new RoomRevisionRequest { ExpectedVersion = screen.Room.Version }, token);
            token.ThrowIfCancellationRequested();
            await openMatch(room.MatchId);
        });
        // DEMO-04: the demo replaces the player's previous one and opens like a started game; the room keeps waiting.
        private void Demo() => Run(async () =>
        {
            var demo = await api.Post<DemoMatchView>(Path + "/demo", null, token);
            token.ThrowIfCancellationRequested();
            await openMatch(demo.MatchId);
        });
        // MENU-25: the host frees a seat for good; OBS-04: or removes an observer, by name.
        private void Remove(string name) => Run(async () => screen.Update(await api.Post<RoomView>(Path + "/remove",
            new RemoveParticipantRequest { Name = name }, token)));
        // OBS-04: a free seat for an observer or the other player; the server refuses a seat taken meanwhile.
        private void TakeSeat(Side side) => Run(async () => screen.Update(await api.Post<RoomView>(Path + "/role",
            new RoomRoleRequest { Role = RoomRole.Player, Side = side }, token)));
        private void Observe() => Run(async () => screen.Update(await api.Post<RoomView>(Path + "/role",
            new RoomRoleRequest { Role = RoomRole.Observer }, token)));
        private void Back() => Run(async () =>
        {
            try
            {
                await api.Post<object>(Path + "/leave", null, token);
            }
            catch (ApiException error) when (error.Status == 404)
            {
                // A removed room no longer holds a seat. Back must still leave this screen.
            }
            token.ThrowIfCancellationRequested();
            await menu();
        });
        private void Refresh() => Run(Load);
        private async void Run(Func<Task> operation)
        {
            if (busy || disposed) return;
            busy = true;
            screen.Busy(true);
            screen.Status.text = "";
            pollFailed = false;
            refused = false;
            try { await operation(); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (ApiException gone) when (gone.Status == 410)
            {
                if (!disposed) await removed();
            }
            catch (Exception error)
            {
                if (!disposed)
                {
                    if (error is ApiException rejected && rejected.Status == 401) expired();
                    else screen.Status.text = Message(error);
                }
            }
            finally
            {
                busy = false;
                if (!disposed) screen.Busy(false);
            }
        }
        private async Task Load()
        {
            var room = await api.Get<RoomView>(Path, token);
            token.ThrowIfCancellationRequested();
            if (room.Status == RoomStatus.Closed) { await menu(); return; }
            screen.Update(room);
            screen.Status.text = "";
            await OpenStarted(room);
        }

        private Task OpenStarted(RoomView room)
        {
            if (room.MatchId.Length == 0)
                return Task.CompletedTask;
            return room.Role == RoomRole.Observer ? observe(room) : openMatch(room.MatchId);
        }
        // MENU-19: the background refresh keeps every control enabled and the player's status in place; only the
        // player's own requests (Run) show the room as busy.
        private async Task Poll()
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(pollInterval, token);
                    // DIST-07: a refused version waits for the update or the player's Refresh.
                    if (!busy && !disposed && !refused)
                        await RefreshQuietly();
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        }

        private async Task RefreshQuietly()
        {
            RoomView room;
            try
            {
                room = await api.Get<RoomView>(Path, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (ApiException gone) when (gone.Status == 410)
            {
                if (!busy && !disposed) await removed();
                return;
            }
            catch (Exception error)
            {
                ShowPollFailure(error);
                return;
            }
            token.ThrowIfCancellationRequested();
            // A request the player started meanwhile reports its own result.
            if (busy || disposed)
                return;
            if (room.Status == RoomStatus.Closed) { await menu(); return; }
            screen.Update(room);
            if (pollFailed)
            {
                screen.Status.text = "";
                pollFailed = false;
            }
            await OpenStarted(room);
        }

        private void ShowPollFailure(Exception error)
        {
            if (disposed)
                return;
            if (error is ApiException rejected && rejected.Status == 401)
            {
                expired();
                return;
            }
            screen.Status.text = Message(error);
            pollFailed = true;
            refused = VersionRefusal.Is(error);
        }
        private static string Message(Exception error)
        {
            if (error is ApiException apiError)
            {
                if (apiError.Message == "StaleConfiguration") return "The room changed. Refresh and choose Ready again.";
                if (apiError.Message == "ExpansionsUnavailable") return "This server does not offer that expansion yet.";
                if (apiError.Message == "InvalidConfiguration") return "That combination of rules is not allowed.";
                if (apiError.Message == "ConfirmationRequired") return "Both players must be ready before starting.";
                if (apiError.Message == "HostRequired") return "Only the host can do this.";
                if (apiError.Message == "MatchAlreadyStarted") return "The game in this room has started. Open it from your games.";
                if (apiError.Message == "SeatEmpty") return "Nobody holds the other seat.";
                if (apiError.Message == "SeatTaken") return "That seat was just taken.";
                if (apiError.Message == "ObserversFull") return "All observer places are taken.";
                if (apiError.Message == "RolesLocked") return "Seats cannot change after the game has started.";
                if (apiError.Message == "PlayerRequired") return "Only the players can do this.";
                if (apiError.Message == "ParticipantNotFound") return "That person is no longer in this room.";
                if (apiError.Status == 404) return "This room is unavailable. Use Back to return to the menu.";
                if (VersionRefusal.Is(error)) return VersionRefusal.Text(apiError.Message);
                return "Request declined. Refresh and try again.";
            }
            return "Connection interrupted. Your seat is kept. Retry Refresh when the server is available.";
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            screen.Ready -= Ready;
            screen.Configure -= Configure;
            screen.Invite -= Invite;
            screen.Start -= Start;
            screen.Demo -= Demo;
            screen.Remove -= Remove;
            screen.TakeSeat -= TakeSeat;
            screen.Observe -= Observe;
            screen.Back -= Back;
            chat.Dispose();
            screen.Refresh -= Refresh;
            lifetime.Cancel();
            lifetime.Dispose();
        }
    }
}
