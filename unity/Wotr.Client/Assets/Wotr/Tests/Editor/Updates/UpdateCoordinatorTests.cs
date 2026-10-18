using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client.Updates;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    // DIST-07 (DS15/DS17/DS22/DS23, U8): the updater's states against a signed catalog, a downloads host that counts
    // package bytes and an installer that only records what it was asked to stage.
    public class UpdateCoordinatorTests
    {
        private static readonly DateTimeOffset Start = new DateTimeOffset(2026, 10, 20, 18, 0, 0, TimeSpan.Zero);
        private string folder;
        private TestReleases releases;
        private ReleaseHost host;
        private Catalog catalog;
        private Installer installer;
        private DateTimeOffset local;
        private readonly List<UpdateCoordinator> coordinators = new List<UpdateCoordinator>();

        [SetUp]
        public void Create()
        {
            folder = Path.Combine(Path.GetTempPath(), "wotr-updater-" + Guid.NewGuid().ToString("N"));
            releases = new TestReleases();
            host = new ReleaseHost();
            catalog = new Catalog(() => local);
            installer = new Installer();
            local = Start;
        }

        [TearDown]
        public void Remove()
        {
            foreach (var coordinator in coordinators)
                coordinator.Dispose();
            coordinators.Clear();
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        [Test]
        public async Task OptionalReleaseShowsSizeAndNotesButNoPackageByteBeforeConsent()
        {
            var package = Publish("0.9.1");
            catalog.View = View("0.9.1");
            var updater = Updater();
            await updater.Check();
            await Until(() => updater.Snapshot.Status == UpdateStatus.Available);
            Assert.AreEqual(UpdateMode.Optional, updater.Snapshot.Mode);
            Assert.AreEqual(package.Length, updater.Snapshot.Manifest.Package.Size);
            Assert.AreEqual("Notes.", updater.Snapshot.Manifest.Notes);
            updater.Later();
            Assert.IsTrue(updater.Snapshot.Dismissed);
            await updater.Check();
            Assert.AreEqual(UpdateStatus.Available, updater.Snapshot.Status);
            Assert.IsTrue(updater.Snapshot.Dismissed);
            Assert.IsFalse(updater.Snapshot.Blocked);
            Assert.AreEqual(0, host.PackageBytes);
        }

        [Test]
        public async Task DownloadUpdateVerifiesThePackageAndInstallsOnlyOnRestart()
        {
            var package = Publish("0.9.1");
            catalog.View = View("0.9.1");
            var updater = Updater();
            await updater.Check();
            await Until(() => updater.Snapshot.Status == UpdateStatus.Available);
            updater.Accept();
            await Until(() => updater.Snapshot.Status == UpdateStatus.Ready);
            Assert.AreEqual(package.Length, host.PackageBytes);
            Assert.IsEmpty(installer.Staged);
            var state = new UpdateStore(folder).Load();
            Assert.AreEqual("0.9.1", state.ReadyVersion);
            CollectionAssert.AreEqual(package, File.ReadAllBytes(state.ReadyPackage));
            Assert.IsTrue(await updater.Install());
            Assert.AreEqual(1, installer.Staged.Count);
            Assert.AreEqual((state.ReadyPackage, false), installer.Staged[0]);
            Assert.AreEqual("0.9.1", installer.Releases[0].Version, "SEC-01: the installer gets the signed version.");
            Assert.AreEqual(state.ReadySha256, installer.Releases[0].Sha256);
            Assert.AreEqual(UpdateStatus.Applying, updater.Snapshot.Status);
        }

        [Test]
        public async Task RequiredReleaseDownloadsWithoutAskingAndBlocksNewMoves()
        {
            var package = Publish("0.9.1");
            catalog.View = View("0.9.1", minimum: "0.9.1", status: ClientCompatibilityStatus.ClientUpdateRequired);
            var updater = Updater();
            await updater.Check();
            Assert.IsTrue(updater.Snapshot.Blocked);
            await Until(() => updater.Snapshot.Status == UpdateStatus.Ready);
            Assert.AreEqual(UpdateMode.RequiredNow, updater.Snapshot.Mode);
            Assert.AreEqual(package.Length, host.PackageBytes);
            Assert.IsEmpty(installer.Staged);
        }

        [Test]
        public async Task ScheduledMinimumDownloadsAfterTheDelayAndBlocksAtTheDeadlineEvenOffline()
        {
            Publish("0.9.1");
            catalog.View = View("0.9.1", scheduled: "0.9.1");
            var updater = Updater();
            await updater.Check();
            Assert.AreEqual(UpdateStatus.Waiting, updater.Snapshot.Status);
            local = Start.AddMinutes(4);
            updater.Tick();
            Assert.AreEqual(UpdateStatus.Waiting, updater.Snapshot.Status);
            Assert.AreEqual(0, host.PackageBytes);
            local = Start.AddMinutes(5);
            updater.Tick();
            await Until(() => updater.Snapshot.Status == UpdateStatus.Ready);
            Assert.IsFalse(updater.Snapshot.Blocked);
            catalog.Fail = true;
            local = Start.AddHours(2).AddSeconds(1);
            updater.Tick();
            Assert.IsTrue(updater.Snapshot.Blocked);
            Assert.AreEqual(UpdateStatus.Ready, updater.Snapshot.Status);
        }

        [Test]
        public async Task CountdownFollowsServerTimeNotTheLocalClock()
        {
            catalog.View = View("0.9.1", scheduled: "0.9.1");
            catalog.ServerOffset = TimeSpan.FromHours(1);
            var updater = Updater();
            await updater.Check();
            Assert.AreEqual(Start.AddHours(1), updater.Now);
            Assert.IsFalse(updater.Snapshot.Blocked);
            catalog.Fail = true;
            local = Start.AddHours(1).AddSeconds(1);
            updater.Tick();
            Assert.IsTrue(updater.Snapshot.Blocked, "The server's clock passed the deadline; the local one did not.");
        }

        [Test]
        public async Task ANewTargetDropsTheConsentAndTheReadyPackage()
        {
            Publish("0.9.1");
            catalog.View = View("0.9.1");
            var updater = Updater();
            await updater.Check();
            await Until(() => updater.Snapshot.Status == UpdateStatus.Available);
            updater.Accept();
            await Until(() => updater.Snapshot.Status == UpdateStatus.Ready);
            var downloaded = host.PackageBytes;
            Publish("0.9.2");
            catalog.View = View("0.9.2");
            await updater.Check();
            await Until(() => updater.Snapshot.Status == UpdateStatus.Available && updater.Snapshot.Version == "0.9.2");
            var state = new UpdateStore(folder).Load();
            Assert.AreEqual("", state.ConsentedVersion);
            Assert.AreEqual("", state.ReadyVersion);
            Assert.IsFalse(Directory.Exists(Path.Combine(folder, "0.9.1")));
            Assert.AreEqual(downloaded, host.PackageBytes, "Consent for 0.9.1 does not download 0.9.2.");
        }

        [Test]
        public async Task AWithdrawnReleaseDropsItsDownload()
        {
            Publish("0.9.1");
            catalog.View = View("0.9.1", minimum: "0.9.1", status: ClientCompatibilityStatus.ClientUpdateRequired);
            var updater = Updater();
            await updater.Check();
            await Until(() => updater.Snapshot.Status == UpdateStatus.Ready);
            catalog.View = View(null);
            await updater.Check();
            Assert.AreEqual(UpdateStatus.Idle, updater.Snapshot.Status);
            Assert.AreEqual("", new UpdateStore(folder).Load().ReadyVersion);
            Assert.IsFalse(Directory.Exists(Path.Combine(folder, "0.9.1")));
        }

        [Test]
        public async Task ASignatureOfAnotherKeyIsRefusedBeforeAnyPackageByte()
        {
            Publish("0.9.1", signer: new TestReleases());
            catalog.View = View("0.9.1", minimum: "0.9.1", status: ClientCompatibilityStatus.ClientUpdateRequired);
            var updater = Updater();
            await updater.Check();
            await Until(() => updater.Snapshot.Status == UpdateStatus.Failed);
            Assert.AreEqual(0, host.PackageBytes);
            Assert.IsTrue(updater.Snapshot.Requested);
        }

        [Test]
        public async Task AManifestOutsideTheDownloadsOriginIsNotFetched()
        {
            Publish("0.9.1");
            var view = View("0.9.1");
            view.Latest.ManifestUrl = "https://elsewhere.example/releases/0.9.1/macos-universal/manifest.json";
            catalog.View = view;
            var updater = Updater();
            await updater.Check();
            await Until(() => updater.Snapshot.Status == UpdateStatus.Failed);
            Assert.AreEqual(0, host.Requests);
        }

        [Test]
        public async Task CancelKeepsTheGameAndWithdrawsTheConsent()
        {
            Publish("0.9.1");
            catalog.View = View("0.9.1");
            host.Hold = true;
            var updater = Updater();
            await updater.Check();
            await Until(() => updater.Snapshot.Status == UpdateStatus.Available);
            updater.Accept();
            await Until(() => host.Held);
            Assert.AreEqual(UpdateStatus.Downloading, updater.Snapshot.Status);
            updater.Cancel();
            await Until(() => updater.Snapshot.Status == UpdateStatus.Available);
            Assert.AreEqual("", new UpdateStore(folder).Load().ConsentedVersion);
            host.Hold = false;
            await updater.Check();
            Assert.AreEqual(UpdateStatus.Available, updater.Snapshot.Status);
            Assert.AreEqual(0, host.PackageBytes);
        }

        [Test]
        public async Task RequiredDownloadCannotBeCancelled()
        {
            Publish("0.9.1");
            catalog.View = View("0.9.1", minimum: "0.9.1", status: ClientCompatibilityStatus.ClientUpdateRequired);
            host.Hold = true;
            var updater = Updater();
            await updater.Check();
            await Until(() => host.Held);
            updater.Cancel();
            await Task.Delay(50);
            Assert.AreEqual(UpdateStatus.Downloading, updater.Snapshot.Status);
        }

        [Test]
        public async Task RepairDownloadsTheRunningVersionAndStaysReadyUntilARequiredRelease()
        {
            var package = Publish("0.9.0");
            catalog.View = View(null);
            var updater = Updater();
            await updater.Check();
            Assert.AreEqual(UpdateMode.None, updater.Snapshot.Mode);
            updater.Repair();
            await Until(() => updater.Snapshot.Status == UpdateStatus.Ready);
            Assert.IsTrue(updater.Snapshot.Repair);
            Assert.AreEqual(package.Length, host.PackageBytes);
            await updater.Check();
            Assert.AreEqual(UpdateStatus.Ready, updater.Snapshot.Status);
            Assert.IsTrue(updater.Snapshot.Repair);
            Assert.IsTrue(await updater.Install());
            Assert.IsTrue(installer.Staged[0].Repair);
            // The repaired game starts with the same version: no repair is offered again.
            updater.Dispose();
            var restarted = Updater();
            await restarted.Check();
            Assert.AreNotEqual(UpdateStatus.Ready, restarted.Snapshot.Status);
            Assert.IsFalse(restarted.Snapshot.Repair);
            Assert.AreEqual("", new UpdateStore(folder).Load().ReadyVersion);
            Assert.IsEmpty(Directory.GetDirectories(folder));
        }

        [Test]
        public async Task AWithdrawnVersionCannotBeRepaired()
        {
            catalog.View = View("0.9.1", installed: null);
            Publish("0.9.1");
            var updater = Updater();
            await updater.Check();
            await Until(() => updater.Snapshot.Status == UpdateStatus.Available);
            updater.Repair();
            Assert.AreEqual(UpdateStatus.Available, updater.Snapshot.Status);
            Assert.AreEqual(0, host.PackageBytes);
        }

        [Test]
        public async Task ADamagedReadyPackageIsNotInstalled()
        {
            Publish("0.9.1");
            catalog.View = View("0.9.1", minimum: "0.9.1", status: ClientCompatibilityStatus.ClientUpdateRequired);
            var updater = Updater();
            await updater.Check();
            await Until(() => updater.Snapshot.Status == UpdateStatus.Ready);
            var path = new UpdateStore(folder).Load().ReadyPackage;
            File.WriteAllText(path, "damaged");
            Assert.IsFalse(await updater.Install());
            Assert.AreEqual(UpdateStatus.Failed, updater.Snapshot.Status);
            Assert.IsEmpty(installer.Staged);
            Assert.AreEqual("", new UpdateStore(folder).Load().ReadyVersion);
        }

        [Test]
        public async Task AFailedStartKeepsThePackageReady()
        {
            Publish("0.9.1");
            catalog.View = View("0.9.1", minimum: "0.9.1", status: ClientCompatibilityStatus.ClientUpdateRequired);
            installer.Failure = new InvalidOperationException("helper missing");
            var updater = Updater();
            await updater.Check();
            await Until(() => updater.Snapshot.Status == UpdateStatus.Ready);
            Assert.IsFalse(await updater.Install());
            Assert.AreEqual(UpdateStatus.Ready, updater.Snapshot.Status);
            StringAssert.Contains("helper missing", updater.Snapshot.Message);
        }

        [Test]
        public async Task TheNextStartOffersTheDownloadedPackageAgain()
        {
            var package = Publish("0.9.1");
            catalog.View = View("0.9.1", minimum: "0.9.1", status: ClientCompatibilityStatus.ClientUpdateRequired);
            var first = Updater();
            await first.Check();
            await Until(() => first.Snapshot.Status == UpdateStatus.Ready);
            first.Dispose();
            var second = Updater();
            await second.Check();
            Assert.AreEqual(UpdateStatus.Ready, second.Snapshot.Status);
            Assert.IsTrue(second.Snapshot.Blocked);
            Assert.AreEqual(package.Length, host.PackageBytes);
        }

        [Test]
        public async Task ARefusalBlocksAtOnceAndTheCatalogLiftsIt()
        {
            catalog.View = View(null);
            catalog.Fail = true;
            var updater = Updater();
            updater.Refused(ClientCompatibility.ClientUpdateRequired);
            Assert.IsTrue(updater.Snapshot.Blocked);
            await Until(() => catalog.Reads > 0);
            catalog.Fail = false;
            await updater.Check();
            await updater.Check();
            Assert.IsFalse(updater.Snapshot.Blocked);
            Assert.AreEqual(UpdateMode.None, updater.Snapshot.Mode);
        }

        [Test]
        public async Task AnUnreachableServerChangesNothing()
        {
            catalog.Fail = true;
            var updater = Updater();
            await updater.Check();
            Assert.AreEqual(UpdateStatus.Idle, updater.Snapshot.Status);
            Assert.IsNull(updater.Snapshot.Plan);
            Assert.IsFalse(updater.Snapshot.Blocked);
            StringAssert.Contains("Could not reach", updater.Snapshot.Message);
        }

        [Test]
        public async Task ACopyThatCannotUpdateItselfPointsToTheGamePage()
        {
            Publish("0.9.1");
            catalog.View = View("0.9.1", minimum: "0.9.1", status: ClientCompatibilityStatus.ClientUpdateRequired);
            var updater = Updater(withInstaller: false);
            await updater.Check();
            Assert.AreEqual(UpdateStatus.Available, updater.Snapshot.Status);
            Assert.IsFalse(updater.Snapshot.CanInstall);
            updater.Accept();
            Assert.AreEqual(0, host.Requests);
        }

        [Test]
        public async Task AnOlderServerDownloadsNothing()
        {
            Publish("0.9.1");
            catalog.View = View("0.9.1", status: ClientCompatibilityStatus.ServerUpdateRequired);
            var updater = Updater();
            await updater.Check();
            Assert.AreEqual(UpdateMode.ServerBehind, updater.Snapshot.Mode);
            Assert.AreEqual(UpdateStatus.Idle, updater.Snapshot.Status);
            Assert.IsFalse(updater.Snapshot.Blocked);
            Assert.AreEqual(0, host.Requests);
        }

        // DIST-15: a version newer than the one that last ran here is announced once; a first installation is not.
        [Test]
        public void ANewerVersionThanLastTimeIsAnnouncedOnceButAFirstInstallationIsNot()
        {
            Assert.AreEqual("", Updater().Snapshot.UpdatedTo, "a first installation");
            var store = new UpdateStore(folder);
            var state = store.Load();
            Assert.AreEqual("0.9.0", state.LastRunVersion);
            state.LastRunVersion = "0.8.5";
            store.Save(state);

            var updated = Updater();
            Assert.AreEqual("0.9.0", updated.Snapshot.UpdatedTo);
            var notice = new UpdateNotice();
            notice.Render(updated.Snapshot, Start, "");
            Assert.IsTrue(notice.IsShown);
            Assert.AreEqual("Updated to version 0.9.0.", notice.Bar.Q<Label>("update-notice-text").text);
            Assert.AreEqual(DisplayStyle.Flex, notice.Bar.Q<Button>("update-notice-notes").style.display.value);
            Assert.AreEqual("Close", notice.Bar.Q<Button>("update-notice-first").text);

            var changes = 0;
            updated.Changed += _ => changes++;
            updated.DismissArrival();
            Assert.AreEqual("", updated.Snapshot.UpdatedTo);
            Assert.AreEqual(1, changes);
            notice.Render(updated.Snapshot, Start, "");
            Assert.IsFalse(notice.IsShown);
            Assert.AreEqual("", Updater().Snapshot.UpdatedTo, "once per version");
        }

        [Test]
        public async Task AnOfferedReleaseHasWhatsNewAndARepairHasNot()
        {
            Publish("0.9.1");
            catalog.View = View("0.9.1");
            var updater = Updater();
            await updater.Check();
            await Until(() => updater.Snapshot.Status == UpdateStatus.Available);
            var notice = new UpdateNotice();
            notice.Render(updater.Snapshot, Start, "");
            Assert.AreEqual(DisplayStyle.Flex, notice.Bar.Q<Button>("update-notice-notes").style.display.value);
            Assert.AreEqual("What's new", notice.Bar.Q<Button>("update-notice-notes").text);
            Assert.AreEqual("https://wotr.example.test/updates#v0.9.1", UpdateArrival.NotesUrl("https://wotr.example.test/", "0.9.1"));

            updater.Later();
            notice.Render(updater.Snapshot, Start, "");
            Assert.IsFalse(notice.IsShown);
            Assert.AreEqual(DisplayStyle.None, notice.Bar.Q<Button>("update-notice-notes").style.display.value);
        }

        [Test]
        public void OnlyANewerVersionCountsAsAnArrival()
        {
            Assert.AreEqual("0.9.0-rc.10", UpdateArrival.Version("0.9.0-rc.9", "0.9.0-rc.10"));
            Assert.AreEqual("", UpdateArrival.Version("", "0.9.0-rc.10"));
            Assert.AreEqual("", UpdateArrival.Version("0.9.0-rc.10", "0.9.0-rc.10"));
            Assert.AreEqual("", UpdateArrival.Version("0.9.0", "0.9.0-rc.10"));
            Assert.AreEqual("", UpdateArrival.Version("not a version", "0.9.0-rc.10"));
        }

        private UpdateCoordinator Updater(bool withInstaller = true)
        {
            var identity = new ClientBuildInfo { Version = "0.9.0", Platform = ClientPlatforms.MacOS, ProtocolVersion = 2 };
            var coordinator = new UpdateCoordinator(identity, catalog, new PackageDownloader(folder, "WotR test", host, _ => long.MaxValue),
                new UpdateStore(folder), releases.Trust, withInstaller ? installer : null, new ServerClock(() => local),
                () => TimeSpan.FromMinutes(5));
            coordinators.Add(coordinator);
            return coordinator;
        }

        private byte[] Publish(string version, TestReleases signer = null)
        {
            var package = new byte[30000 + version.Length];
            new Random(version.Length).NextBytes(package);
            var manifest = TestReleases.Manifest(version, package: package);
            var bytes = TestReleases.Bytes(manifest);
            host.Put(Folder(version) + "manifest.json", bytes);
            host.Put(Folder(version) + "manifest.json.sig", (signer ?? releases).Sign(bytes, releases.Trust.KeyId));
            host.Put(manifest.Package.Url, package);
            return package;
        }

        private static string Folder(string version) => TestReleases.Origin + $"releases/{version}/{ClientPlatforms.MacOS}/";

        private static ClientReleaseOffer Offer(string version) => version == null ? null : new ClientReleaseOffer
        {
            Version = version,
            ProtocolVersion = 2,
            ManifestUrl = Folder(version) + "manifest.json",
            SignatureUrl = Folder(version) + "manifest.json.sig"
        };

        private static ClientReleaseView View(string latest, string minimum = "", string scheduled = null,
            ClientCompatibilityStatus status = ClientCompatibilityStatus.Compatible, string installed = "0.9.0") => new ClientReleaseView
        {
            Platform = ClientPlatforms.MacOS,
            Latest = Offer(latest),
            MinimumVersion = minimum,
            Status = status,
            ScheduledMinimum = scheduled == null ? null : new ClientScheduledMinimum { Version = scheduled, EffectiveAt = Start.AddHours(2) },
            Installed = Offer(installed)
        };

        private static async Task Until(Func<bool> condition)
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (!condition())
            {
                if (DateTime.UtcNow > deadline)
                    Assert.Fail("The updater did not reach the expected state.");
                await Task.Delay(10);
            }
        }

        private sealed class Catalog : IReleaseCatalog
        {
            private readonly Func<DateTimeOffset> local;

            public Catalog(Func<DateTimeOffset> local) => this.local = local;

            public ClientReleaseView View { get; set; }
            public bool Fail { get; set; }
            public TimeSpan ServerOffset { get; set; }
            public int Reads { get; private set; }

            public Task<ClientReleaseView> Read(CancellationToken cancellation)
            {
                Reads++;
                if (Fail)
                    return Task.FromException<ClientReleaseView>(new HttpRequestException("unreachable"));
                View.ServerTime = local() + ServerOffset;
                return Task.FromResult(View);
            }
        }

        private sealed class Installer : IUpdateInstaller
        {
            public List<(string Package, bool Repair)> Staged { get; } = new List<(string, bool)>();
            public List<StagedRelease> Releases { get; } = new List<StagedRelease>();
            public Exception Failure { get; set; }

            public Task Stage(StagedRelease release, CancellationToken cancellation)
            {
                if (Failure != null)
                    return Task.FromException(Failure);
                Staged.Add((release.Package, release.Repair));
                Releases.Add(release);
                return Task.CompletedTask;
            }
        }

        // The downloads host: whole files, a strong ETag, and package bytes counted as they are sent.
        private sealed class ReleaseHost : HttpMessageHandler
        {
            private readonly Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
            private long packageBytes;
            private int requests;

            public long PackageBytes => Interlocked.Read(ref packageBytes);
            public int Requests => requests;
            public bool Hold { get; set; }
            public bool Held { get; private set; }

            public void Put(string url, byte[] bytes) => files[url] = bytes;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
            {
                Interlocked.Increment(ref requests);
                var url = request.RequestUri.AbsoluteUri;
                var package = url.EndsWith(".nupkg", StringComparison.Ordinal);
                if (package && Hold)
                {
                    Held = true;
                    await Task.Delay(Timeout.Infinite, cancellation);
                }
                if (!files.TryGetValue(url, out var bytes))
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                if (package)
                    Interlocked.Add(ref packageBytes, bytes.Length);
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
                response.Headers.ETag = new EntityTagHeaderValue("\"test\"");
                return response;
            }
        }
    }
}
