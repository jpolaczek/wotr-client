using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Wotr.Contracts;

namespace Wotr.Client.Updates
{
    // GET /api/client/releases (DIST-05/DIST-09): public metadata before and after login, without a session. The
    // declared version and protocol give the server's verdict for this copy and the offer of its own release.
    public sealed class ReleaseCatalogClient : IReleaseCatalog, IDisposable
    {
        private readonly GameApi api;

        public ReleaseCatalogClient(GameApi api) => this.api = api;

        public Task<ClientReleaseView> Read(CancellationToken cancellation)
        {
            var identity = api.Identity;
            return api.Get<ClientReleaseView>("api/client/releases?platform=" + Uri.EscapeDataString(identity.Platform) +
                "&version=" + Uri.EscapeDataString(identity.Version) +
                "&protocol=" + identity.ProtocolVersion.ToString(CultureInfo.InvariantCulture), cancellation);
        }

        public void Dispose() => api.Dispose();
    }
}
