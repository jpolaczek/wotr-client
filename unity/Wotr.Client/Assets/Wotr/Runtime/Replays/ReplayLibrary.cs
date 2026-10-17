using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    // One replay of the library: its file and manifest, or the reason a damaged file cannot be shown.
    public sealed class ReplayEntry
    {
        public string Path { get; set; } = "";
        public ReplayManifest Manifest { get; set; }
        public string Error { get; set; } = "";
        public bool Usable => Manifest != null && Error.Length == 0;
    }

    public enum ReplayAdded { Saved, AlreadySaved }

    // REPLAY-03: the library touches no Unity object after an await, so it never waits for the main thread.
    // REPLAY-03: the replays of this installation, in <persistentDataPath>/Replays — shared by every account and tied
    // to no server. A file enters only through Add: it is written to a temporary file in the same folder, checked in
    // full and then renamed into place, so the list never shows a half-written replay. The same replay is kept once
    // (an export of the same game is "already saved"); another replay with the same identity never overwrites it.
    public sealed class ReplayLibrary
    {
        private const string Incoming = ".incoming-";
        private static readonly Regex Unsafe = new Regex("[^A-Za-z0-9-]");
        public string Folder { get; }

        public ReplayLibrary(string folder)
        {
            Folder = folder;
            Directory.CreateDirectory(folder);
            RemoveInterrupted();
        }

        public static string DefaultFolder => System.IO.Path.Combine(Application.persistentDataPath, "Replays");

        // Newest games first, from the manifests alone; a damaged file shows as its own entry and blocks nothing.
        public List<ReplayEntry> List()
        {
            var entries = Directory.GetFiles(Folder, "*" + ReplayFormat.Extension).Select(Describe).ToList();
            return entries.OrderByDescending(e => e.Manifest?.FinishedAt ?? DateTimeOffset.MinValue)
                .ThenBy(e => e.Path, StringComparer.Ordinal).ToList();
        }

        private static ReplayEntry Describe(string path)
        {
            try
            {
                using var archive = ReplayArchive.Open(path);
                return new ReplayEntry { Path = path, Manifest = archive.Manifest };
            }
            catch (Exception error) when (error is ReplayFormatException || error is IOException || error is UnauthorizedAccessException)
            {
                return new ReplayEntry { Path = path, Error = error.Message };
            }
        }

        public Task<ReplayAdded> AddFile(string source, CancellationToken cancellation = default) =>
            Add(async target =>
            {
                using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);
                await input.CopyToAsync(target, 81920, cancellation).ConfigureAwait(false);
            }, cancellation);

        // Writes the replay with `write` to a temporary file and keeps it only when it is a complete, valid replay.
        public async Task<ReplayAdded> Add(Func<Stream, Task> write, CancellationToken cancellation = default)
        {
            var temporary = System.IO.Path.Combine(Folder, Incoming + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    await write(output).ConfigureAwait(false);
                cancellation.ThrowIfCancellationRequested();
                return Commit(temporary);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }

        private ReplayAdded Commit(string temporary)
        {
            string id, key;
            using (var archive = ReplayArchive.Open(temporary, true))
            {
                id = archive.Manifest.ReplayId;
                key = archive.ContentKey;
            }
            var target = System.IO.Path.Combine(Folder, FileName(id, key));
            if (File.Exists(target))
                return Existing(target, key);
            File.Move(temporary, target);
            return ReplayAdded.Saved;
        }

        // The same replay is already here; a different one under the same identity stays untouched.
        private static ReplayAdded Existing(string target, string key)
        {
            var kept = Describe(target);
            if (kept.Usable && ReplayArchive.Key(kept.Manifest) == key)
                return ReplayAdded.AlreadySaved;
            throw new ReplayFormatException("Another replay with the same identity is already in the library.");
        }

        public static string FileName(string replayId, string contentKey)
        {
            var safe = Unsafe.Replace(replayId ?? "", "");
            if (safe.Length == 0)
                safe = "replay-" + contentKey.Substring(0, 16);
            return safe.Substring(0, Math.Min(safe.Length, 64)) + ReplayFormat.Extension;
        }

        // A copy for elsewhere: written beside the destination first, so a failed export leaves no broken file.
        public async Task Export(ReplayEntry entry, string destination, CancellationToken cancellation = default)
        {
            var temporary = destination + Incoming + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var input = new FileStream(entry.Path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    await input.CopyToAsync(output, 81920, cancellation).ConfigureAwait(false);
                if (File.Exists(destination))
                    File.Delete(destination);
                File.Move(temporary, destination);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }

        // Only this local copy goes; the game on the server and other copies stay.
        public void Delete(ReplayEntry entry)
        {
            if (Normalized(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(entry.Path))) != Normalized(System.IO.Path.GetFullPath(Folder)))
                throw new InvalidOperationException("Only replays of the library can be deleted.");
            File.Delete(entry.Path);
        }

        private static string Normalized(string folder) =>
            folder.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);

        // Temporary files left by an interrupted save never become entries; one instance runs at a time.
        private void RemoveInterrupted()
        {
            foreach (var file in Directory.GetFiles(Folder, Incoming + "*.tmp"))
            {
                try
                {
                    File.Delete(file);
                }
                catch (IOException) { }
            }
        }
    }
}
