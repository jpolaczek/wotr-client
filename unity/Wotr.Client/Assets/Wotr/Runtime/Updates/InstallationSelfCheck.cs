using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Wotr.Client.Updates
{
    // Installer smoke tests (DIST-06, DIST-10): `--wotr-self-check <file>` writes how this copy started and was
    // installed, then quits before the game opens. It reads nothing private and writes only the file its caller
    // named, so release builds keep it: the smoke tests have to check the build players install.
    public static class InstallationSelfCheck
    {
        public const string Argument = "--wotr-self-check";

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
            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(Describe(), true));
                Application.Quit(0);
            }
            catch (Exception error)
            {
                Debug.LogError("Self-check report failed: " + error);
                Application.Quit(1);
            }
            return true;
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
