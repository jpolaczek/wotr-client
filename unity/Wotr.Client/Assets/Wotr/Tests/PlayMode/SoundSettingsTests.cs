using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    // AUDIO-03 (AU03): the Sound section of Settings changes the levels at once, keeps them, greys the levels with the
    // sound off, plays nothing while a slider moves and plays its test on purpose at the current levels.
    public sealed class SoundSettingsTests
    {
        [UnityTest]
        public IEnumerator SoundSettingsApplyAtOnceAndOnlyTheTestPlays()
        {
            using var client = new MountedMatchClient("http://localhost", "sound-settings");
            var sounds = client.Interface.Sounds;
            var played = new List<SoundCue>();
            sounds.Policy.Played += played.Add;
            client.Interface.Open();
            yield return null;
            var settings = client.Interface.Settings.Root;
            Assert.AreEqual(0.4f, sounds.Policy.Levels.Effects);
            Assert.AreEqual(0.6f, sounds.Policy.Levels.Alerts);
            Assert.AreEqual("40%", settings.Q<Label>("effects-volume-value").text);
            settings.Q<SliderInt>("effects-volume").value = 13;
            settings.Q<SliderInt>("alerts-volume").value = 20;
            yield return null;
            Assert.AreEqual(0.65f, client.Interface.Preferences.EffectsVolume, 0.0001f);
            Assert.AreEqual(1f, sounds.Policy.Levels.Alerts, 0.0001f);
            Assert.AreEqual("65%", settings.Q<Label>("effects-volume-value").text);
            Assert.IsEmpty(played, "Moving a slider plays nothing.");
            PointerGestureEvents.ClickAt(settings.Q<Button>("test-sound"));
            for (var i = 0; i < 30; i++)
            {
                sounds.Update();
                yield return null;
            }
            CollectionAssert.AreEqual(new[] { SoundCue.ActionAccepted, SoundCue.DecisionRequired }, played, "One effect, then one alert.");
            Assert.AreEqual(1f, sounds.CurrentSource.volume, 0.0001f, "The alert at the Alerts level.");
            settings.Q<Toggle>("sound-enabled").value = false;
            yield return null;
            Assert.IsFalse(sounds.Policy.Levels.Enabled);
            Assert.IsFalse(settings.Q<SliderInt>("effects-volume").enabledSelf, "The levels keep their place, greyed.");
            Assert.IsFalse(settings.Q<Button>("test-sound").enabledSelf);
            sounds.Request(new SoundRequest(SoundCue.DecisionRequired, "after-mute"));
            Assert.AreEqual(2, played.Count, "Nothing sounds with the sound off.");
            var reopened = new InterfacePreferences(client.PreferencesKey);
            Assert.IsFalse(reopened.SoundEnabled, "The choice survives a restart.");
            Assert.AreEqual(0.65f, reopened.EffectsVolume, 0.0001f);
        }
    }
}
