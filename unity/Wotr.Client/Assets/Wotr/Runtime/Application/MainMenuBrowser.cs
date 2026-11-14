using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class MainMenuBrowser : IDisposable
    {
        private readonly GameApi api;
        private readonly MainMenuScreen screen;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly MenuPageLoader<PublicRoomSummary> publicRooms;
        private readonly MenuPageLoader<MatchSummary> matches;
        private readonly MenuPageLoader<MatchSummary> finished;
        private CancellationTokenSource request;
        private int generation;
        private bool loading;
        private bool refused;

        public MainMenuBrowser(GameApi api, MainMenuScreen screen)
        {
            this.api = api;
            this.screen = screen;
            publicRooms = new MenuPageLoader<PublicRoomSummary>(PublicPage, room => room.Id);
            matches = new MenuPageLoader<MatchSummary>(MatchPage, match => match.MatchId);
            finished = new MenuPageLoader<MatchSummary>(FinishedPage, match => match.MatchId);
            screen.MorePublic += MorePublic;
            screen.MoreMatches += MoreMatches;
            screen.MoreFinished += MoreFinished;
            screen.Refresh += Refresh;
            screen.DeleteMatch += DeleteMatch;
            _ = Poll();
        }

        // UI-537: My games' confirmed x removes the game for both players (MENU-21: DELETE api/matches/{id}); the row
        // leaves at once and the lists reload. A ranked game is refused (RANK-04); a game already gone is gone.
        private void DeleteMatch(MatchSummary match) => _ = Delete(match);

        private async Task Delete(MatchSummary match)
        {
            try
            {
                await api.Delete("api/matches/" + Uri.EscapeDataString(match.MatchId), lifetime.Token);
                screen.RemoveMatch(match.MatchId);
                screen.Status.text = "";
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
            {
                return;
            }
            catch (ApiException error) when (error.Status == 404)
            {
                screen.RemoveMatch(match.MatchId);
            }
            catch (ApiException error) when (error.Status == 409)
            {
                screen.Status.text = "A ranked game cannot be deleted.";
            }
            catch (Exception)
            {
                screen.Status.text = "The game could not be deleted. Check the connection and try again.";
            }
            if (!lifetime.IsCancellationRequested)
                Refresh();
        }

        public void Refresh() => _ = Load();
        private void MorePublic()
        {
            if (publicRooms.NextCursor.Length > 0) More(LoadPublic);
        }
        private void MoreMatches()
        {
            if (matches.NextCursor.Length > 0) More(LoadMatches);
        }

        private void MoreFinished()
        {
            if (finished.NextCursor.Length > 0) More(LoadFinished);
        }

        private async Task<MenuPage<PublicRoomSummary>> PublicPage(string cursor, CancellationToken token)
        {
            var page = await api.Get<PublicRoomListView>("api/rooms/public?cursor=" + Uri.EscapeDataString(cursor), token);
            return new MenuPage<PublicRoomSummary> { Items = page.Rooms, NextCursor = page.NextCursor };
        }

        private async Task<MenuPage<MatchSummary>> MatchPage(string cursor, CancellationToken token)
        {
            var page = await api.Get<MatchListView>("api/matches?cursor=" + Uri.EscapeDataString(cursor), token);
            return new MenuPage<MatchSummary> { Items = page.Matches, NextCursor = page.NextCursor };
        }

        // UI-496: LOBBY-05 keeps finished games out of the default list; they have their own tab.
        private async Task<MenuPage<MatchSummary>> FinishedPage(string cursor, CancellationToken token)
        {
            var page = await api.Get<MatchListView>("api/matches?status=Finished&cursor=" + Uri.EscapeDataString(cursor), token);
            return new MenuPage<MatchSummary> { Items = page.Matches, NextCursor = page.NextCursor };
        }

        private CancellationToken BeginRequest()
        {
            generation++;
            request?.Cancel();
            request?.Dispose();
            request = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            loading = true;
            return request.Token;
        }

        // MENU-25: a reason the menu was opened with (a player removed from a room) stays in the status line.
        public string Notice { get; set; } = "";

        private async Task Load()
        {
            refused = false;
            var token = BeginRequest();
            var version = generation;
            try
            {
                var errors = await Task.WhenAll(
                    OwnAvatar(token),
                    Section(() => LoadRooms(token), token),
                    Section(() => LoadPublic(false, token), token),
                    Section(() => LoadMatches(false, token), token),
                    Section(() => LoadFinished(false, token), token));
                if (!token.IsCancellationRequested && version == generation)
                    screen.Status.text = string.Join(" ", errors.Prepend(Notice).Where(error => error.Length > 0).Distinct());
            }
            finally
            {
                if (version == generation)
                {
                    loading = false;
                    if (!token.IsCancellationRequested) screen.RequestMoreIfNeeded();
                }
            }
        }

        // UI-505: a change made in the browser. Failing to learn it is no news: the line keeps the picture it has, and the
        // lists below say if the server is gone.
        private async Task<string> OwnAvatar(CancellationToken token)
        {
            try
            {
                var me = await api.Get<SessionView>("api/auth/me", token);
                token.ThrowIfCancellationRequested();
                api.AvatarSeen(me.Avatar);
                screen.ShowOwnAvatar(me.Avatar);
            }
            catch (Exception error) when (!(error is OperationCanceledException))
            {
                UnityEngine.Debug.Log("Own avatar not read: " + error.GetType().Name);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            return "";
        }

        private async Task LoadRooms(CancellationToken token)
        {
            var rooms = await api.Get<List<RoomView>>("api/rooms", token);
            token.ThrowIfCancellationRequested();
            screen.ShowRooms(rooms);
        }

        private async Task LoadPublic(bool more, CancellationToken token)
        {
            if (more) await publicRooms.More(token);
            else await publicRooms.Refresh(token);
            screen.ShowPublic(new PublicRoomListView { Rooms = publicRooms.Items });
        }

        private async Task LoadMatches(bool more, CancellationToken token)
        {
            if (more) await matches.More(token);
            else await matches.Refresh(token);
            screen.ShowMatches(new MatchListView { Matches = matches.Items });
        }

        private async void More(Func<bool, CancellationToken, Task> load)
        {
            if (loading || lifetime.IsCancellationRequested) return;
            var token = BeginRequest();
            var version = generation;
            var errorFree = false;
            try
            {
                var error = await Section(() => load(true, token), token);
                errorFree = error.Length == 0;
                if (!token.IsCancellationRequested && version == generation) screen.Status.text = error;
            }
            finally
            {
                if (version == generation)
                {
                    loading = false;
                    if (!token.IsCancellationRequested && errorFree) screen.RequestMoreIfNeeded();
                }
            }
        }

        private async Task<string> Section(Func<Task> load, CancellationToken token)
        {
            try { await load(); return ""; }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { return ""; }
            catch (Exception error)
            {
                refused |= VersionRefusal.Is(error);
                return Error(error);
            }
        }

        private async Task Poll()
        {
            try
            {
                while (!lifetime.IsCancellationRequested)
                {
                    await Task.Delay(5000, lifetime.Token);
                    // DIST-07: a refused version waits for the update or the player's Refresh.
                    if (!loading && !refused) await Load();
                }
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        }

        private async Task LoadFinished(bool more, CancellationToken token)
        {
            if (more) await finished.More(token);
            else await finished.Refresh(token);
            screen.ShowFinished(new MatchListView { Matches = finished.Items });
        }

        private static string Error(Exception error)
        {
            if (error is ApiException apiError)
            {
                if (apiError.Status == 401) return "Session expired. Sign out and sign in again.";
                if (apiError.Status == 404) return "The server needs a lobby update. Some games could not be loaded.";
                if (VersionRefusal.Is(error)) return VersionRefusal.Text(apiError.Message);
            }
            return "Some games could not be loaded. Check the connection and retry Refresh.";
        }

        public void Dispose()
        {
            screen.MorePublic -= MorePublic;
            screen.MoreMatches -= MoreMatches;
            screen.MoreFinished -= MoreFinished;
            screen.Refresh -= Refresh;
            screen.DeleteMatch -= DeleteMatch;
            lifetime.Cancel();
            request?.Cancel();
            request?.Dispose();
            lifetime.Dispose();
        }
    }
}
