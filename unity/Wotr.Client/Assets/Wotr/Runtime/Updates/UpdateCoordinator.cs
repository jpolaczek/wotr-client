using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wotr.Contracts;

namespace Wotr.Client.Updates
{
    public interface IReleaseCatalog
    {
        Task<ClientReleaseView> Read(CancellationToken cancellation);
    }

    public interface IUpdateInstaller
    {
        // Stages the verified package and starts the helper that applies it after this process exits.
        Task Stage(StagedRelease release, CancellationToken cancellation);
    }

    public enum UpdateStatus
    {
        Idle,
        Checking,
        // A release waiting for the player: Download update / Later for an optional one (DS15), or the game page
        // when this copy cannot update itself.
        Available,
        // Required from a future time: the download starts after a random delay of up to ten minutes (D13).
        Waiting,
        Downloading,
        Ready,
        Applying,
        Failed
    }

    // What the interface shows. Version is the release being offered, downloaded or ready; Manifest its verified
    // manifest (size and notes) once read. Requested marks work the player asked for or a required release, whose
    // failure the game announces. Blocked means new online moves wait: the server refuses this version, or the
    // deadline of a scheduled minimum passed (also offline).
    public sealed class UpdateSnapshot
    {
        public UpdateStatus Status { get; internal set; }
        public UpdatePlan Plan { get; internal set; }
        public string Version { get; internal set; } = "";
        public ClientReleaseManifest Manifest { get; internal set; }
        public DownloadProgress Progress { get; internal set; }
        public string Message { get; internal set; } = "";
        public bool Repair { get; internal set; }
        public bool Requested { get; internal set; }
        public bool Dismissed { get; internal set; }
        public bool CanInstall { get; internal set; }
        public bool Blocked { get; internal set; }
        // DIST-15: the version this copy was just updated to, until the player closes its notice.
        public string UpdatedTo { get; internal set; } = "";

        public UpdateMode Mode => Plan?.Mode ?? UpdateMode.None;
        public bool Required => Mode == UpdateMode.RequiredNow || Mode == UpdateMode.RequiredSoon;
    }

    // DIST-07 states (DISTRIBUTION_PLAN): Idle → Checking → Available → Downloading → Ready → Applying, with Failed and
    // a way back from each. The game downloads and verifies; the installer only installs. Required releases download
    // without asking, optional ones after Download update, a repair after the player confirms in Settings; the
    // player's repair waits only for a required release. Consent and downloads belong to one release: a new target
    // drops both, and a download that ends after the target changed gives way to the new one. Tick drives the
    // clock: checks every 15 minutes, the delayed download of a scheduled minimum and its deadline.
    public sealed class UpdateCoordinator : IDisposable
    {
        public static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan RetryInterval = TimeSpan.FromMinutes(1);

        private readonly ClientBuildInfo identity;
        private readonly IReleaseCatalog catalog;
        private readonly PackageDownloader downloader;
        private readonly UpdateStore store;
        private readonly ReleaseTrust trust;
        private readonly IUpdateInstaller installer;
        private readonly ServerClock clock;
        private readonly Func<TimeSpan> jitter;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly UpdateSnapshot snapshot = new UpdateSnapshot();
        private CancellationTokenSource transfer;
        private UpdateState state;
        private bool refused;
        private bool checking;
        private bool transferring;
        private DateTimeOffset nextCheck;
        private DateTimeOffset? downloadAt;
        private string dismissedVersion = "";

        public UpdateCoordinator(ClientBuildInfo identity, IReleaseCatalog catalog, PackageDownloader downloader, UpdateStore store,
            ReleaseTrust trust, IUpdateInstaller installer, ServerClock clock, Func<TimeSpan> jitter)
        {
            this.identity = identity;
            this.catalog = catalog;
            this.downloader = downloader;
            this.store = store;
            this.trust = trust;
            this.installer = installer;
            this.clock = clock;
            this.jitter = jitter;
            state = store.Load();
            snapshot.CanInstall = installer != null;
            NoteArrival();
        }

        public event Action<UpdateSnapshot> Changed;

        public UpdateSnapshot Snapshot => snapshot;

