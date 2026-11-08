using System;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;

namespace Wotr.Tests
{
    public class InterfacePreferencesTests
    {
        private string key;

        [SetUp]
        public void SetUp() => key = "test.interface." + Guid.NewGuid().ToString("N");

        [TearDown]
        public void TearDown() => PlayerPrefs.DeleteKey(key);

        [TestCase("not json")]
        [TestCase("{}")]
        [TestCase("{\"Version\":5,\"Scale\":1.25,\"ReducedMotion\":true}")]
        [TestCase("{\"Version\":1,\"Scale\":99}")]
        public void UnknownOrInvalidSettingsKeepReadableDefaults(string saved)
        {
            PlayerPrefs.SetString(key, saved);
            var preferences = new InterfacePreferences(key);
            Assert.AreEqual(1f, preferences.Scale);
            Assert.IsFalse(preferences.ReducedMotion);
            Assert.AreEqual(WindowMode.Fullscreen, preferences.WindowMode, "MENU-17: Fullscreen by default.");
            Assert.AreEqual(DisplaySize.Native, preferences.Resolution);
        }

        [TestCase("{\"Version\":1,\"Scale\":1.25,\"ReducedMotion\":true}")]
        [TestCase("{\"Version\":1,\"Scale\":1.25,\"ReducedMotion\":true,\"WindowMode\":7,\"Width\":1000,\"Height\":700}")]
        public void SettingsWithoutAValidDisplayKeepTheirSizeAndUseTheScreensOwn(string saved)
        {
            PlayerPrefs.SetString(key, saved);
            var preferences = new InterfacePreferences(key);
            Assert.AreEqual(1.25f, preferences.Scale);
            Assert.IsTrue(preferences.ReducedMotion);
            Assert.AreEqual(WindowMode.Fullscreen, preferences.WindowMode, "MENU-17: Fullscreen by default.");
            Assert.AreEqual(DisplaySize.Native, preferences.Resolution);
        }

        // AUDIO-03: version 3 keeps the sound settings, a silence included; older settings keep everything else and
        // take the sound defaults; a level outside 0–100% falls back to its default.
        [Test]
        public void SoundSettingsSurviveReloadIncludingSilenceAndOlderSettingsTakeTheDefaults()
        {
            var preferences = new InterfacePreferences(key);
            Assert.IsTrue(preferences.SoundEnabled);
            Assert.AreEqual(0.4f, preferences.EffectsVolume);
            Assert.AreEqual(0.6f, preferences.AlertsVolume);
            Assert.IsTrue(preferences.BackgroundAlerts);
            var changes = 0;
            preferences.Changed += () => changes++;
            preferences.SetEffectsVolume(0);
            preferences.SetAlertsVolume(0.43f);
            preferences.SetSoundEnabled(false);
            preferences.SetBackgroundAlerts(false);
            preferences.SetBackgroundAlerts(false);
            Assert.AreEqual(4, changes, "Only actual changes notify.");
            Assert.AreEqual(0.45f, preferences.AlertsVolume, 0.0001f, "Levels move in 5% steps.");
            var reloaded = new InterfacePreferences(key);
            Assert.AreEqual(0f, reloaded.EffectsVolume, "A saved 0% stays 0%.");
            Assert.AreEqual(0.45f, reloaded.AlertsVolume, 0.0001f);
            Assert.IsFalse(reloaded.SoundEnabled, "A switched-off sound stays off.");
            Assert.IsFalse(reloaded.BackgroundAlerts);
            Assert.IsFalse(reloaded.SoundLevels.Enabled);
            PlayerPrefs.SetString(key, "{\"Version\":2,\"Scale\":1.25,\"WindowMode\":2}");
            var older = new InterfacePreferences(key);
            Assert.AreEqual(1.25f, older.Scale);
            Assert.AreEqual(WindowMode.Windowed, older.WindowMode, "Version 2 keeps its window mode.");
            Assert.IsTrue(older.SoundEnabled);
            Assert.AreEqual(0.4f, older.EffectsVolume);
            Assert.AreEqual(0.6f, older.AlertsVolume);
            PlayerPrefs.SetString(key, "{\"Version\":3,\"SoundEnabled\":true,\"EffectsVolume\":7,\"AlertsVolume\":-1}");
            var invalid = new InterfacePreferences(key);
            Assert.AreEqual(0.4f, invalid.EffectsVolume);
            Assert.AreEqual(0.6f, invalid.AlertsVolume);
        }

