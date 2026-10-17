using System;
using UnityEngine;

namespace Wotr.Client
{
    // UI-218: the map view the player chose (normal or political), kept on this device. It is separate from the
    // interface settings, whose change re-applies the display.
    public sealed class MapViewPreference
    {
        private const int CurrentVersion = 1;
        private readonly string key;
        public bool Political { get; private set; }

        public MapViewPreference(string key = "wotr.map-view.v1")
        {
            this.key = key;
            try
            {
                var saved = JsonUtility.FromJson<Settings>(PlayerPrefs.GetString(key, "{}"));
                if (saved?.Version == CurrentVersion)
                    Political = saved.Political;
            }
            catch (ArgumentException) { }
        }

        public void SetPolitical(bool value)
        {
            if (Political == value)
                return;
            Political = value;
            PlayerPrefs.SetString(key, JsonUtility.ToJson(new Settings { Version = CurrentVersion, Political = value }));
            PlayerPrefs.Save();
        }

        [Serializable]
        private sealed class Settings
        {
            public int Version;
            public bool Political;
        }
    }
}
