using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Editor
{
    public static class BuildTasks
    {
        public static void BuildLocalMac()
        {
            PlatformArchitecture.UseMacUniversal();
            Build(BuildTarget.StandaloneOSX, "local/macos/Wotr.app", BuildOptions.None);
        }

        public static void BuildAndRunLocalMac()
        {
            PlatformArchitecture.UseMacUniversal();
            Build(BuildTarget.StandaloneOSX, "local/macos/Wotr.app", BuildOptions.AutoRunPlayer);
        }

        public static void BuildLocalWindows()
        {
            PlatformArchitecture.UseWindowsX64();
            Build(BuildTarget.StandaloneWindows64, "local/windows/Wotr.exe", BuildOptions.None);
        }

        // LNX-02 (D18): Linux x86_64, also the Steam Deck's build.
        public static void BuildLocalLinux()
        {
            Build(BuildTarget.StandaloneLinux64, "local/linux/Wotr.x86_64", BuildOptions.None);
        }

        public static void BuildLinux()
        {
            Build(BuildTarget.StandaloneLinux64, "linux/Wotr.x86_64", BuildOptions.Development);
        }

        public static void BuildMac()
        {
            PlatformArchitecture.UseMacUniversal();
            Build(BuildTarget.StandaloneOSX, "mac/Wotr.app", BuildOptions.Development);
        }
        public static void BuildWindows()
        {
            PlatformArchitecture.UseWindowsX64();
            Build(BuildTarget.StandaloneWindows64, "windows/Wotr.exe", BuildOptions.Development);
        }

        public static void BuildReleaseMac()
        {
            PlatformArchitecture.UseMacUniversal();
            BuildRelease(BuildTarget.StandaloneOSX, "macos/Wotr.app");
        }

        public static void BuildReleaseWindows()
        {
            PlatformArchitecture.UseWindowsX64();
            BuildRelease(BuildTarget.StandaloneWindows64, "windows/Wotr.exe");
        }

        public static void BuildReleaseLinux()
        {
            BuildRelease(BuildTarget.StandaloneLinux64, "linux/Wotr.x86_64");
        }

        private static void BuildRelease(BuildTarget target, string output)
        {
            var identity = ClientBuildIdentity.Create(target, release: true);
            var previousVersion = PlayerSettings.bundleVersion;
            try
            {
                PlayerSettings.bundleVersion = identity.Version;
                Build(target, "../releases/" + identity.Version + "/" + output, BuildOptions.None, identity);
            }
            finally
            {
                PlayerSettings.bundleVersion = previousVersion;
            }
        }

        public static void PrepareProject()
        {
            DesktopBuildSetup.Prepare();
        }

        // Every player embeds its identity; build-info.json beside it holds the same bytes (DS10).
        private static string Build(BuildTarget target, string output, BuildOptions options, ClientBuildInfo identity = null)
        {
            DesktopBuildSetup.RequirePlatform(target);
            PrepareProject();
            identity = identity ?? ClientBuildIdentity.Create(target, release: false);
            var destination = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../.artifacts/builds", output));
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            // MENU-28: first the addresses, so a missing one stops the build before anything is embedded.
            BuildServerAddresses.Embed();
            var embedded = ClientBuildIdentity.Embed(identity);
            BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { DesktopBuildSetup.MainScene },
                    target = target,
                    locationPathName = destination,
                    options = options
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Build failed: " + report.summary.result);
                if ((report.summary.options & BuildOptions.Development) != (options & BuildOptions.Development))
                    throw new InvalidOperationException("Build development flag differs from the requested configuration.");
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(destination), "build-info.json"), embedded, new UTF8Encoding(false));
            }
            finally
            {
                ClientBuildIdentity.RemoveEmbedded();
            }
            Debug.Log("WOTR_BUILD_SUCCESS " + destination);
            GameCiBuildReport.Succeeded(report.summary);
            return destination;
        }
    }
}
