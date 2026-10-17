using System;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // AUDIO-03: the Sound part of Settings — the main switch, the Effects and Alerts levels in 5% steps, the alerts
    // heard without focus and a test the player starts on purpose (moving a slider plays nothing). Every control keeps
    // its place; with the sound off the levels are greyed.
    public sealed class SoundSettingsSection
    {
        public VisualElement Root { get; } = new VisualElement { name = "sound-settings" };
        private readonly InterfacePreferences preferences;
        private readonly Toggle sound = new Toggle("Sound") { name = "sound-enabled" };
        private readonly Toggle background = new Toggle("Background alerts") { name = "background-alerts" };
        private readonly SliderInt effects;
        private readonly SliderInt alerts;
        private readonly Label effectsValue = new Label { name = "effects-volume-value" };
        private readonly Label alertsValue = new Label { name = "alerts-volume-value" };
        private readonly Button test;

        public SoundSettingsSection(InterfacePreferences preferences, Action testSound)
        {
            this.preferences = preferences;
            Root.AddToClassList("sound-settings");
            Root.Add(new Label("Sound") { name = "sound-heading" });
            sound.RegisterValueChangedCallback(e => preferences.SetSoundEnabled(e.newValue));
            Root.Add(sound);
            effects = Level("effects-volume", "Effects", effectsValue, v => preferences.SetEffectsVolume(v));
            alerts = Level("alerts-volume", "Alerts", alertsValue, v => preferences.SetAlertsVolume(v));
            background.RegisterValueChangedCallback(e => preferences.SetBackgroundAlerts(e.newValue));
            background.tooltip = "Without focus, only a decision you must make and the end of your game";
            Root.Add(background);
            test = BoardScreen.Button("Test sound", () => testSound?.Invoke());
            test.name = "test-sound";
            test.tooltip = "Plays one effect and one alert at the levels above";
            Root.Add(test);
            Render();
        }

        private SliderInt Level(string name, string text, Label value, Action<float> set)
        {
            var row = BoardScreen.Row("sound-level");
            var label = new Label(text) { name = name + "-label" };
            label.AddToClassList("sound-level-label");
            var slider = new SliderInt(0, 20) { name = name, pageSize = 1 };
            slider.AddToClassList("sound-level-slider");
            slider.RegisterValueChangedCallback(e => set(e.newValue / 20f));
            value.AddToClassList("sound-level-value");
            row.Add(label);
            row.Add(slider);
            row.Add(value);
            Root.Add(row);
            return slider;
        }

        public void Render()
        {
            sound.SetValueWithoutNotify(preferences.SoundEnabled);
            background.SetValueWithoutNotify(preferences.BackgroundAlerts);
            effects.SetValueWithoutNotify(Steps(preferences.EffectsVolume));
            alerts.SetValueWithoutNotify(Steps(preferences.AlertsVolume));
            effectsValue.text = Percent(preferences.EffectsVolume);
            alertsValue.text = Percent(preferences.AlertsVolume);
            foreach (var control in new VisualElement[] { effects, alerts, background, test })
                control.SetEnabled(preferences.SoundEnabled);
        }

        private static int Steps(float volume) => (int)Math.Round(volume * 20);

        private static string Percent(float volume) => Math.Round(volume * 100) + "%";
    }
}
