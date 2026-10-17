using System;
using System.Diagnostics;
using System.IO;

namespace Wotr.Client.Updates
{
    // Velopack closes every process of the installation before it replaces files, so only one copy of the game
    // may run: the first writes its process id to persistentDataPath/updates/instance.pid; another start that finds
    // a live process of the same name there tells the player the game is already running instead of being killed
    // later by an update. A process id file, not an exclusive file share: Mono does not enforce FileShare between
    // processes on macOS.
    public sealed class SingleInstanceLock : IDisposable
    {
        private readonly string path;
        private readonly int id;

        private SingleInstanceLock(string path, int id)
        {
            this.path = path;
            this.id = id;
        }

        public static SingleInstanceLock TryAcquire(string folder)
        {
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, "instance.pid");
            using (var current = Process.GetCurrentProcess())
            {
                if (OtherInstanceRuns(path, current))
                    return null;
                File.WriteAllText(path, current.Id.ToString());
                return new SingleInstanceLock(path, current.Id);
            }
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(path) && File.ReadAllText(path).Trim() == id.ToString())
                    File.Delete(path);
            }
            catch (IOException)
            {
            }
        }

        private static bool OtherInstanceRuns(string path, Process current)
        {
            if (!File.Exists(path) || !int.TryParse(File.ReadAllText(path).Trim(), out var other) || other == current.Id)
                return false;
            try
            {
                using (var process = Process.GetProcessById(other))
                    return !process.HasExited && process.ProcessName == current.ProcessName;
            }
            catch (Exception error) when (error is ArgumentException || error is InvalidOperationException)
            {
                return false;
            }
        }
    }
}
