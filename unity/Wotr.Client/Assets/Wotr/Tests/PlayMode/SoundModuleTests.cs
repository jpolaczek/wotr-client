using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Wotr.Client;

namespace Wotr.PlayTests
{
    // AUDIO-02: the real catalogue and Unity output behind the policy; muting stops at once; every interface has one
    // sound owner, gone with it.
    public sealed class SoundModuleTests
    {
        [UnityTest]
        public IEnumerator TheModulePlaysCatalogueClipsAndMutingStopsThem()
        {
            using var module = new SoundModule();
            CollectionAssert.AreEquivalent(System.Enum.GetValues(typeof(SoundCue)).Cast<SoundCue>(), module.Catalog.Cues);
            var played = 0;
            module.Policy.Played += _ => played++;
            module.Request(new SoundRequest(SoundCue.UiClick));
            yield return null;
            Assert.AreEqual(1, played);
            Assert.AreEqual("ui-click-1", module.CurrentSource.clip.name);
            Assert.AreEqual(0.4f, module.CurrentSource.volume, 0.001f, "Effects at 40%.");
            module.Request(new SoundRequest(SoundCue.BattleStarted, "battle-1"));
            Assert.AreEqual("battle-started-1", module.CurrentSource.clip.name);
            Assert.AreEqual(0.6f, module.CurrentSource.volume, 0.001f, "Alerts at 60%.");
            module.Policy.Levels = new SoundLevels(false, 0.4f, 0.6f, true);
            for (var i = 0; i < 10; i++)
            {
                module.Update();
                yield return null;
            }
            Assert.IsFalse(module.CurrentSource.isPlaying && module.CurrentSource.volume > 0, "Muting fades the clip out within 40 ms.");
            module.Request(new SoundRequest(SoundCue.DecisionRequired, "decision-1"));
            Assert.AreEqual(2, played, "Nothing sounds while muted.");
        }

        [UnityTest]
        public IEnumerator EveryInterfaceHasOneSoundOwnerGoneWithIt()
        {
            int Owners() => Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Count(s => s.gameObject.name == "WoTR Sounds") / 2;
            var before = Owners();
            using (var client = new MountedMatchClient("http://localhost", "sound-owner"))
            {
                yield return null;
                Assert.AreEqual(before + 1, Owners());
                Assert.IsNotNull(client.Interface.Sounds);
            }
            yield return null;
            Assert.AreEqual(before, Owners(), "Closing the interface removes its sound owner.");
        }
    }
}
