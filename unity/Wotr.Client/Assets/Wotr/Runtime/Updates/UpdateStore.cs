using System;
using System.IO;
using Newtonsoft.Json;

namespace Wotr.Client.Updates
{
    // What the updater remembers between runs, in persistentDataPath/updates/state.json: the release the player
    // agreed to download (consent belongs to one version and never moves to another), the release whose package is
    // downloaded and verified (checked again before it is installed), and whether that package is a repair of the running version.
    // LastRunVersion is the version that last started here (DIST-15: a newer one shows What's new once).
    public sealed class UpdateState
    {
        public string LastRunVersion { get; set; } = "";
        public string ConsentedVersion { get; set; } = "";
        public string ReadyVersion { get; set; } = "";
        public string ReadyPackage { get; set; } = "";
        public string ReadySha256 { get; set; } = "";
        public bool ReadyIsRepair { get; set; }
    }

    public sealed class UpdateStore
    {
        private readonly string path;

        public UpdateStore(string folder)
        {
            Folder = folder;
            path = Path.Combine(folder, "state.json");
        }

        public string Folder { get; }

        public UpdateState Load()
        {
            try
            {
                return File.Exists(path) ? JsonConvert.DeserializeObject<UpdateState>(File.ReadAllText(path)) ?? new UpdateState() : new UpdateState();
            }
            catch (Exception error) when (error is JsonException || error is IOException)
            {
                return new UpdateState();
            }
        }

        // Written through a temporary file so a crash never leaves half a state.
        public void Save(UpdateState state)
        {
            Directory.CreateDirectory(Folder);
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonConvert.SerializeObject(state, Formatting.Indented));
            if (File.Exists(path))
                File.Replace(temporary, path, null);
            else
                File.Move(temporary, path);
        }

        // Drops downloads of other versions; the ready package stays.
        public void RemoveOtherDownloads(string keepVersion)
        {
            if (!Directory.Exists(Folder))
                return;
            foreach (var directory in Directory.GetDirectories(Folder))
                if (Path.GetFileName(directory) != keepVersion)
                    Directory.Delete(directory, true);
        }
    }
}
