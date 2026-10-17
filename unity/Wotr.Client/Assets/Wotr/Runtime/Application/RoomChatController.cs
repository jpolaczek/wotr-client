using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // MENU-26: reads the room's new chat messages every second and sends the player's. Reading quietly retries on
    // failure; a message that was not sent stays in the field and the status says why.
    public sealed class RoomChatController : IDisposable
    {
        private readonly GameApi api;
        private readonly RoomChatPanel panel;
        private readonly Label status;
        private readonly string path;
        private readonly TimeSpan interval;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool sending;
        private bool disposed;
        // CHAT-01: a server without the board's chat answers 404; the board then hides its chat.
        public event Action Unavailable;

        public RoomChatController(GameApi api, RoomChatPanel panel, Label status, string roomId, TimeSpan? interval = null)
            : this(api, panel, status, "api/rooms/" + roomId + "/chat", interval, true)
        {
        }

        private RoomChatController(GameApi api, RoomChatPanel panel, Label status, string path, TimeSpan? interval, bool start)
        {
            this.api = api;
            this.panel = panel;
            this.status = status;
            this.path = path;
            this.interval = interval ?? TimeSpan.FromSeconds(1);
            panel.Sent += Send;
            if (start)
                _ = Poll();
        }

        // CHAT-01: the chat of the room that started this game, read every few seconds on the board.
        public static RoomChatController ForGame(GameApi api, GameChatPanel chat, string matchId, TimeSpan? interval = null)
        {
            var controller = new RoomChatController(api, chat.Chat, chat.Status, "api/matches/" + Uri.EscapeDataString(matchId) + "/chat",
                interval ?? TimeSpan.FromSeconds(3), false);
            controller.Unavailable += () => chat.Available(false);
            chat.Available(true);
            _ = controller.Poll();
            return controller;
        }

        private async Task Poll()
        {
            var token = lifetime.Token;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Read(token);
                    await Task.Delay(interval, token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        }

        private async Task Read(CancellationToken token)
        {
            try
            {
                var view = await api.Get<RoomChatView>(path + "?after=" + panel.Latest, token);
                if (!disposed)
                    panel.Show(view);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (ApiException error) when (error.Status == 404 && Unavailable != null)
            {
                if (!disposed)
                    Unavailable?.Invoke();
                lifetime.Cancel();
            }
            catch (Exception) { }
        }

        private async void Send(string text)
        {
            if (sending || disposed)
                return;
            sending = true;
            panel.Busy(true);
            try
            {
                var view = await api.Post<RoomChatView>(path, new RoomChatRequest { Text = text }, lifetime.Token);
                if (disposed)
                    return;
                panel.Clear();
                panel.Show(view);
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
            catch (Exception error)
            {
                if (!disposed)
                    status.text = Problem(error);
            }
            finally
            {
                sending = false;
                if (!disposed)
                    panel.Busy(false);
            }
        }

        // MENU-29: a room that is still shown answers 404 only from a server without the room chat (MENU-24); a room
        // that has gone takes the waiting room back to the menu on its own.
        public static string Problem(Exception error) => error switch
        {
            ApiException { Status: 400 } => "A message is one line of up to 200 characters.",
            ApiException { Status: 404 } => "This server has no room chat yet. It needs an update.",
            ApiException { Status: 429 } => "Too many messages. Wait a moment.",
            _ => "Message not sent. Try again."
        };

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            panel.Sent -= Send;
            lifetime.Cancel();
            lifetime.Dispose();
        }
    }
}
