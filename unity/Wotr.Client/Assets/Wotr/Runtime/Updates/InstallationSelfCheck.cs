using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Wotr.Client.Updates
{
    // Installer smoke tests (DIST-06, DIST-10): `--wotr-self-check <file>` writes how this copy started and was
    // installed, then quits before the game opens. It reads nothing private and writes only the file its caller
    // named, so release builds keep it: the smoke tests have to check the build players install.
    // SEC-01: the file must be a new `.json` file named by an absolute path in an existing folder; an existing path
    // (file, folder or link) is never overwritten or followed. Otherwise the game exits with code 2 and writes nothing.
    public static class InstallationSelfCheck
    {
        public const string Argument = "--wotr-self-check";
        public const int RefusedExitCode = 2;

        [Serializable]
        private sealed class Report
        {
            public string buildVersion;
            public string platform;
            public bool development;
            public string installedVersion;
            public bool canUpdateItself;
            public string velopackStartup;
            public string persistentDataPath;
            public string processArchitecture;
        }

        public static bool TryRun()
        {
            var path = ReportPath();
            if (path == null)
                return false;
            var problem = Problem(path);
            if (problem.Length > 0)
            {
                Debug.LogError("Self-check refused: " + problem);
                Application.Quit(RefusedExitCode);
                return true;
            }
            try
            {
                Write(path, JsonUtility.ToJson(Describe(), true));
                Application.Quit(0);
            }
            catch (Exception error)
            {
                Debug.LogError("Self-check report failed: " + error);
                Application.Quit(1);
            }
            return true;
        }

        // Why the report may not go to this path, or "" when it may.
        public static string Problem(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
                return "the report needs an absolute path.";
            if (!string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
                return "the report must be a .json file.";
            var folder = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                return "the report's folder does not exist.";
            if (File.Exists(path) || Directory.Exists(path))
                return "the report's path already exists.";
            return "";
        }

        // CreateNew fails on any existing entry, a dangling link included, so nothing else is ever written through.
        public static void Write(string path, string text)
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
                writer.Write(text);
        }

        private static Report Describe()
        {
            var identity = ClientIdentity.Current;
            return new Report
            {
                buildVersion = identity.Version,
                platform = identity.Platform,
                development = identity.Development,
                installedVersion = ClientInstallation.InstalledVersion,
                canUpdateItself = ClientInstallation.CanUpdateItself,
                velopackStartup = VelopackStartup.Result,
                persistentDataPath = Application.persistentDataPath,
                processArchitecture = RuntimeInformation.ProcessArchitecture.ToString()
            };
        }

        private static string ReportPath()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, Argument);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
