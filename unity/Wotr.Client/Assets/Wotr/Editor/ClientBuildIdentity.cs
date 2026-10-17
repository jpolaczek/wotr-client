using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.Editor
{
    // DS10: the one generator of a build's identity. BuildTasks embeds its JSON in the player and writes the same
    // bytes beside it as build-info.json, which release packaging, the release signer and DIST-06 installers read.
    public static class ClientBuildIdentity
    {
        public const string GeneratedFolder = "Assets/Wotr/Generated";
        public const string ResourcePath = GeneratedFolder + "/Resources/" + ClientIdentity.ResourceName + ".json";
        private static readonly Regex Commit = new Regex(@"\A[a-f0-9]{40}\z");

        [Serializable]
        private sealed class VersionFile
        {
            public string version;
        }

        public static ClientBuildInfo Create(BuildTarget target, bool release)
        {
            var root = DesktopBuildSetup.RepositoryRoot;
            return new ClientBuildInfo
            {
                Version = ReleaseVersion(root),
                SourceCommit = SourceCommit(root, release),
                Platform = Platform(target),
                Development = !release,
                ProtocolVersion = Protocol.Version,
                RulesVersion = Protocol.RulesVersion,
                ContentVersion = Protocol.ContentVersion,
                PublicContentVersion = PublicContentLock.PinnedVersion(root),
                PublicContentSha256 = PublicContentHash(),
                UnityVersion = Application.unityVersion
            };
        }

        public static string Embed(ClientBuildInfo info)
        {
            var text = ClientIdentity.Serialize(info);
            Directory.CreateDirectory(Path.GetDirectoryName(ResourcePath));
            File.WriteAllText(ResourcePath, text, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(ResourcePath, ImportAssetOptions.ForceSynchronousImport);
            return text;
        }

        public static void RemoveEmbedded()
        {
            if (AssetDatabase.IsValidFolder(GeneratedFolder))
                AssetDatabase.DeleteAsset(GeneratedFolder);
        }

        // A build started outside BuildTasks would ship without an identity; it stops here with the reason.
        public static void RequireEmbedded(BuildTarget target)
        {
            if (!File.Exists(ResourcePath))
                throw new BuildFailedException("Build through WoTR → Build or BuildTasks so the player carries its identity (DIST-05).");
            if (ClientIdentity.Parse(File.ReadAllText(ResourcePath)).Platform != Platform(target))
                throw new BuildFailedException("The embedded client identity names another platform than this build.");
        }

        public static string Platform(BuildTarget target)
        {
            if (target == BuildTarget.StandaloneOSX)
                return ClientPlatforms.MacOS;
            if (target == BuildTarget.StandaloneWindows64)
                return ClientPlatforms.Windows;
            if (target == BuildTarget.StandaloneLinux64)
                return ClientPlatforms.Linux;
            throw new BuildFailedException("WoTR builds only macOS Universal, Windows x64 and Linux x64.");
        }

        private static string ReleaseVersion(string root)
        {
            var version = JsonUtility.FromJson<VersionFile>(File.ReadAllText(Path.Combine(root, "release/version.json")))?.version;
            if (!ClientVersion.TryParse(version, out var parsed))
                throw new InvalidOperationException("Invalid release/version.json version.");
            return parsed.Text;
        }

        // GameCI starts Unity in a container without the job's environment, so release builds take the commit as the
        // -wotrCommit argument; WOTR_BUILD_COMMIT remains for local scripts.
        private static string SourceCommit(string root, bool release)
        {
            var commit = CommandLineCommit() ?? Environment.GetEnvironmentVariable("WOTR_BUILD_COMMIT") ?? (release ? "" : GitHead(root));
            if (release && !Commit.IsMatch(commit))
                throw new InvalidOperationException("Pass -wotrCommit <40-character source commit> to a release build.");
            return Commit.IsMatch(commit) ? commit : "";
        }

        private static string CommandLineCommit()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-wotrCommit");
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        // Best effort for local builds; a release always names its commit explicitly.
        private static string GitHead(string root)
        {
            try
            {
                var start = new ProcessStartInfo("git", "rev-parse HEAD")
                {
                    WorkingDirectory = root, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
                };
                using (var git = Process.Start(start))
                {
                    var output = git.StandardOutput.ReadToEnd().Trim();
                    return git.WaitForExit(5000) && git.ExitCode == 0 ? output : "";
                }
            }
            catch (Exception error) when (error is Win32Exception || error is InvalidOperationException)
            {
                return "";
            }
        }

        private static string PublicContentHash()
        {
            var entries = Directory.GetFiles(Path.Combine(Application.dataPath, "Wotr/Resources"), "*.json")
                .OrderBy(Path.GetFileName, StringComparer.Ordinal).Select(path => Path.GetFileName(path) + ":" + Hash(File.ReadAllBytes(path)));
            return Hash(Encoding.UTF8.GetBytes(string.Join("\n", entries) + "\n"));
        }

        private static string Hash(byte[] bytes)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }
}