        // Server time for countdowns: the catalog's serverTime corrects a wrong local clock.
        public DateTimeOffset Now => clock.Now;

        public ClientBuildInfo Identity => identity;

        // Periodic work on the main thread; cheap when nothing is due.
        public void Tick()
        {
            var now = clock.Now;
            if (!checking && now >= nextCheck)
                _ = Check();
            if (downloadAt.HasValue && now >= downloadAt.Value && snapshot.Status == UpdateStatus.Waiting)
            {
                downloadAt = null;
                _ = Download(snapshot.Plan.Offer, false);
            }
            var blocked = Blocked(now);
            if (blocked == snapshot.Blocked)
                return;
            snapshot.Blocked = blocked;
            Publish();
            if (blocked)
                _ = Check();
        }

        // A 409 from any request: the server refuses this version (new moves wait until the next catalog answer
        // says otherwise) or is older than it.
        public void Refused(string code)
        {
            if (code == ClientCompatibility.ClientUpdateRequired && !refused)
            {
                refused = true;
                snapshot.Blocked = true;
                Publish();
            }
            _ = Check();
        }

        public async Task Check()
        {
            if (checking)
                return;
            checking = true;
            nextCheck = clock.Now + CheckInterval;
            if (snapshot.Status == UpdateStatus.Idle || snapshot.Status == UpdateStatus.Failed)
            {
                snapshot.Requested = false;
                Show(UpdateStatus.Checking);
            }
            try
            {
                var view = await catalog.Read(lifetime.Token);
                clock.Observe(view.ServerTime);
                var plan = UpdatePolicy.Plan(identity.Version, view, refused);
                // The catalog applies the same rules as the server's gate; its next answer replaces the refusal.
                refused = false;
                await Apply(plan);
            }
            catch (Exception) when (!lifetime.IsCancellationRequested)
            {
                // Without a catalog nothing changes: an unreachable server never pretends a new release exists.
                nextCheck = clock.Now + RetryInterval;
                if (snapshot.Status == UpdateStatus.Checking)
                    Show(UpdateStatus.Idle, "Could not reach the server to check for updates.");
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                checking = false;
            }
        }

        // Download update on an optional release.
        public void Accept()
        {
            var offer = snapshot.Plan?.Offer;
            if (offer == null || installer == null || transferring || snapshot.Status == UpdateStatus.Ready ||
                snapshot.Status == UpdateStatus.Applying)
                return;
            state.ConsentedVersion = offer.Version;
            store.Save(state);
            _ = Download(offer, false);
        }

        // Later: hides the optional offer of this release until the next start; a newer one shows again.
        public void Later()
        {
            dismissedVersion = snapshot.Version;
            snapshot.Dismissed = true;
            Publish();
        }

        // Cancel withdraws the consent too, so the next check does not start the download again; the part already
        // downloaded stays for a later Download update.
        public void Cancel()
        {
            if (!transferring || (snapshot.Required && !snapshot.Repair))
                return;
            if (state.ConsentedVersion.Length > 0)
            {
                state.ConsentedVersion = "";
                store.Save(state);
            }
            transfer?.Cancel();
        }

        // Settings → Repair installation: the running version's release again (DS23).
        public void Repair()
        {
            var offer = snapshot.Plan?.RepairOffer;
            if (offer == null || installer == null || transferring || snapshot.Status == UpdateStatus.Applying)
                return;
            _ = Download(offer, true);
        }

        // Restart and update, after the caller saved the local state: Velopack takes over once the game quits.
        // The package is checked once more, since it may have waited on disk since an earlier session.
        public async Task<bool> Install()
        {
            if (snapshot.Status != UpdateStatus.Ready || installer == null)
                return false;
            Show(UpdateStatus.Applying);
            try
            {
                if (!File.Exists(state.ReadyPackage) || await PackageDownloader.HashAsync(state.ReadyPackage) != state.ReadySha256)
                {
                    DropReady();
                    nextCheck = clock.Now;
                    Show(UpdateStatus.Failed, "The downloaded update was damaged and downloads again.");
                    return false;
                }
                await installer.Stage(new StagedRelease(state.ReadyPackage, state.ReadyVersion, state.ReadySha256, state.ReadyIsRepair),
                    lifetime.Token);
                // The installer holds its own copy now. A repair keeps the running version, so a ready package left
                // behind would offer the same repair again on every start (2026-10-17); an update would be dropped by
                // the new version anyway.
                DropReady();
                return true;
            }
            catch (Exception error) when (!lifetime.IsCancellationRequested)
            {
                Show(UpdateStatus.Ready, "The update could not start: " + error.Message);
                return false;
            }
        }

