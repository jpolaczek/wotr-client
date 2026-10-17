using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Wotr.Contracts;

namespace Wotr.Client
{
    // REPLAY-03: one .wotrreplay file opened for reading. Before anything is shown it is checked against format v1
    // (wotr-server docs/server/REPLAY-FORMAT.md): fixed entry names only, the limits, the manifest's counts and a
    // complete recording; with `verify` every entry's size and SHA-256 too. Entries are read one at a time and each
    // read checks its own sum again; nothing is unpacked to disk.
    public sealed class ReplayArchive : IDisposable
    {
        public static readonly JsonSerializerSettings Json = new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() },
            NullValueHandling = NullValueHandling.Ignore
        };

        private static readonly Regex AllowedEntry = new Regex(
            @"^(manifest\.json|battles\.json|index/(steps|turns)\.json|frames/\d{4}\.json|history/(free-peoples|shadow)/\d{4}\.json|catalog/[a-z-]+\.json)$");

        private readonly FileStream stream;
        private readonly ZipArchive zip;
        private readonly Dictionary<string, ReplayFile> files;
        public ReplayManifest Manifest { get; }
        public string Path { get; }

        private ReplayArchive(string path, FileStream stream, ZipArchive zip, ReplayManifest manifest)
        {
            Path = path;
            this.stream = stream;
            this.zip = zip;
            Manifest = manifest;
            files = manifest.Files.ToDictionary(f => f.Path, StringComparer.Ordinal);
        }

        public static ReplayArchive Open(string path, bool verify = false)
        {
            var info = new FileInfo(path);
            if (!info.Exists)
                throw new ReplayFormatException("The replay file is missing.");
            if (info.Length > ReplayFormat.MaxArchiveBytes)
                throw new ReplayFormatException("The replay file is too large.");
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            try
            {
                var zip = OpenZip(stream);
                var archive = new ReplayArchive(path, stream, zip, CheckStructure(zip));
                if (verify)
                    archive.VerifyAll();
                return archive;
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        private static ZipArchive OpenZip(Stream stream)
        {
            try
            {
                return new ZipArchive(stream, ZipArchiveMode.Read, true);
            }
            catch (Exception error) when (error is InvalidDataException || error is IOException || error is ArgumentException)
            {
                throw new ReplayFormatException("The replay file is damaged.");
            }
        }

        private static ReplayManifest CheckStructure(ZipArchive zip)
        {
            if (zip.Entries.Count > ReplayFormat.MaxEntries)
                throw new ReplayFormatException("The replay file has too many parts.");
            foreach (var entry in zip.Entries)
            {
                if (!AllowedEntry.IsMatch(entry.FullName))
                    throw new ReplayFormatException("The replay file holds an unexpected part.");
                if (entry.Length > ReplayFormat.MaxEntryBytes)
                    throw new ReplayFormatException("A part of the replay file is too large.");
            }
            var manifest = ReadManifest(zip);
            CheckManifest(manifest, zip);
            return manifest;
        }

        private static ReplayManifest ReadManifest(ZipArchive zip)
        {
            var entry = zip.GetEntry(ReplayFormat.ManifestEntry) ?? throw new ReplayFormatException("The replay file has no manifest.");
            try
            {
                return JsonConvert.DeserializeObject<ReplayManifest>(Encoding.UTF8.GetString(Read(entry)), Json)
                    ?? throw new ReplayFormatException("The replay manifest is empty.");
            }
            catch (JsonException)
            {
                throw new ReplayFormatException("The replay manifest is damaged.");
            }
        }

        private static void CheckManifest(ReplayManifest manifest, ZipArchive zip)
        {
            if (manifest.Format != ReplayFormat.Name)
                throw new ReplayFormatException("This is not a War of the Ring replay.");
            if (manifest.FormatVersion > ReplayFormat.Version)
                throw new ReplayFormatException("This replay uses a newer replay format. Update the game to open it.");
            if (manifest.FormatVersion < 1)
                throw new ReplayFormatException("The replay format is not recognised.");
            if (!manifest.Complete)
                throw new ReplayFormatException("This replay is incomplete.");
            if (manifest.Steps < 1 || manifest.Steps > ReplayFormat.MaxSteps)
                throw new ReplayFormatException("The replay's length is out of range.");
            if (manifest.FrameChunks != (manifest.Steps + ReplayFormat.FramesPerChunk - 1) / ReplayFormat.FramesPerChunk)
                throw new ReplayFormatException("The replay's parts do not match its length.");
            var listed = new HashSet<string>(manifest.Files.Select(f => f.Path), StringComparer.Ordinal);
            var present = zip.Entries.Select(e => e.FullName).Where(n => n != ReplayFormat.ManifestEntry).ToList();
            if (listed.Count != manifest.Files.Count || !listed.SetEquals(present))
                throw new ReplayFormatException("The replay's parts do not match its manifest.");
            foreach (var file in manifest.Files)
                if (zip.GetEntry(file.Path).Length != file.Size)
                    throw new ReplayFormatException("A part of the replay file has the wrong size.");
            foreach (var required in RequiredEntries(manifest))
                if (!listed.Contains(required))
                    throw new ReplayFormatException("The replay file is missing a part.");
        }

        private static IEnumerable<string> RequiredEntries(ReplayManifest manifest)
        {
            yield return ReplayFormat.StepsEntry;
            yield return ReplayFormat.TurnsEntry;
            yield return ReplayFormat.BattlesEntry;
            for (var chunk = 0; chunk < manifest.FrameChunks; chunk++)
                yield return ReplayFormat.FramesEntry(chunk);
            for (var chunk = 0; chunk < manifest.FreePeoplesHistoryChunks; chunk++)
                yield return ReplayFormat.HistoryEntry(Side.FreePeoples, chunk);
            for (var chunk = 0; chunk < manifest.ShadowHistoryChunks; chunk++)
                yield return ReplayFormat.HistoryEntry(Side.Shadow, chunk);
        }

        // Every part against its sum, before a file enters the library.
        public void VerifyAll()
        {
            foreach (var file in Manifest.Files)
                Entry(file.Path);
        }

        public List<ReplayStep> Steps() => Read<List<ReplayStep>>(ReplayFormat.StepsEntry);
        public List<ReplayTurn> Turns() => Read<List<ReplayTurn>>(ReplayFormat.TurnsEntry);
        public List<ReplayFrame> Frames(int chunk) => Read<List<ReplayFrame>>(ReplayFormat.FramesEntry(chunk));
        public ActionLogPage History(Side side, int chunk) => Read<ActionLogPage>(ReplayFormat.HistoryEntry(side, chunk));
        public List<ReplayBattleReport> Battles() => Read<List<ReplayBattleReport>>(ReplayFormat.BattlesEntry);

        public int HistoryChunks(Side side) =>
            side == Side.FreePeoples ? Manifest.FreePeoplesHistoryChunks : Manifest.ShadowHistoryChunks;

        public string Catalog(string file) => Encoding.UTF8.GetString(Entry(ReplayFormat.CatalogEntry(file)));

        // The key of what the replay shows: the sums of all its parts but the manifest (an export date differs).
        public string ContentKey => Key(Manifest);

        public static string Key(ReplayManifest manifest) =>
            Hex(Sha256(Encoding.UTF8.GetBytes(string.Join("\n", manifest.Files.OrderBy(f => f.Path, StringComparer.Ordinal)
                .Select(f => f.Path + ":" + f.Sha256)))));

        private T Read<T>(string name)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(Entry(name)), Json);
            }
            catch (JsonException)
            {
                throw new ReplayFormatException("A part of the replay file is damaged.");
            }
        }

        private byte[] Entry(string name)
        {
            if (!files.TryGetValue(name, out var file))
                throw new ReplayFormatException("The replay file is missing a part.");
            byte[] bytes;
            lock (zip)
                bytes = Read(zip.GetEntry(name));
            if (bytes.Length != file.Size || Hex(Sha256(bytes)) != file.Sha256)
                throw new ReplayFormatException("A part of the replay file is damaged.");
            return bytes;
        }

        private static byte[] Read(ZipArchiveEntry entry)
        {
            try
            {
                using var source = entry.Open();
                using var copy = new MemoryStream();
                source.CopyTo(copy);
                return copy.ToArray();
            }
            catch (Exception error) when (error is InvalidDataException || error is IOException)
            {
                throw new ReplayFormatException("The replay file is damaged.");
            }
        }

        private static byte[] Sha256(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(bytes);
        }

        private static string Hex(byte[] bytes) => string.Concat(bytes.Select(b => b.ToString("x2")));

        public void Dispose()
        {
            zip.Dispose();
            stream.Dispose();
        }
    }
}
