using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Editor
{
    internal static class DesktopBuildSetup
    {
        internal const string MainScene = "Assets/Wotr/Scenes/Main.unity";
        // Unity's default for this company and product, pinned (DIST-06): on macOS the player keeps PlayerPrefs and
        // persistentDataPath under the bundle identifier, so changing it would orphan every player's settings and journal.
        internal const string ApplicationIdentifier = "com.WoTR-Private.War-of-the-Ring-Online";
        internal static string RepositoryRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "../../.."));

        internal static void Prepare()
        {
            if (!File.Exists(MainScene))
                throw new BuildFailedException("Missing " + MainScene + ". Restore the scene from the repository.");
            RequirePinnedContent();
            PreparePanel();
            ApplyPlayerSettings();
        }

        private static void RequirePinnedContent()
        {
            try
            {
                PublicContentLock.Verify(RepositoryRoot);
            }
            catch (IOException error)
            {
                throw new BuildFailedException(error.Message);
            }
        }

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "WoTR Private";
            PlayerSettings.productName = "War of the Ring Online";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, ApplicationIdentifier);
            PlayerSettings.defaultScreenWidth = 1366;
            PlayerSettings.defaultScreenHeight = 768;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.allowFullscreenSwitch = true;
            PlayerSettings.runInBackground = true;
        }

        private static void PreparePanel()
        {
            const string path = "Assets/Wotr/Resources/WotrPanel.asset";
            if (AssetDatabase.LoadAssetAtPath<PanelSettings>(path) != null)
                return;
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1366, 768);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = .5f;
            settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("WotrTheme");
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
        }

        internal static void RequirePlatform(BuildTarget target)
        {
            if (target != BuildTarget.StandaloneOSX && target != BuildTarget.StandaloneWindows64)
                throw new BuildFailedException("WoTR supports macOS and Windows x64 desktop builds.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
                throw new BuildFailedException("Install the " + target + " build support module in Unity Hub.");
        }
    }
}
