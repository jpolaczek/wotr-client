using System;
using UnityEngine;

namespace Wotr.Client
{
    public sealed class InterfacePreferences
    {
        // MENU-17: version 2 starts settings of version 1 in Fullscreen. AUDIO-03: version 3 adds the sound settings;
        // older settings keep everything else and take the sound defaults.
        private const int CurrentVersion = 3;
        public const float DefaultEffects = 0.4f;
        public const float DefaultAlerts = 0.6f;
        private readonly string key;
        public float Scale { get; private set; } = 1f;
        public bool ReducedMotion { get; private set; }
        // MENU-17: the game starts in Fullscreen.
        public WindowMode WindowMode { get; private set; } = WindowMode.Fullscreen;
        public DisplaySize Resolution { get; private set; } = DisplaySize.Native;
        public bool SoundEnabled { get; private set; } = true;
        public float EffectsVolume { get; private set; } = DefaultEffects;
        public float AlertsVolume { get; private set; } = DefaultAlerts;
        public bool BackgroundAlerts { get; private set; } = true;
        public event Action Changed;

        public SoundLevels SoundLevels => new SoundLevels(SoundEnabled, EffectsVolume, AlertsVolume, BackgroundAlerts);

        public InterfacePreferences(string key = "wotr.interface.v1")
        {
            this.key = key;
            try
            {
                var saved = JsonUtility.FromJson<Settings>(PlayerPrefs.GetString(key, "{}"));
                if (saved?.Version is not (1 or 2 or CurrentVersion))
                    return;
                Scale = ValidScale(saved.Scale) ? saved.Scale : 1f;
                ReducedMotion = saved.ReducedMotion;
                // MENU-12 added the display to version 1, where Borderless was only the default; MENU-17 (version 2)
                // starts those settings in Fullscreen and keeps a later choice of window mode.
                if (saved.Version >= 2 && ValidMode(saved.WindowMode))
                    WindowMode = (WindowMode)saved.WindowMode;
                var resolution = new DisplaySize(saved.Width, saved.Height);
                Resolution = DisplayOptions.IsPreset(resolution) ? resolution : DisplaySize.Native;
                if (saved.Version == CurrentVersion)
                    ReadSound(saved);
            }
            catch (ArgumentException) { }
        }

        public void SetScale(float value)
        {
            if (!ValidScale(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            if (Scale == value)
                return;
            Scale = value;
            Save();
        }

        public void SetReducedMotion(bool value)
        {
            if (ReducedMotion == value)
                return;
            ReducedMotion = value;
            Save();
        }

        public void SetWindowMode(WindowMode value)
        {
            if (!ValidMode((int)value))
                throw new ArgumentOutOfRangeException(nameof(value));
            if (WindowMode == value)
                return;
            WindowMode = value;
            Save();
        }

        public void SetResolution(DisplaySize value)
        {
            if (!DisplayOptions.IsPreset(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            if (Resolution.Equals(value))
                return;
            Resolution = value;
            Save();
        }

        // A saved 0% or a switched-off sound stays as saved; only a value outside 0–100% falls back to the default.
        private void ReadSound(Settings saved)
        {
            SoundEnabled = saved.SoundEnabled;
            EffectsVolume = ValidVolume(saved.EffectsVolume) ? saved.EffectsVolume : DefaultEffects;
            AlertsVolume = ValidVolume(saved.AlertsVolume) ? saved.AlertsVolume : DefaultAlerts;
            BackgroundAlerts = saved.BackgroundAlerts;
        }

        public void SetSoundEnabled(bool value)
        {
            if (SoundEnabled == value)
                return;
            SoundEnabled = value;
            Save();
        }

        public void SetEffectsVolume(float value)
        {
            value = Volume(value);
            if (EffectsVolume == value)
                return;
            EffectsVolume = value;
            Save();
        }

        public void SetAlertsVolume(float value)
        {
            value = Volume(value);
            if (AlertsVolume == value)
                return;
            AlertsVolume = value;
            Save();
        }

        public void SetBackgroundAlerts(bool value)
        {
            if (BackgroundAlerts == value)
                return;
            BackgroundAlerts = value;
            Save();
        }

        // Volumes move in 5% steps between 0 and 100%.
        private static float Volume(float value)
        {
            if (float.IsNaN(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            return Mathf.Round(Mathf.Clamp01(value) * 20) / 20;
        }

        private static bool ValidVolume(float value) => !float.IsNaN(value) && value >= 0 && value <= 1;

        private static bool ValidScale(float value) => value == 1f || value == 1.25f;

        private static bool ValidMode(int value) => Enum.IsDefined(typeof(WindowMode), value);

        private void Save()
        {
            var settings = new Settings
            {
                Version = CurrentVersion,
                Scale = Scale,
                ReducedMotion = ReducedMotion,
                WindowMode = (int)WindowMode,
                Width = Resolution.Width,
                Height = Resolution.Height,
                SoundEnabled = SoundEnabled,
                EffectsVolume = EffectsVolume,
                AlertsVolume = AlertsVolume,
                BackgroundAlerts = BackgroundAlerts
            };
            PlayerPrefs.SetString(key, JsonUtility.ToJson(settings));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        [Serializable]
        private sealed class Settings
        {
            public int Version;
            public float Scale;
            public bool ReducedMotion;
            public int WindowMode;
            public int Width;
            public int Height;
            public bool SoundEnabled = true;
            public float EffectsVolume = DefaultEffects;
            public float AlertsVolume = DefaultAlerts;
            public bool BackgroundAlerts = true;
        }
    }
}
