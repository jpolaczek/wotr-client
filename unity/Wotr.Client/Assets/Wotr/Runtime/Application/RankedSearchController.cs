using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-498 (R35, docs/ui/RANKED_UX.md): a running ranked search. It owns its poller, as a waiting room does, because the
    // flow's Perform allows one operation at a time and Cancel search must stay live. A running search survives no screen
    // change, since every other route is greyed while it runs; the server's ticket lapses by itself if the client goes.
    public sealed class RankedSearchController : IDisposable
    {
        public const string Route = "api/ranked/queue";
        private static readonly TimeSpan DefaultPoll = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan DefaultPresentation = TimeSpan.FromSeconds(3);
        // A cancel that cannot reach the server stops trying once the ticket has lapsed there (6 s).
        private const float CancelPatienceSeconds = 8;

        private readonly GameApi api;
        private readonly MainMenuScreen menu;
        private readonly Action<string> openMatch;
        private readonly Func<Exception, string> explain;
        private readonly SoundModule sounds;
        private readonly TimeSpan pollInterval;
        private readonly TimeSpan presentation;
        private CancellationTokenSource running;
        private SemaphoreSlim wake;
        private RankedSearchRequest request;
        private bool accepted;
        private float cancelDeadline;
        private bool Cancelling => cancelDeadline > 0;
        private FindGameForm Form => menu.FindGame;

        public RankedSearchController(GameApi api, MainMenuScreen menu, Action<string> openMatch, Func<Exception, string> explain,
            SoundModule sounds = null, TimeSpan? pollInterval = null, TimeSpan? presentation = null)
        {
            this.api = api;
            this.menu = menu;
            this.openMatch = openMatch;
            this.explain = explain;
            this.sounds = sounds;
            this.pollInterval = pollInterval ?? DefaultPoll;
            this.presentation = presentation ?? DefaultPresentation;
            Form.Find += Start;
            Form.CancelSearch += Cancel;
        }

        // The client is going away: tell the server if it can. The ticket lapses by itself if it cannot.
        public void Dispose()
        {
            Form.Find -= Start;
            Form.CancelSearch -= Cancel;
            if (running == null)
                return;
            running.Cancel();
            running = null;
            _ = CancelQuietly();
        }

        private void Start(RankedSearchRequest search)
        {
            if (running != null)
                return;
            running = new CancellationTokenSource();
            wake = new SemaphoreSlim(0);
            request = search;
            accepted = false;
            cancelDeadline = 0;
            menu.ShowSearching(true);
            _ = Run(running.Token);
        }

        private void Cancel()
        {
            if (running == null || Cancelling)
                return;
            cancelDeadline = Time.realtimeSinceStartup + CancelPatienceSeconds;
            wake.Release();
        }

        private async Task Run(CancellationToken token)
        {
            try
            {
                while (!await Step(token))
                    await wake.WaitAsync(pollInterval, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        }

        // One exchange with the server; true when the search is over.
        private async Task<bool> Step(CancellationToken token)
        {
            try
            {
                var view = await Exchange(token);
                accepted = true;
                Form.ShowConnectionLost(false);
                return await Apply(view, token);
            }
            catch (ApiException refused) when (refused.Status < 500 && refused.Status != 429)
            {
                End(explain(refused));
                return true;
            }
            catch (Exception error) when (IsConnectionFailure(error, token))
            {
                return ConnectionLost();
            }
        }

        private Task<RankedSearchView> Exchange(CancellationToken token)
        {
            if (Cancelling)
                return api.Delete<RankedSearchView>(Route, token);
            return accepted ? api.Get<RankedSearchView>(Route, token) : api.Post<RankedSearchView>(Route, request, token);
        }

        private async Task<bool> Apply(RankedSearchView view, CancellationToken token)
        {
            switch (view.Status)
            {
                case RankedSearchStatus.Matched:
                    await Enter(view, token);
                    return true;
                case RankedSearchStatus.Searching:
                    Form.ShowElapsed(view.ElapsedSeconds);
                    return false;
                case RankedSearchStatus.Stopped:
                    End(FindGameForm.SearchStopped);
                    return true;
                default:
                    End(Cancelling ? "" : FindGameForm.SearchStopped);
                    return true;
            }
        }

        // The alert first, since the window may be in the background and the clock starts at once; the first decision's
        // extra time covers the presentation.
        private async Task Enter(RankedSearchView view, CancellationToken token)
        {
            sounds?.Request(new SoundRequest(SoundCue.DecisionRequired, "ranked-match/" + view.MatchId));
            Form.ShowOpponent(view.Opponent, view.OpponentRating);
            await Task.Delay(presentation, token);
            running = null;
            openMatch(view.MatchId);
        }

        // Keep trying: a search that lapsed or paired meanwhile is told on return. Only a cancel gives up, as nothing can
        // be paired once the ticket has lapsed.
        private bool ConnectionLost()
        {
            if (Cancelling && Time.realtimeSinceStartup > cancelDeadline)
            {
                End(FindGameForm.CancelUnconfirmed);
                return true;
            }
            Form.ShowConnectionLost(true);
            return false;
        }

        private void End(string notice)
        {
            running = null;
            menu.ShowSearching(false);
            Form.ShowStatus(notice);
        }

        private async Task CancelQuietly()
        {
            try
            {
                await api.Delete<RankedSearchView>(Route, CancellationToken.None);
            }
            catch (Exception error) when (error is ApiException or HttpRequestException or OperationCanceledException or ObjectDisposedException) { }
        }

        private static bool IsConnectionFailure(Exception error, CancellationToken token) =>
            error is ApiException or HttpRequestException ||
            error is OperationCanceledException && !token.IsCancellationRequested;
    }
}
