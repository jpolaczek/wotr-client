using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.Tests
{
    public class DisplayOptionsTests
    {
        internal sealed class FakeScreen : IDisplayScreen
        {
            public DisplaySize Native { get; set; }
        }

        [Test]
        public void AWindowPicksAPresetThatFitsAndEveryPresetKeepsItsPlace()
        {
            var screen = new FakeScreen { Native = new DisplaySize(1920, 1080) };
            var choices = DisplayOptions.Choices(screen, WindowMode.Windowed);
            CollectionAssert.AreEqual(new[] { "Native", "1280×720", "1366×768", "1600×900", "1920×1080", "2560×1440", "3840×2160" },
                choices.Select(choice => choice.Label));
            CollectionAssert.AreEqual(new[] { true, true, true, true, true, false, false }, choices.Select(choice => choice.Supported));
            StringAssert.Contains("1920×1080", choices[0].Tooltip);
            StringAssert.Contains("Larger than this screen", choices[5].Tooltip);
            Assert.AreEqual(new DisplaySize(1600, 900), DisplayOptions.Resolve(new DisplaySize(1600, 900), screen, WindowMode.Windowed));
        }

        // MENU-17: a full-screen mode always uses the screen's own resolution; no preset can be picked there, and a
        // saved choice waits for the window.
        [Test]
        public void AFullScreenModeAlwaysUsesTheScreensOwnResolution()
        {
            var screen = new FakeScreen { Native = new DisplaySize(2880, 1800) };
            foreach (var mode in new[] { WindowMode.Fullscreen, WindowMode.Borderless })
            {
                var choices = DisplayOptions.Choices(screen, mode);
                Assert.IsTrue(choices.All(choice => !choice.Supported), mode + " picks no size");
                Assert.IsTrue(choices.All(choice => choice.Tooltip.Contains("Choose Windowed")), mode.ToString());
                CollectionAssert.AreEqual(DisplayOptions.Presets.Select(p => p.IsNative ? "Native" : p.ToString()), choices.Select(c => c.Label));
                foreach (var preset in DisplayOptions.Presets)
                {
                    Assert.AreEqual(screen.Native, DisplayOptions.Resolve(preset, screen, mode), preset.Key);
                    Assert.AreEqual(DisplaySize.Native, DisplayOptions.InUse(preset, screen, mode), preset.Key);
                }
            }
            Assert.AreEqual(new DisplaySize(2560, 1440), DisplayOptions.Resolve(new DisplaySize(2560, 1440), screen, WindowMode.Windowed),
                "The saved size comes back with the window.");
        }

        // MENU-17: the game starts in Fullscreen, also from settings saved before (version 1, where Borderless was
        // only the default); a later choice of window mode is kept.
        [Test]
        public void TheGameStartsInFullscreenAndKeepsALaterChoice()
        {
            var key = "test.display." + System.Guid.NewGuid().ToString("N");
            try
            {
                Assert.AreEqual(WindowMode.Fullscreen, new InterfacePreferences(key).WindowMode);
                PlayerPrefs.SetString(key, "{\"Version\":1,\"Scale\":1.25,\"ReducedMotion\":true,\"WindowMode\":0,\"Width\":1600,\"Height\":900}");
                var legacy = new InterfacePreferences(key);
                Assert.AreEqual(WindowMode.Fullscreen, legacy.WindowMode, "Version 1 starts in Fullscreen.");
                Assert.AreEqual(1.25f, legacy.Scale);
                Assert.IsTrue(legacy.ReducedMotion);
                Assert.AreEqual(new DisplaySize(1600, 900), legacy.Resolution);
                legacy.SetWindowMode(WindowMode.Borderless);
                Assert.AreEqual(WindowMode.Borderless, new InterfacePreferences(key).WindowMode, "A chosen mode is kept.");
            }
            finally
            {
                PlayerPrefs.DeleteKey(key);
            }
        }

        // UI-176: the panel expands past 1366x768 on any other proportions instead of shrinking the interface.
        [Test]
        public void TheInterfacePanelExpandsInsteadOfShrinking()
        {
            var panel = UnityEngine.Resources.Load<PanelSettings>("WotrPanel");
            Assert.AreEqual(PanelScaleMode.ScaleWithScreenSize, panel.scaleMode);
            Assert.AreEqual(PanelScreenMatchMode.Expand, panel.screenMatchMode);
            Assert.AreEqual(new Vector2Int(1366, 768), panel.referenceResolution);
        }

        // UI-176: a full-screen game follows its window to another screen; a window keeps its size while it fits.
        [Test]
        public void AFullScreenGameFollowsItsWindowToAnotherScreen()
        {
            var key = "test.display." + System.Guid.NewGuid().ToString("N");
            try
            {
                var preferences = new InterfacePreferences(key);
                preferences.SetWindowMode(WindowMode.Borderless);
                preferences.SetResolution(new DisplaySize(1920, 1080));
                var screen = new FakeScreen { Native = new DisplaySize(3024, 1964) };
                var applied = new List<string>();
                using var controller = new DisplayController(preferences, screen, (size, mode) => applied.Add(size + " " + mode));
                CollectionAssert.AreEqual(new[] { "3024×1964 Borderless" }, applied);
                screen.Native = new DisplaySize(1920, 1080);
                controller.Refresh();
                controller.Refresh();
                CollectionAssert.AreEqual(new[] { "3024×1964 Borderless", "1920×1080 Borderless" }, applied,
                    "On another screen the full screen takes that screen's resolution, applied once.");
                preferences.SetWindowMode(WindowMode.Windowed);
                preferences.SetResolution(new DisplaySize(1600, 900));
                applied.Clear();
                screen.Native = new DisplaySize(2560, 1440);
                controller.Refresh();
                Assert.IsEmpty(applied, "A window that still fits keeps its size on another screen.");
                screen.Native = new DisplaySize(1366, 768);
                controller.Refresh();
                CollectionAssert.AreEqual(new[] { "1366×768 Windowed" }, applied, "A window too large for the new screen takes its size.");
            }
            finally
            {
                UnityEngine.PlayerPrefs.DeleteKey(key);
            }
        }

        [Test]
        public void AnUnsupportedOrNativeChoiceResolvesToTheScreensOwnSize()
        {
            var screen = new FakeScreen { Native = new DisplaySize(1366, 768) };
            Assert.AreEqual(new DisplaySize(1280, 720), DisplayOptions.Resolve(new DisplaySize(1280, 720), screen, WindowMode.Windowed));
            Assert.AreEqual(screen.Native, DisplayOptions.Resolve(new DisplaySize(1920, 1080), screen, WindowMode.Windowed));
            Assert.AreEqual(screen.Native, DisplayOptions.Resolve(DisplaySize.Native, screen, WindowMode.Borderless));
        }

        [Test]
        public void TheSettingsPanelGreysPresetsInPlaceAndMarksTheModeAndResolutionInUse()
        {
            var key = "test.display." + System.Guid.NewGuid().ToString("N");
            try
            {
                var preferences = new InterfacePreferences(key);
                var screen = new FakeScreen { Native = new DisplaySize(1920, 1080) };
                var panel = new InterfaceSettingsPanel(preferences, null, screen);
                var names = panel.Root.Q("resolutions").Children().Select(button => button.name).ToList();
                CollectionAssert.AreEqual(new[] { "resolution-native", "resolution-1280x720", "resolution-1366x768", "resolution-1600x900",
                    "resolution-1920x1080", "resolution-2560x1440", "resolution-3840x2160" }, names);
                Assert.IsTrue(panel.Root.Q("window-mode-fullscreen").ClassListContains("selected"), "MENU-17: Fullscreen by default.");
                Assert.IsTrue(names.All(name => !panel.Root.Q<Button>(name).enabledSelf), "MENU-17: a full screen picks no resolution.");
                Assert.IsTrue(panel.Root.Q("resolution-native").ClassListContains("selected"), "The screen's own resolution is in use.");
                preferences.SetWindowMode(WindowMode.Windowed);
                Assert.IsFalse(panel.Root.Q<Button>("resolution-2560x1440").enabledSelf);
                Assert.IsTrue(panel.Root.Q<Button>("resolution-1600x900").enabledSelf);
                preferences.SetResolution(new DisplaySize(1600, 900));
                Assert.IsTrue(panel.Root.Q("resolution-1600x900").ClassListContains("selected"));
                StringAssert.EndsWith("✓", panel.Root.Q<Button>("resolution-1600x900").text);
                Assert.IsFalse(panel.Root.Q("resolution-native").ClassListContains("selected"));
                Assert.IsTrue(panel.Root.Q("window-mode-windowed").ClassListContains("selected"));
                screen.Native = new DisplaySize(1366, 768);
                panel.Show();
                Assert.IsFalse(panel.Root.Q<Button>("resolution-1600x900").enabledSelf);
                Assert.IsTrue(panel.Root.Q("resolution-native").ClassListContains("selected"), "A saved size this screen cannot show falls back to Native.");
                panel.Dispose();
            }
            finally
            {
                UnityEngine.PlayerPrefs.DeleteKey(key);
            }
        }
    }
}
