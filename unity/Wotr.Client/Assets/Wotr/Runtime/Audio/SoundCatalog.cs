using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Wotr.Client
{
    // AUDIO-02: the clips of every cue from Resources/Wotr/Audio/audio-catalog.json (AUDIO-01). A clip loads on first
    // use and stays; a missing clip or catalogue leaves the cue silent instead of stopping the game.
    public sealed class SoundCatalog : ISoundLibrary
    {
        private sealed class Entry
        {
            public string Channel = "Effects";
            public List<string> Clips = new List<string>();
        }

        private sealed class Document
        {
            public Dictionary<string, Entry> Cues = new Dictionary<string, Entry>();
        }

        private readonly Dictionary<SoundCue, Entry> entries = new Dictionary<SoundCue, Entry>();
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

        public SoundCatalog(string resource = "Wotr/Audio/audio-catalog")
        {
            var text = Resources.Load<TextAsset>(resource)?.text;
            if (text == null)
                return;
            foreach (var cue in JsonConvert.DeserializeObject<Document>(text).Cues)
                if (System.Enum.TryParse<SoundCue>(cue.Key, out var parsed))
                    entries[parsed] = cue.Value;
        }

        public IEnumerable<SoundCue> Cues => entries.Keys;

        public int Variants(SoundCue cue) => entries.TryGetValue(cue, out var entry) ? entry.Clips.Count : 0;

        public float Length(SoundCue cue, int variant) => Clip(cue, variant)?.length ?? 0;

        public SoundChannel Channel(SoundCue cue) =>
            entries.TryGetValue(cue, out var entry) && entry.Channel == nameof(SoundChannel.Alerts) ? SoundChannel.Alerts : SoundChannel.Effects;

        public AudioClip Clip(SoundCue cue, int variant)
        {
            if (!entries.TryGetValue(cue, out var entry) || variant < 0 || variant >= entry.Clips.Count)
                return null;
            var path = entry.Clips[variant];
            if (!clips.TryGetValue(path, out var clip))
                clips[path] = clip = Resources.Load<AudioClip>(path);
            return clip;
        }
    }
}
