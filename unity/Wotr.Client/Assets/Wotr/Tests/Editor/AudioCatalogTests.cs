using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Wotr.EditorTests
{
    // AUDIO-01 (AU01): the local sound catalogue has every cue of AUDIO_UX on its channel, and every clip imports as a
    // short mono effect within its length, with silent edges, no clipping and settings that play without delay.
    public sealed class AudioCatalogTests
    {
        private static readonly (string Cue, string Channel)[] Expected =
        {
            ("UiClick", "Effects"), ("CardInspect", "Effects"), ("CardMove", "Effects"), ("DiceRoll", "Effects"),
            ("ActionAccepted", "Effects"), ("BattleStarted", "Alerts"), ("FellowshipAdvanced", "Effects"),
            ("CorruptionChanged", "Effects"), ("DecisionRequired", "Alerts"), ("ActionFailed", "Alerts"),
            ("Victory", "Alerts"), ("Defeat", "Alerts"), ("PoliticsMoved", "Effects")
        };

        private static JObject Cues() => (JObject)JObject.Parse(Resources.Load<TextAsset>("Wotr/Audio/audio-catalog").text)["cues"];

        [Test]
        public void TheCatalogueHasEveryCueOnItsChannel()
        {
            var cues = Cues();
            CollectionAssert.AreEquivalent(Expected.Select(e => e.Cue), cues.Properties().Select(p => p.Name));
            foreach (var (cue, channel) in Expected)
            {
                Assert.AreEqual(channel, cues[cue].Value<string>("channel"), cue);
                var clips = cues[cue]["clips"].Values<string>().ToList();
                Assert.That(clips.Count, Is.InRange(1, 3), cue);
            }
            // The two track sounds play one after the other in at most 900 ms (AUDIO_UX).
            var pair = cues["FellowshipAdvanced"].Value<float>("longestSeconds") + cues["CorruptionChanged"].Value<float>("longestSeconds");
            Assert.LessOrEqual(pair, 0.9f);
            // AUDIO-12: the Politics bell rings through the token's 0.7 s slide and ends before the beat's hold does.
            Assert.That(cues["PoliticsMoved"].Value<float>("longestSeconds"), Is.InRange(1.2f, 1.8f));
        }

        [Test]
        public void EveryClipImportsAsAShortCleanMonoEffect()
        {
            foreach (var property in Cues().Properties())
            {
                var longest = property.Value.Value<float>("longestSeconds");
                foreach (var path in property.Value["clips"].Values<string>())
                {
                    var clip = Resources.Load<AudioClip>(path);
                    Assert.IsNotNull(clip, path);
                    Assert.AreEqual(1, clip.channels, path);
                    Assert.AreEqual(48000, clip.frequency, path);
                    Assert.LessOrEqual(clip.length, longest + 0.001f, path);
                    Assert.Greater(clip.samples, 0, path);
                    var samples = new float[clip.samples];
                    Assert.IsTrue(clip.GetData(samples, 0), path);
                    var peak = samples.Max(Mathf.Abs);
                    Assert.That(peak, Is.InRange(0.05f, 0.9f), path + " has a sound and headroom");
                    Assert.Less(Mathf.Abs(samples[0]), 0.001f, path + " starts in silence");
                    Assert.Less(Mathf.Abs(samples[samples.Length - 1]), 0.001f, path + " ends in silence");
                    var importer = (AudioImporter)AssetImporter.GetAtPath("Assets/Wotr/Resources/" + path + ".wav");
                    Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, importer.defaultSampleSettings.loadType, path);
                    Assert.AreEqual(AudioCompressionFormat.PCM, importer.defaultSampleSettings.compressionFormat, path);
                    Assert.IsTrue(importer.defaultSampleSettings.preloadAudioData, path + " plays without a first-time delay");
                }
            }
        }
    }
}
