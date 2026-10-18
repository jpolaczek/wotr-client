using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Wotr.Client.Updates;

namespace Wotr.Client
{
    // DIST-07: binds the update coordinator to the interface — the notice over every screen, the required-update
    // overlay and the Updates row in Settings — and to the safe exit. Restart and update and Decline and quit save
    // the open game first (ClientFlow.SaveForExit; an error stays on screen with Retry), then the package is staged
    // and the game quits so the installer can replace it. Started only in a release build outside the Editor; a copy
    // that the installer did not install shows its version and points to the game page instead of downloading.
    public sealed class UpdateController : IDisposable
    {
        private readonly UpdateCoordinator coordinator;
        private readonly InterfaceController ui;
        private readonly Func<bool, Task<string>> save;
        private readonly Func<Task> close;
        private readonly Action quit;
        private readonly Action openGamePage;
        private readonly IDisposable owned;
        private readonly string ownVersion;
        private DateTimeOffset nextRender;
        private bool exiting;
        private bool retrySave;
        private string exitError = "";
        private bool disposed;

        public UpdateNotice Notice { get; } = new UpdateNotice();
        public UpdateRequiredOverlay Overlay { get; } = new UpdateRequiredOverlay();
        public UpdateSettingsSection Section { get; }
        public UpdateCoordinator Coordinator => coordinator;

        public UpdateController(UpdateCoordinator coordinator, InterfaceController ui, Func<bool, Task<string>> save, Func<Task> close,
            Action quit, Action openGamePage, Action<string> openNotes, string installedVersion, IDisposable owned = null)
        {
            this.coordinator = coordinator;
            this.ui = ui;
            this.save = save;
            this.close = close;
            this.quit = quit;
            this.openGamePage = openGamePage;
            this.owned = owned;
            ownVersion = coordinator.Identity.Version;
            Section = new UpdateSettingsSection(coordinator.Identity, installedVersion);
            ui.Settings.AddSection(Section.Root);
            ui.AddLayer(Notice.Root);
            ui.AddLayer(Overlay.Root);
            Notice.Accept += coordinator.Accept;
            Notice.Later += coordinator.Later;
            Notice.Cancel += coordinator.Cancel;
            Notice.Install += Restart;
            Notice.Retry += Check;
            Notice.GamePage += openGamePage;
            Notice.WhatsNew += openNotes;
            Notice.Close += coordinator.DismissArrival;
            Overlay.Install += Restart;
            Overlay.GamePage += openGamePage;
            Overlay.Retry += Check;
            Overlay.Decline += DeclineAndQuit;
            Section.Check += Check;
            Section.Accept += coordinator.Accept;
            Section.Cancel += coordinator.Cancel;
            Section.Install += () =>
            {
                ui.Settings.Close();
                Restart();
            };
            Section.GamePage += openGamePage;
            Section.Repair += coordinator.Repair;
            coordinator.Changed += Render;
            GameApi.VersionRefused += coordinator.Refused;
            Render(coordinator.Snapshot);
        }

        public static string Folder => Path.Combine(Application.persistentDataPath, "updates");

        // The game's own updater: the server from the build's configuration, the pinned publisher key and downloads
        // origin, Velopack only in an installed copy.
        public static UpdateController Start(InterfaceController ui, ClientFlow flow)
        {
            var identity = ClientIdentity.Current;
            if (Application.isEditor || identity.Development)
                return null;
            var catalog = new ReleaseCatalogClient(new GameApi(ServerConfiguration.Address));
            var installer = ClientInstallation.CanUpdateItself ? new VelopackBridge(ClientInstallation.CurrentLocator) : null;
            var random = new System.Random();
            var coordinator = new UpdateCoordinator(identity, catalog,
                new PackageDownloader(Folder, $"WotR/{identity.Version} ({identity.Platform})"), new UpdateStore(Folder),
                ReleaseTrust.Pinned, installer, new ServerClock(), () => TimeSpan.FromSeconds(random.NextDouble() * 600));
            var gamePage = ServerConfiguration.Address.TrimEnd('/') + "/";
            return new UpdateController(coordinator, ui, flow.SaveForExit, flow.CloseForExit, ApplicationQuit.Exit,
                () => Application.OpenURL(gamePage), version => Application.OpenURL(UpdateArrival.NotesUrl(gamePage, version)),
                ClientInstallation.InstalledVersion, catalog);
        }

        // Every frame from ClientBootstrap: the coordinator's clock, and the countdown once a second.
        public void Tick()
        {
            if (disposed)
                return;
            coordinator.Tick();
            if (coordinator.Snapshot.Mode == UpdateMode.RequiredSoon && coordinator.Now >= nextRender)
                Render(coordinator.Snapshot);
        }

        public void Check() => _ = coordinator.Check();

        public void Restart() => _ = Exit(true);

        public void DeclineAndQuit() => _ = Exit(false);

        private async Task Exit(bool install)
        {
            if (exiting || disposed)
                return;
            exiting = true;
            exitError = "";
            Render(coordinator.Snapshot);
            try
            {
                var error = await save(retrySave);
                if (error.Length > 0)
                {
                    retrySave = true;
                    exitError = "Your game could not be saved: " + error + " Choose the button again to retry.";
                    return;
                }
                retrySave = false;
                if (install && !await coordinator.Install())
                    return;
                await close();
                quit();
            }
            catch (Exception error)
            {
                retrySave = true;
                exitError = "Your game could not be saved: " + error.Message;
            }
            finally
            {
                exiting = false;
                if (!disposed)
                    Render(coordinator.Snapshot);
            }
        }

        private void Render(UpdateSnapshot update)
        {
            if (disposed)
                return;
            var now = coordinator.Now;
            nextRender = now.AddSeconds(1);
            Notice.Render(update, now, exitError);
            Overlay.Render(update, ownVersion, exiting, exitError);
            Section.Render(update, ownVersion, exiting, exitError);
            ui.Hold(Overlay.IsOpen);
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            GameApi.VersionRefused -= coordinator.Refused;
            coordinator.Changed -= Render;
            coordinator.Dispose();
            owned?.Dispose();
        }
    }
}