        // DIST-15: the player closed the notice of the version this copy was just updated to.
        public void DismissArrival()
        {
            if (snapshot.UpdatedTo.Length == 0)
                return;
            snapshot.UpdatedTo = "";
            Publish();
        }

        public void Dispose()
        {
            lifetime.Cancel();
            transfer?.Cancel();
            downloader.Dispose();
        }

        // Remembers the running version; a newer one than last time is announced once (UpdateArrival).
        private void NoteArrival()
        {
            if (state.LastRunVersion == identity.Version)
                return;
            snapshot.UpdatedTo = UpdateArrival.Version(state.LastRunVersion, identity.Version);
            state.LastRunVersion = identity.Version;
            store.Save(state);
        }

        private async Task Apply(UpdatePlan plan)
        {
            snapshot.Plan = plan;
            snapshot.Blocked = Blocked(clock.Now);
            if (transferring || snapshot.Status == UpdateStatus.Applying)
            {
                // The running download ends first and then applies the newest plan.
                Publish();
                return;
            }
            var offer = plan.Offer;
            if (state.ReadyIsRepair && state.ReadyVersion == identity.Version && File.Exists(state.ReadyPackage) &&
                !(snapshot.Required && offer != null))
            {
                Target(state.ReadyVersion, true);
                Show(UpdateStatus.Ready);
                return;
            }
            KeepOnly(offer?.Version ?? "");
            Target(offer?.Version ?? "", false);
            snapshot.Dismissed = offer != null && offer.Version == dismissedVersion;
            if (plan.Mode == UpdateMode.ServerBehind)
                Show(UpdateStatus.Idle, "The server has not been updated for this version yet.");
            else if (offer == null)
                Show(UpdateStatus.Idle, plan.Mode == UpdateMode.RequiredNow ? "No update for this platform has been published yet." : "");
            else if (state.ReadyVersion == offer.Version && File.Exists(state.ReadyPackage))
                Show(UpdateStatus.Ready);
            else if (installer == null)
                Show(UpdateStatus.Available);
            else if (plan.Mode == UpdateMode.RequiredNow || state.ConsentedVersion == offer.Version)
                _ = Download(offer, false);
            else if (plan.Mode == UpdateMode.RequiredSoon)
            {
                if (snapshot.Status != UpdateStatus.Waiting || !downloadAt.HasValue)
                    downloadAt = clock.Now + jitter();
                Show(UpdateStatus.Waiting);
            }
            else
                await Inspect(offer);
        }

        // An optional release shows its size and notes from the verified manifest; no byte of the package moves
        // before Download update.
        private async Task Inspect(ClientReleaseOffer offer)
        {
            try
            {
                if (snapshot.Manifest?.Version != offer.Version)
                    snapshot.Manifest = await Manifest(offer, lifetime.Token);
                Show(UpdateStatus.Available);
            }
            catch (UpdateRejected error)
            {
                Show(UpdateStatus.Failed, error.Message);
            }
        }

