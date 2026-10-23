using NUnit.Framework;
using UnityEngine;

namespace Wotr.PlayTests
{
    // PlayMode runs are silent: the game's sounds still play at their own volumes, so the audio tests keep their checks,
    // but the listener's master volume is zero for the whole run and comes back afterwards.
    [SetUpFixture]
    public sealed class SilentTestRun
    {
        private float volume;

        [OneTimeSetUp]
        public void Mute()
        {
            volume = AudioListener.volume;
            AudioListener.volume = 0;
        }

        [OneTimeTearDown]
        public void Restore() => AudioListener.volume = volume;
    }
}
