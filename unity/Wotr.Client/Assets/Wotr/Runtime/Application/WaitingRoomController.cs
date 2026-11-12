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
        // UI-533: the waiting room of a game against the CPU played from the other side (ClientFlow makes it).
        private readonly Func<RoomView, Task> swapSides;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly RoomChatController chat;
        private readonly CancellationToken token;
        private readonly TimeSpan pollInterval;
        private bool busy;
        private bool disposed;
        // The status shows a failure of the background refresh, which the next successful one clears.
        private bool pollFailed;
        private bool refused;
        private AddRoomBotRequest invitation;

        // pollInterval: how often the room is refreshed in the background (2 s; tests may shorten it).
        public WaitingRoomController(GameApi api, WaitingRoomScreen screen, RoomView room,
            Func<string, Task> openMatch, Func<Task> menu, Action expired, TimeSpan? pollInterval = null, Func<Task> removed = null,
            Func<RoomView, Task> observe = null, Func<RoomView, Task> swapSides = null)
        {
            this.swapSides = swapSides;
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
            screen.AddCpu += InviteCpu;
            screen.SwapSides += SwapSides;
            screen.ChangeCpuPace += ChangeCpuPace;
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
        private void Ready(bool value) => Run(async () => Apply(await api.Put<RoomView>(Path + "/ready",
            new RoomReadyRequest { ExpectedVersion = screen.Room.Version, Ready = value }, token)));
        // UI-263: the host's rules; the server resets both players' Ready and answers with the room. A CPU seat
        // rejoins under the new rules, so the answer goes through Apply like every roster change.
        private void Configure(MatchConfiguration configuration) => Run(async () => Apply(await api.Put<RoomView>(
            Path + "/configuration", new ConfigureRoomRequest { ExpectedVersion = screen.Room.Version, Configuration = configuration },
            token)));
        private void Invite() => Run(async () => Apply(await api.Post<RoomView>(Path + "/invitation", null, token)));
        // BOT-37: the CPU's pace is a change of the room: its version rises and both players get ready again.
        public void ChangeCpuPace(BotThinkingMode mode) => Run(async () => Apply(await api.Put<RoomView>(Path + "/bot/thinking",
            new ConfigureRoomBotRequest { ExpectedVersion = screen.Room.Version, ThinkingMode = mode }, token)));
        // The flow replaces this waiting room with the new one, so nothing here waits for it.
        private void SwapSides()
        {
            if (!busy && !disposed && swapSides != null && screen.Room != null)
                _ = swapSides(screen.Room);
        }

        public void InviteCpu() => InviteCpu(BotThinkingMode.Fast);

        // UI-533: the pace comes with the invitation when a swap of sides brings the CPU back.
        public void InviteCpu(BotThinkingMode mode) => Run(async () =>
        {
            invitation ??= new AddRoomBotRequest
            {
                RequestId = Guid.NewGuid().ToString("N"),
                ExpectedVersion = screen.Room.Version,
                ThinkingMode = mode
            };
            var side = screen.Room.You == Side.FreePeoples ? Side.Shadow : Side.FreePeoples;
            screen.ShowInvitation(side);
            try
            {
                Apply(await api.Put<RoomView>(Path + "/bot", invitation, token));
                invitation = null;
                screen.ShowInvitation(null);
            }
            catch (Exception error) when (!token.IsCancellationRequested)
            {
                // A lost response has an unknown outcome. Retry its original ID/version,
                // until an authoritative roster or configuration change resolves it.
                if (error is ApiException rejected && rejected.Status < 500 && rejected.Status != 408 && rejected.Status != 429)
                    invitation = null;
                screen.ShowInvitation(side, BotSeatStatus.Unavailable);
                throw;
            }
        });

        private void Apply(RoomView room)
        {
            token.ThrowIfCancellationRequested();
            if (room.Version < screen.Room.Version) return;
            if (room.Bot != null || room.Version != screen.Room.Version || invitation != null && room.Version != invitation.ExpectedVersion)
            {
                invitation = null;
                screen.ShowInvitation(null);
            }
            screen.Update(room);
        }
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
        private void Remove(string name) => Run(async () => Apply(await api.Post<RoomView>(Path + "/remove",
            new RemoveParticipantRequest { Name = name }, token)));
        // OBS-04: a free seat for an observer or the other player; the server refuses a seat taken meanwhile.
        private void TakeSeat(Side side) => Run(async () => Apply(await api.Post<RoomView>(Path + "/role",
            new RoomRoleRequest { Role = RoomRole.Player, Side = side }, token)));
        private void Observe() => Run(async () => Apply(await api.Post<RoomView>(Path + "/role",
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
            Apply(room);
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
            Apply(room);
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
                if (apiError.Message == "BotUnavailable") return "CPU is unavailable. Try again later.";
                if (apiError.Message == "BotNotReady") return "CPU is still joining. Wait for Ready.";
                if (apiError.Message == "BotConfigurationUnsupported" || apiError.Message == "BotVersionUnsupported")
                    return "CPU cannot play this configuration here.";
                if (apiError.Message == "BotReservationCancelled") return "CPU was removed. Refresh the room.";
                if (apiError.Message == "BotRequestConflict") return "The room changed. Refresh before inviting CPU again.";
                if (apiError.Message == "BotNotSeated") return "CPU is not in this room any more. Refresh the room.";
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
            screen.AddCpu -= InviteCpu;
            screen.SwapSides -= SwapSides;
            screen.ChangeCpuPace -= ChangeCpuPace;
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
