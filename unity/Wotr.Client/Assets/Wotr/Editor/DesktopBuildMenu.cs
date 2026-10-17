using System.IO;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace Wotr.Editor
{
    public static class DesktopBuildMenu
    {
        [MenuItem("WoTR/Build/Prepare Project", priority = 10)]
        public static void PrepareProject()
        {
            BuildTasks.PrepareProject();
            EditorBuildSettings.scenes = MainScenes();
            PlatformArchitecture.UseMacUniversal();
            PlatformArchitecture.UseWindowsX64();
            EnsureProfile("WoTR macOS", "0d2129357eac403d8b359c2dcbf82502", BuildTarget.StandaloneOSX);
            EnsureProfile("WoTR Windows", "4e3c793746204150860bf175a9a41a05", BuildTarget.StandaloneWindows64);
            AssetDatabase.SaveAssets();
            Debug.Log("WOTR_EDITOR_READY Fullscreen Window; Main scene; desktop profiles; public content synchronized.");
        }

        private static EditorBuildSettingsScene[] MainScenes()
        {
            return new[] { new EditorBuildSettingsScene(DesktopBuildSetup.MainScene, true) };
        }

        private static void EnsureProfile(string name, string platformId, BuildTarget target)
        {
            var path = "Assets/Settings/Build Profiles/" + name + ".asset";
            var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(path);
            if (profile == null)
            {
                if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
                {
                    Debug.LogWarning("Install " + target + " support in Unity Hub to create " + name + ".");
                    return;
                }
                profile = BuildProfile.CreateBuildProfile(new GUID(platformId), name);
            }
            profile.overrideGlobalScenes = true;
            profile.scenes = MainScenes();
            EditorUtility.SetDirty(profile);
        }

        [MenuItem("WoTR/Build/Build macOS", priority = 30)]
        public static void BuildMac() => BuildTasks.BuildLocalMac();

        [MenuItem("WoTR/Build/Build And Run macOS", priority = 31)]
        public static void BuildAndRunMac() => BuildTasks.BuildAndRunLocalMac();

        [MenuItem("WoTR/Build/Build Windows", priority = 32)]
        public static void BuildWindows() => BuildTasks.BuildLocalWindows();

        [MenuItem("WoTR/Build/Open Builds Folder", priority = 50)]
        public static void OpenBuildsFolder()
        {
            var path = Path.Combine(DesktopBuildSetup.RepositoryRoot, ".artifacts/builds/local");
            Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(path);
        }

        [MenuItem("WoTR/Build/Build macOS", true)]
        [MenuItem("WoTR/Build/Build And Run macOS", true)]
        [MenuItem("WoTR/Build/Build Windows", true)]
        [MenuItem("WoTR/Build/Prepare Project", true)]
        private static bool CanBuild()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode &&
                !EditorApplication.isCompiling && !BuildPipeline.isBuildingPlayer;
        }
    }
}
