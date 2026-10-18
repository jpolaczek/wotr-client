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
    // SEC-01: InstallGuard refuses an older (or, outside a repair, equal) version and a package whose own version
    // differs from its manifest, and checks the copy Velopack will apply against the signed SHA-256; only a repair may
    // install a version that is not newer.
    public sealed class VelopackBridge : IUpdateInstaller
    {
        private readonly IVelopackLocator locator;

        public VelopackBridge(IVelopackLocator locator)
        {
            this.locator = locator ?? throw new InvalidOperationException("This copy is not an installation and cannot update itself.");
        }

        public async Task Stage(StagedRelease release, CancellationToken cancellation)
        {
            var asset = await VelopackAsset.FromNupkgGenerateChecksumAsync(release.Package);
            InstallGuard.CheckVersions(locator.CurrentlyInstalledVersion?.ToString(), release, asset.Version?.ToString());
            var manager = new UpdateManager(new VerifiedFileSource(release.Package, asset),
                new UpdateOptions { AllowVersionDowngrade = release.Repair }, locator);
            if (release.Repair)
                ForgetCachedPackage(asset);
            await manager.DownloadUpdatesAsync(new UpdateInfo(asset, false, null, Array.Empty<VelopackAsset>()), null, cancellation);
            CheckStagedCopy(asset, release.Sha256);
            manager.WaitExitThenApplyUpdates(asset, false, true, Array.Empty<string>());
        }

        private string CachedPath(VelopackAsset asset) => Path.Combine(locator.PackagesDir, asset.FileName);

        private void ForgetCachedPackage(VelopackAsset asset)
        {
            var cached = CachedPath(asset);
            if (File.Exists(cached))
                File.Delete(cached);
        }

        // The file Velopack's helper applies after the game quits is the copy in its packages folder.
        private void CheckStagedCopy(VelopackAsset asset, string sha256)
        {
            try
            {
                InstallGuard.CheckFile(CachedPath(asset), sha256);
            }
            catch (UpdateRejected)
            {
                ForgetCachedPackage(asset);
                throw;
            }
        }
    }
}
