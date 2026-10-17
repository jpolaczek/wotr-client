using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Independent from list pagination: changing a filter cannot replace a pinned historical selection.
    public sealed class ActionDetailLoader : IDisposable
    {
        private readonly GameApi api;
        private readonly string match;
        private readonly MatchReadRoutes routes;
        private readonly ActionLogPanel panel;
        private readonly Func<ActionLogFilter, ActionLogModel> cache;
        private readonly Dictionary<string, BattleReportView> battles = new Dictionary<string, BattleReportView>();
        private readonly Queue<string> battleOrder = new Queue<string>();
        private CancellationTokenSource lifetime = new CancellationTokenSource();
        private int generation;
        private bool online, disposed;

        public ActionDetailLoader(GameApi api, string match, ActionLogPanel panel, Func<ActionLogFilter, ActionLogModel> cache)
            : this(api, MatchReadRoutes.Player(match), panel, cache)
        {
        }

        public ActionDetailLoader(GameApi api, MatchReadRoutes routes, ActionLogPanel panel, Func<ActionLogFilter, ActionLogModel> cache)
        {
            this.api = api;
            this.routes = routes;
            match = routes.Match;
            this.panel = panel;
            this.cache = cache;
            panel.Inspect += Inspect;
            panel.CardClosed += Cancel;
        }

        public void SetConnection(bool value) => online = value;
        private void Inspect(ActionOpenRequest request, bool pinned)
        {
            Cancel();
            _ = Load(request, generation, lifetime.Token);
        }

        private async Task Load(ActionOpenRequest request, int ticket, CancellationToken cancellation)
        {
            var model = cache(request.Filter);
            var stored = model.Find(request.ActionId);
            var all = stored != null && stored.Steps.Count == stored.Action.StepCount;
            var view = stored == null ? null : ActionVersion.Through(stored, request.Version, request.IncludeConsequences);
            if (view != null) panel.ShowAction(view, false);
            try
            {
                if (!all || view == null)
                {
                    if (!online) { panel.SetCardStatus("Offline · Remaining steps require a connection", true); return; }
                    stored = await api.Get<ActionLogView>(routes.Path("/actions/" + Escape(request.ActionId), "filter=" + request.Filter), cancellation);
                    if (!Current(ticket, request) || stored.MatchId != match) return;
                    panel.List.SaveAnchor();
                    model.Details(stored);
                    if (panel.Model == model) panel.Changed();
                    view = ActionVersion.Through(stored, request.Version, request.IncludeConsequences);
                    if (view == null) { panel.SetCardStatus("This recorded version is unavailable", true); return; }
                }
                await Complete(view, request, ticket, cancellation);
            }
            catch (OperationCanceledException) { }
            catch (Exception)
            {
                if (Current(ticket, request)) panel.SetCardStatus("Details unavailable", true);
            }
        }

        private async Task Complete(ActionLogView view, ActionOpenRequest request, int ticket, CancellationToken cancellation)
        {
            var context = ActionContextModel.For(view, request.StepId);
            BattleReportView report = null;
            if (context.BattleId.Length > 0)
            {
                panel.ShowAction(view, false);
                var key = context.BattleId + "/" + view.Action.Version;
                if (!battles.TryGetValue(key, out report))
                {
                    if (!online) { panel.SetCardStatus("Offline · Battle report requires a connection", true); return; }
                    panel.SetCardStatus("Loading battle report…", false);
                    report = await api.Get<BattleReportView>(routes.Path("/battles/" + Escape(context.BattleId), ""), cancellation);
                    if (!Current(ticket, request) || report.MatchId != match || report.Id != context.BattleId) return;
                    report = BattleReportVersion.Through(report, view);
                    CacheBattle(key, report);
                }
            }
            if (Current(ticket, request)) panel.ShowAction(view, true, report);
        }

        // OBS-05: the battle reports of the previous perspective go with its history.
        public void Reset()
        {
            Cancel();
            battles.Clear();
            battleOrder.Clear();
        }
        private void CacheBattle(string key, BattleReportView report)
        {
            if (!battles.ContainsKey(key)) battleOrder.Enqueue(key);
            battles[key] = report;
            while (battleOrder.Count > 12) battles.Remove(battleOrder.Dequeue());
        }

        private static string Escape(string value) => Uri.EscapeDataString(value);
        private bool Current(int ticket, ActionOpenRequest request) => !disposed && ticket == generation && panel.Selection == request;
        private void Cancel()
        {
            generation++;
            lifetime.Cancel();
            lifetime.Dispose();
            lifetime = new CancellationTokenSource();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            panel.Inspect -= Inspect;
            panel.CardClosed -= Cancel;
            lifetime.Cancel();
            lifetime.Dispose();
        }
    }
}
