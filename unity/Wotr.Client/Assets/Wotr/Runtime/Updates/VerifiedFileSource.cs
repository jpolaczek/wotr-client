using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Velopack;
using Velopack.Logging;
using Velopack.Sources;

namespace Wotr.Client.Updates
{
    // The game downloads and verifies the package itself (signed manifest, SHA-256); this source hands Velopack the
    // one already-verified local file and never touches the network (spike S1).
    internal sealed class VerifiedFileSource : IUpdateSource
    {
        private readonly string package;
        private readonly VelopackAsset asset;

        public VerifiedFileSource(string package, VelopackAsset asset)
        {
            this.package = package;
            this.asset = asset;
        }

        public Task<VelopackAssetFeed> GetReleaseFeed(IVelopackLogger logger, string appId, string channel,
            Guid? stagingId = null, VelopackAsset latestLocalRelease = null)
        {
            return Task.FromResult(new VelopackAssetFeed { Assets = new[] { asset } });
        }

        public Task DownloadReleaseEntry(IVelopackLogger logger, VelopackAsset releaseEntry, string localFile,
            Action<int> progress, CancellationToken cancelToken = default)
        {
            if (releaseEntry.FileName != asset.FileName)
                throw new InvalidOperationException("Velopack asked for " + releaseEntry.FileName + ", not the verified " + asset.FileName);
            File.Copy(package, localFile, true);
            progress?.Invoke(100);
            return Task.CompletedTask;
        }
    }
}
