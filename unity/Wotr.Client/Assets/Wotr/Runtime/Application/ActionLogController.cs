using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class ActionLogController : IDisposable
    {
        private readonly GameApi api;
        private readonly string match;
        private readonly MatchReadRoutes routes;
        private readonly ActionLogPanel panel;
        private readonly Dictionary<ActionLogFilter, ActionLogModel> models = new Dictionary<ActionLogFilter, ActionLogModel>();
        private CancellationTokenSource lifetime = new CancellationTokenSource();
        private int generation;
        private readonly ActionDetailLoader details;
        private bool disposed, busy, refresh, older;
        private bool online;
        private long revision = -1;
        public ActionLogModel Model => panel.Model;

        public ActionLogController(GameApi api, string match, ActionLogPanel panel)
            : this(api, MatchReadRoutes.Player(match), panel)
        {
        }

        // OBS-03: an observer's history is read through the observer's routes, for the side being viewed.
        public ActionLogController(GameApi api, MatchReadRoutes routes, ActionLogPanel panel)
        {
            this.api = api;
            this.routes = routes;
            match = routes.Match;
            this.panel = panel;
            models[panel.Filter] = panel.Model;
            panel.Opened += Refresh;
            panel.FilterChanged += Filter;
            panel.LoadOlder += Older;
            panel.Retry += Refresh;
            details = new ActionDetailLoader(api, routes, panel, Cache);
        }

        public void Update(PlayerView view)
        {
            if (disposed || view.MatchId != match || view.Revision <= revision) return;
            revision = view.Revision;
            refresh = true;
            Pump();
        }

        public void SetConnection(string state)
        {
            online = state == "Connected";
            details.SetConnection(online);
            if (online) Refresh();
            else panel.SetStatus("Offline · Loaded entries remain available", false, busy);
        }

        private void Refresh() { refresh = true; Pump(); }

        // OBS-05: another perspective has another history: loaded pages, cached actions and the revision start over,
        // and a page still loading for the previous side is dropped.
        public void Reset()
        {
            generation++;
            lifetime.Cancel();
            lifetime.Dispose();
            lifetime = new CancellationTokenSource();
            busy = older = false;
            revision = -1;
            models.Clear();
            details.Reset();
            panel.Bind(Cache(panel.Filter));
            Refresh();
        }
        private void Older() { if (Model.Older.Length > 0) older = true; Pump(); }

        private void Filter(ActionLogFilter filter)
        {
            generation++;
            lifetime.Cancel();
            lifetime.Dispose();
            lifetime = new CancellationTokenSource();
            busy = older = false;
            panel.Bind(Cache(filter));
            Refresh();
        }

        private void Pump()
        {
            if (disposed || busy || !panel.IsOpen || !online || !refresh && !older) return;
            _ = Load(generation, panel.Filter, Model, lifetime.Token);
        }

        private async Task Load(int ticket, ActionLogFilter filter, ActionLogModel model, CancellationToken cancellation)
        {
            busy = true;
            var backward = model.Loaded && older && !refresh;
            var initial = !model.Loaded;
            if (backward) older = false;
            else refresh = false;
            panel.SetStatus("Loading…", false, true);
            try
            {
                var cursor = initial ? "" : backward ? "&before=" + Escape(model.Older) : "&after=" + Escape(model.Latest);
                var page = await api.Get<ActionLogPage>(routes.Path("/actions", "filter=" + filter + "&limit=50" + cursor), cancellation);
                if (!Current(ticket) || page.MatchId != match) return;
                panel.List.SaveAnchor();
                var added = model.Merge(page);
                if (initial || backward) model.Older = page.NextCursor;
                if (!backward) model.Latest = page.LatestCursor;
                if (!initial && !backward && added > 0) model.HasNew = true;
                model.Loaded = true;
                if (!initial && !backward && page.NextCursor.Length > 0) refresh = true;
                panel.Changed();
                panel.SetStatus(model.StepCount == 0 ? "No recorded actions." : "", false, false);
            }
            catch (OperationCanceledException) { }
            catch (Exception)
            {
                if (Current(ticket))
                {
                    refresh = older = false;
                    panel.SetStatus("History unavailable · Loaded entries remain available", true, false);
                }
            }
            finally
            {
                if (Current(ticket))
                {
                    busy = false;
                    Pump();
                }
            }
        }

        private ActionLogModel Cache(ActionLogFilter filter)
        {
            if (!models.TryGetValue(filter, out var model))
                models[filter] = model = new ActionLogModel();
            return model;
        }

        private static string Escape(string value) => Uri.EscapeDataString(value);
        private bool Current(int ticket) => !disposed && ticket == generation;

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            lifetime.Cancel();
            lifetime.Dispose();
            panel.Opened -= Refresh;
            panel.FilterChanged -= Filter;
            panel.LoadOlder -= Older;
            panel.Retry -= Refresh;
            details.Dispose();
        }
    }
}
