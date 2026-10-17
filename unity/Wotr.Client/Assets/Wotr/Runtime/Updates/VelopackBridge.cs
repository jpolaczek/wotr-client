using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Velopack;
using Velopack.Locators;

namespace Wotr.Client.Updates
{
    // D13: Velopack only installs. Stage puts the verified package into Velopack's packages folder and starts its
    // helper (UpdateMac / Update.exe) to apply it once this process exits; the caller then quits through
    // Application.Quit, which Velopack's Unity Mono sample requires. A repair applies the running version again:
    // Velopack skips a package file it already has without checking it, so the cached copy goes first.
    public sealed class VelopackBridge : IUpdateInstaller
    {
        private readonly IVelopackLocator locator;

        public VelopackBridge(IVelopackLocator locator)
        {
            this.locator = locator ?? throw new InvalidOperationException("This copy is not an installation and cannot update itself.");
        }

        public async Task Stage(string package, bool repair, CancellationToken cancellation)
        {
            var asset = await VelopackAsset.FromNupkgGenerateChecksumAsync(package);
            var manager = new UpdateManager(new VerifiedFileSource(package, asset),
                new UpdateOptions { AllowVersionDowngrade = true }, locator);
            if (repair)
                ForgetCachedPackage(asset);
            await manager.DownloadUpdatesAsync(new UpdateInfo(asset, false, null, Array.Empty<VelopackAsset>()), null, cancellation);
            manager.WaitExitThenApplyUpdates(asset, false, true, Array.Empty<string>());
        }

        private void ForgetCachedPackage(VelopackAsset asset)
        {
            var cached = Path.Combine(locator.PackagesDir, asset.FileName);
            if (File.Exists(cached))
                File.Delete(cached);
        }
    }
}
