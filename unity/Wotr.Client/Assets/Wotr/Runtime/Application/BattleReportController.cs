using System;
using System.Threading;
using System.Threading.Tasks;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class BattleReportController : IDisposable
    {
        private readonly GameApi api;
        private readonly string match;
        private readonly MatchReadRoutes routes;
        private readonly BattleReport panel;
        private readonly NotificationReadStore reads;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool disposed;

        public BattleReportController(GameApi api, string match, BattleReport panel, NotificationReadStore reads = null)
            : this(api, MatchReadRoutes.Player(match), panel, reads)
        {
        }

        public BattleReportController(GameApi api, MatchReadRoutes routes, BattleReport panel, NotificationReadStore reads = null)
        {
            this.api = api;
            this.routes = routes;
            match = routes.Match;
            this.panel = panel;
            this.reads = reads;
            panel.Requested += Request;
            panel.RoundRead += Remember;
            if (reads != null)
                panel.MarkRead(reads.Steps);
        }

        // UI-112: the read ⚔ round stays hidden after a restart; without storage it is hidden for this session.
        private void Remember(string key) => reads?.Acknowledge(new[] { key });

        private void Request(string id, int version) => _ = Load(id, version);

        private async Task Load(string id, int version)
        {
            try
            {
                var report = await api.Get<BattleReportView>(routes.Path("/battles/" + Uri.EscapeDataString(id), ""), lifetime.Token);
                if (Current(version) && report.MatchId == match && report.Id == id) panel.Show(report);
            }
            catch (OperationCanceledException) { }
            catch (Exception)
            {
                if (Current(version)) panel.SetStatus("Full report unavailable · Loaded details remain readable", true);
            }
        }

        private bool Current(int version) => !disposed && version == panel.RequestVersion;

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            panel.Requested -= Request;
            panel.RoundRead -= Remember;
            lifetime.Cancel();
            lifetime.Dispose();
        }
    }
}