        [Test]
        public void WindowModeAndResolutionSurviveReloadAndRejectSizesOutsideTheList()
        {
            var preferences = new InterfacePreferences(key);
            var changes = 0;
            preferences.Changed += () => changes++;
            preferences.SetWindowMode(WindowMode.Windowed);
            preferences.SetResolution(new DisplaySize(1366, 768));
            preferences.SetResolution(new DisplaySize(1366, 768));
            Assert.AreEqual(2, changes);
            var restored = new InterfacePreferences(key);
            Assert.AreEqual(WindowMode.Windowed, restored.WindowMode);
            Assert.AreEqual(new DisplaySize(1366, 768), restored.Resolution);
            Assert.Throws<ArgumentOutOfRangeException>(() => preferences.SetResolution(new DisplaySize(1000, 700)));
            Assert.Throws<ArgumentOutOfRangeException>(() => preferences.SetWindowMode((WindowMode)7));
            Assert.AreEqual(new DisplaySize(1366, 768), preferences.Resolution);
        }

        [Test]
        public void SizeAndMotionSurviveReloadAndOnlyActualChangesNotify()
        {
            var preferences = new InterfacePreferences(key);
            var changes = 0;
            preferences.Changed += () => changes++;
            preferences.SetScale(1.25f);
            preferences.SetReducedMotion(true);
            preferences.SetScale(1.25f);
            preferences.SetReducedMotion(true);
            Assert.AreEqual(2, changes);
            var restored = new InterfacePreferences(key);
            Assert.AreEqual(1.25f, restored.Scale);
            Assert.IsTrue(restored.ReducedMotion);
            Assert.Throws<ArgumentOutOfRangeException>(() => preferences.SetScale(.5f));
            Assert.AreEqual(1.25f, preferences.Scale);
        }

        // UI-529 (owner, 2026-11-08): the figures' size starts at 150 % (the owner's default) and goes down to the
        // standard; a choice survives a reload and notifies once; a size off the list is refused; an older save, even one
        // a version-3 build wrote with 100 %, reads the default.
        [Test]
        public void FigureSizeStartsAtTheDefaultSurvivesReloadAndRefusesOtherSizes()
        {
            var preferences = new InterfacePreferences(key);
            Assert.AreEqual(1.5f, preferences.FigureSize, "150 % is the default.");
            CollectionAssert.AreEqual(new[] { 1f, 1.25f, 1.5f, 1.75f }, InterfacePreferences.FigureSizes, "The standard is the smallest.");
            var changes = 0;
            preferences.Changed += () => changes++;
            preferences.SetFigureSize(1f);
            preferences.SetFigureSize(1f);
            Assert.AreEqual(1, changes);
            Assert.AreEqual(1f, new InterfacePreferences(key).FigureSize, "The standard survives a reload.");
            Assert.Throws<ArgumentOutOfRangeException>(() => preferences.SetFigureSize(.75f));
            Assert.Throws<ArgumentOutOfRangeException>(() => preferences.SetFigureSize(3f));
            Assert.AreEqual(1f, preferences.FigureSize);
            PlayerPrefs.SetString(key, "{\"Version\":3,\"Scale\":1.25,\"FigureSize\":1}");
            var older = new InterfacePreferences(key);
            Assert.AreEqual(1.5f, older.FigureSize, "A version-3 save reads the default.");
            Assert.AreEqual(1.25f, older.Scale, "and keeps the rest.");
            PlayerPrefs.SetString(key, "{\"Version\":4,\"FigureSize\":9}");
            Assert.AreEqual(1.5f, new InterfacePreferences(key).FigureSize, "A size off the list reads the default.");
        }
    }
}
