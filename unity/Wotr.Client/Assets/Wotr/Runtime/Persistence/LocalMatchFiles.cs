using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Wotr.Client
{
    public sealed class LocalSaveException : Exception
    {
        public LocalSaveException(Exception inner) : base("Local recovery data is unavailable. Check storage and try again.", inner) { }
    }

    // Only local intentions are stored here. No session credentials or server snapshots.
    public sealed class LocalMatchFiles : ILocalMatchStorage
    {
        private static readonly object IoGate = new object();
        private readonly string directory;
        private readonly string scope;
        public string MatchId { get; }

        public LocalMatchFiles(string root, string server, string account, string matchId)
        {
            if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(matchId))
                throw new ArgumentException("Recovery data requires a signed-in account and match.");
            scope = server + "\n" + account + "\n" + matchId;
            using var hash = SHA256.Create();
            var key = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(scope))).Replace("-", "").ToLowerInvariant();
            directory = Path.Combine(root, key);
            MatchId = matchId;
        }

        public Task<T> Read<T>(string name) where T : class => Access(() => ReadFile<T>(name));
        public Task Write<T>(string name, T data) where T : class => Access(() => { WriteFile(name, data); return true; });

        public Task MergeNotificationRead(NotificationReadRecord incoming) => Access(() =>
        {
            NotificationReadRecord saved = null;
            try { saved = ReadFile<NotificationReadRecord>("notification-read"); }
            catch (Exception error) when (error is InvalidDataException || error is JsonException) { }
            var ids = new HashSet<string>(saved?.Steps ?? new List<string>());
            ids.UnionWith(incoming.Steps);
            WriteFile("notification-read", new NotificationReadRecord { Steps = new List<string>(ids) });
            return true;
        });

        public Task SaveCommand(string name, Wotr.Contracts.GameCommand command) => Access(() =>
        {
            var saved = ReadFile<Wotr.Contracts.GameCommand>(name);
            if (command.MatchId != MatchId || saved != null && JsonConvert.SerializeObject(saved) != JsonConvert.SerializeObject(command))
                throw new InvalidDataException("A different command is already awaiting recovery.");
            WriteFile(name, command);
            return true;
        });

        public Task CompleteCommand(string name, string commandId) => Access(() =>
        {
            if (ReadFile<Wotr.Contracts.GameCommand>(name)?.CommandId == commandId)
                File.Delete(FilePath(name));
            return true;
        });

        private Task<T> Access<T>(Func<T> operation) => Task.Run(() =>
        {
            try
            {
                lock (IoGate)
                {
                    Directory.CreateDirectory(directory);
                    using var access = new FileStream(Path.Combine(directory, "access.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    return operation();
                }
            }
            catch (Exception error) when (error is IOException || error is InvalidDataException || error is UnauthorizedAccessException || error is JsonException)
            {
                throw new LocalSaveException(error);
            }
        });

        private T ReadFile<T>(string name) where T : class
        {
            var path = FilePath(name);
            if (!File.Exists(path))
                return null;
            if (new FileInfo(path).Length > 1024 * 1024)
                throw new InvalidDataException("Recovery record is too large.");
            var record = JsonConvert.DeserializeObject<Record<T>>(File.ReadAllText(path));
            if (record == null || record.Version != 1 || record.Scope != scope || record.Data == null)
                throw new InvalidDataException("Recovery record has an incompatible scope or version.");
            return record.Data;
        }

        private void WriteFile<T>(string name, T data) where T : class
        {
            var path = FilePath(name);
            if (data == null)
            {
                File.Delete(path);
                return;
            }
            var temporary = path + ".tmp";
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Record<T> { Version = 1, Scope = scope, Data = data }));
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            if (File.Exists(path))
                File.Replace(temporary, path, null);
            else
                File.Move(temporary, path);
        }

        private string FilePath(string name)
        {
            if (name != "action" && name != "policy" && name != "draft" && name != "notification-read")
                throw new ArgumentException("Unknown recovery record.");
            return Path.Combine(directory, name + ".json");
        }

        private sealed class Record<T>
        {
            public int Version { get; set; }
            public string Scope { get; set; }
            public T Data { get; set; }
        }
    }
}
