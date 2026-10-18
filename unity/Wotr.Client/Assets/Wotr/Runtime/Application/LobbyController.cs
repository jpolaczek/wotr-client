using System;
using System.Threading;
using System.Threading.Tasks;
using Wotr.Contracts;

namespace Wotr.Client
{
    // LOBBY-01: keeps the main menu's lobby current — one poll every LobbyLimits.PollSeconds (which also keeps this
    // player online), the chat's sends, challenges and their answers. An accepted challenge opens its room: for the
    // challenged player from the answer, for the challenger from the next poll.
    public sealed class LobbyController : IDisposable
    {
        public LobbyState State { get; }
        public event Action<RoomView> RoomOpened;
        private readonly GameApi api;
        private readonly LobbyPanel panel;
        private readonly ChallengeTab tab;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool opening;

        // LOBBY-02: the state belongs to the sign-in, so the chat seen so far comes back when the menu does.
        public LobbyController(GameApi api, LobbyPanel panel, ChallengeTab tab, LobbyState state)
        {
            this.api = api;
            this.panel = panel;
            this.tab = tab;
            State = state;
            panel.Apply(State);
            panel.Send += text => _ = SendMessage(text);
            panel.Challenge += name => _ = SendChallenge(name);
            tab.Accept += id => _ = Accept(id);
            tab.Decline += id => _ = Decline(id);
            _ = Poll();
        }

        private async Task Poll()
        {
            while (!lifetime.IsCancellationRequested)
            {
                await Refresh();
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(LobbyLimits.PollSeconds), lifetime.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        public async Task Refresh()
        {
            try
            {
                var view = await api.Get<LobbyView>("api/lobby?after=" + State.Latest, lifetime.Token);
                if (lifetime.IsCancellationRequested)
                    return;
                var outcomes = State.Apply(view);
                panel.Apply(State);
                tab.Show(State.Incoming);
                foreach (var outcome in outcomes)
                    await Report(outcome);
            }
            catch (Exception) when (!lifetime.IsCancellationRequested)
            {
                // The next poll tries again; the menu stays usable without the lobby.
            }
        }

        private async Task Report(ChallengeView outcome)
        {
            panel.Status.text = LobbyState.Outcome(outcome);
            if (outcome.Status == ChallengeStatus.Accepted && outcome.RoomId.Length > 0)
                await Open(() => api.Get<RoomView>("api/rooms/" + outcome.RoomId, lifetime.Token));
        }

        private async Task SendMessage(string text)
        {
            try
            {
                var message = await api.Post<LobbyMessage>("api/lobby/messages", new LobbyMessageRequest { Text = text }, lifetime.Token);
                State.AddMessages(new[] { message });
                panel.Apply(State);
                panel.Status.text = "";
            }
            catch (ApiException error)
            {
                panel.Status.text = LobbyState.Refusal(Code(error), "");
            }
            catch (Exception) when (!lifetime.IsCancellationRequested)
            {
                panel.Status.text = LobbyState.Refusal("", "");
            }
        }

        private async Task SendChallenge(string name)
        {
            try
            {
                await api.Post<ChallengeView>("api/lobby/challenges", new ChallengeRequest { Name = name }, lifetime.Token);
                panel.Status.text = "Challenge sent to " + name + ". Waiting for an answer…";
            }
            catch (ApiException error)
            {
                panel.Status.text = LobbyState.Refusal(Code(error), name);
            }
            catch (Exception) when (!lifetime.IsCancellationRequested)
            {
                panel.Status.text = LobbyState.Refusal("", name);
            }
        }

        private async Task Accept(string id)
        {
            State.Dismiss(id);
            tab.Hide();
            await Open(() => api.Post<RoomView>("api/lobby/challenges/" + Uri.EscapeDataString(id) + "/accept", null, lifetime.Token));
        }

        private async Task Decline(string id)
        {
            State.Dismiss(id);
            tab.Hide();
            try
            {
                await api.Post<object>("api/lobby/challenges/" + Uri.EscapeDataString(id) + "/decline", null, lifetime.Token);
            }
            catch (Exception) when (!lifetime.IsCancellationRequested)
            {
                // A challenge that already closed needs no answer.
            }
        }

        private async Task Open(Func<Task<RoomView>> load)
        {
            if (opening)
                return;
            opening = true;
            try
            {
                var room = await load();
                RoomOpened?.Invoke(room);
            }
            catch (ApiException error)
            {
                panel.Status.text = LobbyState.Refusal(Code(error), "");
            }
            catch (Exception) when (!lifetime.IsCancellationRequested)
            {
                panel.Status.text = LobbyState.Refusal("", "");
            }
            finally
            {
                opening = false;
            }
        }

        private static string Code(ApiException error) => error.Status == 429 ? "TooManyRequests" : error.Message;

        public void Dispose()
        {
            lifetime.Cancel();
            lifetime.Dispose();
        }
    }
}
