using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // REPLAY-03 (RP05): the server's v1 sample enters the library, reads any step from both sides without the network,
    // and exports as a working copy. The same replay is kept once; another replay under its identity, a damaged,
    // newer, incomplete or foreign file never enters and never harms what is already there.
    public class ReplayLibraryTests
    {
        private static readonly string Sample = Path.Combine(Application.dataPath, "Wotr/Tests/Editor/ReplaySample.wotrreplay");
        private string folder;

        [SetUp]
        public void CreateFolder() => folder = Path.Combine(Path.GetTempPath(), "wotr-replays-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void RemoveFolder()
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        [Test]
        public async Task TheSampleEntersOnceReadsBothSidesAndExportsAWorkingCopy()
        {
            var library = new ReplayLibrary(Path.Combine(folder, "Replays"));
            Assert.AreEqual(ReplayAdded.Saved, await library.AddFile(Sample));
            Assert.AreEqual(ReplayAdded.AlreadySaved, await library.AddFile(Sample));
            Assert.AreEqual(ReplayAdded.AlreadySaved, await library.AddFile(Variant(ExportedLater)), "A new export of the same game is the same replay.");
            var entry = library.List().Single();
            Assert.IsTrue(entry.Usable);
            Assert.AreEqual(Side.Shadow, entry.Manifest.Winner);
            using (var archive = ReplayArchive.Open(entry.Path, true))
            {
                var last = archive.Manifest.Steps - 1;
                var frame = archive.Frames(last / ReplayFormat.FramesPerChunk)[last % ReplayFormat.FramesPerChunk];
                Assert.AreEqual(last, frame.Step);
                Assert.AreEqual(Side.FreePeoples, frame.FreePeoples.You);
                Assert.AreEqual(Side.Shadow, frame.Shadow.You);
                Assert.AreEqual(Side.Shadow, frame.Shadow.Winner);
                Assert.IsNotEmpty(archive.History(Side.FreePeoples, 0).Steps);
                StringAssert.Contains("\"id\"", archive.Catalog("base-card-text.json"));
            }
            var exported = Path.Combine(folder, "copy.wotrreplay");
            await library.Export(entry, exported);
            var other = new ReplayLibrary(Path.Combine(folder, "Other installation"));
            Assert.AreEqual(ReplayAdded.Saved, await other.AddFile(exported));
            library.Delete(entry);
            Assert.IsEmpty(library.List());
            Assert.IsTrue(File.Exists(exported), "Deleting the library's copy leaves other copies.");
        }

        [TestCase("Truncated", "The replay file is damaged.")]
        [TestCase("ChangedFrame", "A part of the replay file is damaged.")]
        [TestCase("Newer", "This replay uses a newer replay format. Update the game to open it.")]
        [TestCase("Incomplete", "This replay is incomplete.")]
        [TestCase("Foreign", "The replay file holds an unexpected part.")]
        [TestCase("MissingPart", "The replay's parts do not match its manifest.")]
        public async Task ABrokenFileNeverEntersAndLeavesTheLibraryAsItWas(string variant, string message)
        {
            var library = new ReplayLibrary(Path.Combine(folder, "Replays"));
            var broken = Variant(variant switch
            {
                "Truncated" => (Action<string>)Truncate,
                "ChangedFrame" => ChangeFrame,
                "Newer" => Newer,
                "Incomplete" => Incomplete,
                "Foreign" => Foreign,
                _ => MissingPart
            });
            var error = await Refused<ReplayFormatException>(() => library.AddFile(broken));
            Assert.AreEqual(message, error.Message);
            Assert.IsEmpty(library.List());
            Assert.IsEmpty(Directory.GetFiles(library.Folder), "No temporary file is left behind.");
        }

        [Test]
        public async Task AnotherReplayUnderTheSameIdentityNeverOverwritesTheSavedOne()
        {
            var library = new ReplayLibrary(Path.Combine(folder, "Replays"));
            await library.AddFile(Sample);
            var before = File.ReadAllBytes(library.List().Single().Path);
            var error = await Refused<ReplayFormatException>(() => library.AddFile(Variant(OtherContent)));
            StringAssert.Contains("same identity", error.Message);
            CollectionAssert.AreEqual(before, File.ReadAllBytes(library.List().Single().Path));
        }

        [Test]
        public async Task AnInterruptedSaveLeavesNoEntryAndADamagedFileBlocksNothing()
        {
            var library = new ReplayLibrary(Path.Combine(folder, "Replays"));
            await library.AddFile(Sample);
            await Refused<IOException>(() => library.Add(async output =>
            {
                await output.WriteAsync(new byte[1000], 0, 1000);
                throw new IOException("The disk is full.");
            }));
            File.WriteAllText(Path.Combine(library.Folder, ".incoming-left.tmp"), "half");
            File.WriteAllText(Path.Combine(library.Folder, "broken.wotrreplay"), "not a zip");
            var reopened = new ReplayLibrary(library.Folder);
            var entries = reopened.List();
            Assert.AreEqual(2, entries.Count);
            Assert.AreEqual(1, entries.Count(e => e.Usable));
            Assert.AreEqual("The replay file is damaged.", entries.Single(e => !e.Usable).Error);
            Assert.IsFalse(File.Exists(Path.Combine(library.Folder, ".incoming-left.tmp")), "Interrupted saves are cleared.");
        }

        // Awaited, never waited for: a blocking assertion would hold the main thread the awaits may need.
        private static async Task<T> Refused<T>(Func<Task> action) where T : Exception
        {
            try
            {
                await action();
            }
            catch (T error)
            {
                return error;
            }
            Assert.Fail("Expected " + typeof(T).Name + ".");
            return null;
        }

        private static ZipArchive OpenForUpdate(string path) =>
            new ZipArchive(new FileStream(path, FileMode.Open, FileAccess.ReadWrite), ZipArchiveMode.Update);

        // A copy of the sample changed by `change`, in the test's folder.
        private string Variant(Action<string> change)
        {
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, Guid.NewGuid().ToString("N") + ".wotrreplay");
            File.Copy(Sample, path);
            change(path);
            return path;
        }

        private static void Truncate(string path)
        {
            var bytes = File.ReadAllBytes(path);
            File.WriteAllBytes(path, bytes.Take(bytes.Length / 2).ToArray());
        }

        // The same length with one changed letter: only the sum can tell.
        private static void ChangeFrame(string path) =>
            Rewrite(path, ReplayFormat.FramesEntry(0), text => text.Replace("\"Shadow\"", "\"Shadoz\""), false);

        private static void Newer(string path) => EditManifest(path, manifest => manifest.FormatVersion = ReplayFormat.Version + 1);

        private static void Incomplete(string path) => EditManifest(path, manifest => manifest.Complete = false);

        private static void ExportedLater(string path) => EditManifest(path, manifest => manifest.ExportedAt = manifest.ExportedAt.AddDays(1));

        private static void Foreign(string path)
        {
            using var zip = OpenForUpdate(path);
            using var writer = new StreamWriter(zip.CreateEntry("../outside.txt").Open());
            writer.Write("outside");
        }

        private static void MissingPart(string path)
        {
            using var zip = OpenForUpdate(path);
            zip.GetEntry(ReplayFormat.TurnsEntry).Delete();
        }

        // A valid replay with the same identity and different content: a catalog text changed with a matching sum.
        private static void OtherContent(string path) =>
            Rewrite(path, ReplayFormat.CatalogEntry("base-characters.json"), text => text + " ", true);

        private static void EditManifest(string path, Action<ReplayManifest> edit) =>
            Rewrite(path, ReplayFormat.ManifestEntry, text =>
            {
                var manifest = JsonConvert.DeserializeObject<ReplayManifest>(text, ReplayArchive.Json);
                edit(manifest);
                return JsonConvert.SerializeObject(manifest, ReplayArchive.Json);
            }, false);

        // Replaces one entry's text; with `resum` the manifest records the new size and sum, as a valid file would.
        private static void Rewrite(string path, string entryName, Func<string, string> change, bool resum)
        {
            using var zip = OpenForUpdate(path);
            var entry = zip.GetEntry(entryName);
            string text;
            using (var reader = new StreamReader(entry.Open()))
                text = reader.ReadToEnd();
            entry.Delete();
            var bytes = Encoding.UTF8.GetBytes(change(text));
            using (var output = zip.CreateEntry(entryName).Open())
                output.Write(bytes, 0, bytes.Length);
            if (!resum)
                return;
            var manifestEntry = zip.GetEntry(ReplayFormat.ManifestEntry);
            ReplayManifest manifest;
            using (var reader = new StreamReader(manifestEntry.Open()))
                manifest = JsonConvert.DeserializeObject<ReplayManifest>(reader.ReadToEnd(), ReplayArchive.Json);
            var file = manifest.Files.Single(f => f.Path == entryName);
            file.Size = bytes.Length;
            using (var sha = SHA256.Create())
                file.Sha256 = string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2")));
            manifestEntry.Delete();
            using var writer = new StreamWriter(zip.CreateEntry(ReplayFormat.ManifestEntry).Open());
            writer.Write(JsonConvert.SerializeObject(manifest, ReplayArchive.Json));
        }
    }
}
