using System;
using UnityEngine;

namespace Wotr.Client
{
    // AUDIO-02: the one sound owner of the application (InterfaceController): the catalogue, the policy and the
    // Unity output. Game controllers ask it for cues; it never waits for a clip and never blocks the game.
    public sealed class SoundModule : IDisposable
    {
        private readonly UnitySoundOutput output;

        public SoundCatalog Catalog { get; }
        public SoundPolicy Policy { get; }
        // AUDIO-04: what the open game tells the gesture sounds; empty outside a game.
        public int CommandsSent { get; private set; }
        public Func<string> CardLook { get; set; }
        public Func<bool> GestureIgnored { get; set; }

        public SoundModule()
        {
            Catalog = new SoundCatalog();
            output = new UnitySoundOutput(Catalog);
            Policy = new SoundPolicy(Catalog, output, () => Time.unscaledTimeAsDouble);
            // AUDIO-03: losing focus (also by minimising) leaves only the background alerts. The game starts focused;
            // the editor and batch runs report no change.
            Application.focusChanged += Focus;
        }

        private void Focus(bool focused) => Policy.Focused = focused;

        public void CommandSent() => CommandsSent++;

        // Settings' Test sound: one effect, then one alert, at the current levels; never part of a game.
        public void TestSound() => Policy.Sequence(new SoundSequence("",
            new[] { (SoundCue.ActionAccepted, 0.0), (SoundCue.DecisionRequired, 0.3) }));

        public AudioSource CurrentSource => output.Current;

        public void Request(SoundRequest request) => Policy.Request(request);

        public void Update() => Policy.Update(Time.unscaledDeltaTime);

        public void Dispose()
        {
            Application.focusChanged -= Focus;
            Policy.Silence();
            output.Dispose();
        }
    }
}
