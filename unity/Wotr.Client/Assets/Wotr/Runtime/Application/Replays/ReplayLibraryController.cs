using System;
using System.IO;
using System.Threading.Tasks;

namespace Wotr.Client
{
    // REPLAY-04: what the library page does — the list from the library, Import and Export through the system's file
    // dialogs, Delete of the local copy and Play through the flow. It needs no account and no server.
    public sealed class ReplayLibraryController : IDisposable
    {
        private readonly ReplayLibrary library;
        private readonly ReplayLibraryPage page;
        private readonly IReplayFilePicker picker;
        private readonly Action<ReplayEntry> play;
        private bool busy;
        private bool disposed;

        public ReplayLibraryController(ReplayLibrary library, ReplayLibraryPage page, IReplayFilePicker picker, Action<ReplayEntry> play)
        {
            this.library = library;
            this.page = page;
            this.picker = picker;
            this.play = play;
            page.Play += Play;
            page.Import += Import;
            page.Export += Export;
            page.Delete += Delete;
        }

        public void Refresh()
        {
            if (!disposed)
                page.Show(library.List());
        }

        private void Play(ReplayEntry entry)
        {
            if (!busy)
                play(entry);
        }

        private void Import() => Run(async () =>
        {
            var path = await picker.ChooseImport();
            if (path == null)
                return "";
            var added = await library.AddFile(path);
            return added == ReplayAdded.Saved ? "Replay imported." : "This replay is already in your library.";
        });

        private void Export(ReplayEntry entry) => Run(async () =>
        {
            var path = await picker.ChooseExport(Path.GetFileName(entry.Path));
            if (path == null)
                return "";
            await library.Export(entry, path);
            return "Replay exported to " + Path.GetFileName(path) + ".";
        });

        private void Delete(ReplayEntry entry) => Run(() =>
        {
            library.Delete(entry);
            return Task.FromResult("Replay deleted from this computer.");
        });

        // One operation at a time; its result or error shows in the page's status and the list follows the disk.
        private async void Run(Func<Task<string>> operation)
        {
            if (busy || disposed)
                return;
            busy = true;
            page.Busy(true);
            try
            {
                page.Status.text = await operation();
            }
            catch (Exception error) when (error is ReplayFormatException || error is IOException || error is UnauthorizedAccessException)
            {
                if (!disposed)
                    page.Status.text = error is ReplayFormatException ? error.Message : "The file could not be used: " + error.Message;
            }
            finally
            {
                busy = false;
                if (!disposed)
                {
                    page.Busy(false);
                    Refresh();
                }
            }
        }

        public void Dispose()
        {
            disposed = true;
            page.Play -= Play;
            page.Import -= Import;
            page.Export -= Export;
            page.Delete -= Delete;
        }
    }
}