        private async Task Download(ClientReleaseOffer offer, bool repair)
        {
            if (transferring || installer == null)
                return;
            transferring = true;
            transfer?.Dispose();
            transfer = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            Target(offer.Version, repair);
            snapshot.Requested = true;
            snapshot.Progress = default;
            Show(UpdateStatus.Downloading);
            string failure = null;
            var cancelled = false;
            try
            {
                if (snapshot.Manifest?.Version != offer.Version)
                    snapshot.Manifest = await Manifest(offer, transfer.Token);
                var file = snapshot.Manifest.Package;
                store.RemoveOtherDownloads(offer.Version);
                var package = await downloader.Download(offer.Version, file, new Reporter(Report), transfer.Token);
                state.ReadyVersion = offer.Version;
                state.ReadyPackage = package;
                state.ReadySha256 = file.Sha256;
                state.ReadyIsRepair = repair;
                store.Save(state);
            }
            catch (OperationCanceledException) when (!lifetime.IsCancellationRequested)
            {
                cancelled = true;
            }
            catch (UpdateRejected error)
            {
                failure = error.Message;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                failure = "The update could not be saved: " + error.Message;
            }
            finally
            {
                transferring = false;
            }
            if (lifetime.IsCancellationRequested)
                return;
            if (failure != null)
            {
                nextCheck = clock.Now + RetryInterval;
                Show(UpdateStatus.Failed, failure);
            }
            else if (cancelled)
            {
                snapshot.Requested = false;
                Show(repair ? UpdateStatus.Idle : UpdateStatus.Available, "Download cancelled. It resumes where it stopped.");
            }
            else if (repair)
                Show(UpdateStatus.Ready);
            else
                await Apply(snapshot.Plan);
        }

        private async Task<ClientReleaseManifest> Manifest(ClientReleaseOffer offer, CancellationToken cancellation)
        {
            var folder = trust.DownloadsOrigin + $"releases/{offer.Version}/{identity.Platform}/";
            if (offer.ManifestUrl != folder + "manifest.json" || offer.SignatureUrl != folder + "manifest.json.sig")
                throw new UpdateRejected("The catalog points to a manifest outside the approved downloads origin.");
            var manifest = await downloader.FetchSmall(offer.ManifestUrl, cancellation);
            var signature = await downloader.FetchSmall(offer.SignatureUrl, cancellation);
            return ManifestVerifier.Verify(manifest, signature, trust, offer.Version, identity.Platform);
        }

        private void Report(DownloadProgress progress)
        {
            var before = Percent(snapshot.Progress);
            snapshot.Progress = progress;
            if (Percent(progress) != before)
                Publish();
        }

        private static int Percent(DownloadProgress progress) =>
            progress.Total > 0 ? (int)(progress.Received * 100 / progress.Total) : -1;

        private bool Blocked(DateTimeOffset now) =>
            refused || snapshot.Mode == UpdateMode.RequiredNow ||
            (snapshot.Mode == UpdateMode.RequiredSoon && snapshot.Plan.Deadline.HasValue && now >= snapshot.Plan.Deadline.Value);

        private void Target(string version, bool repair)
        {
            if (snapshot.Version != version || snapshot.Repair != repair)
                snapshot.Progress = default;
            snapshot.Version = version;
            snapshot.Repair = repair;
            if (snapshot.Manifest != null && snapshot.Manifest.Version != version)
                snapshot.Manifest = null;
        }

        // Consent and the ready package belong to one release; another target drops both and its files.
        private void KeepOnly(string version)
        {
            var changed = false;
            if (state.ConsentedVersion.Length > 0 && state.ConsentedVersion != version)
            {
                state.ConsentedVersion = "";
                changed = true;
            }
            if (state.ReadyVersion.Length > 0 && (state.ReadyVersion != version || state.ReadyIsRepair))
            {
                ClearReady();
                changed = true;
            }
            if (changed)
                store.Save(state);
            store.RemoveOtherDownloads(version);
        }

        private void DropReady()
        {
            ClearReady();
            store.Save(state);
            store.RemoveOtherDownloads("");
        }

        private void ClearReady()
        {
            state.ReadyVersion = "";
            state.ReadyPackage = "";
            state.ReadySha256 = "";
            state.ReadyIsRepair = false;
        }

        private void Show(UpdateStatus status, string message = "")
        {
            snapshot.Status = status;
            snapshot.Message = message;
            Publish();
        }

        private void Publish() => Changed?.Invoke(snapshot);

        // Reports in the downloader's own continuation, on the main thread in the game.
        private sealed class Reporter : IProgress<DownloadProgress>
        {
            private readonly Action<DownloadProgress> report;

            public Reporter(Action<DownloadProgress> report) => this.report = report;

            public void Report(DownloadProgress value) => report(value);
        }
    }
}
