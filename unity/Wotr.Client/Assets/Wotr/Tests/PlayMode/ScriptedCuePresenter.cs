using System;
using System.Collections.Generic;
using Wotr.Client;

namespace Wotr.PlayTests
{
    // UI-304: a stand-in for the family presenters of UI-305–UI-312 — it shows the cues of the given kinds for a fixed
    // time and records what the board presented when each beat began.
    internal sealed class ScriptedCuePresenter : ICuePresenter
    {
        private readonly Dictionary<PresentationCueKind, double> seconds;
        private readonly Action<PresentationCue> began;
        public List<PresentationCue> Begun { get; } = new List<PresentationCue>();
        public int Finished { get; private set; }
        public int Ended { get; private set; }

        public ScriptedCuePresenter(Dictionary<PresentationCueKind, double> seconds, Action<PresentationCue> began = null)
        {
            this.seconds = seconds;
            this.began = began;
        }

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            if (!seconds.TryGetValue(cue.Kind, out var length))
                return null;
            Begun.Add(cue);
            began?.Invoke(cue);
            return new Beat(this, now + length);
        }

        private sealed class Beat : IPresentationBeat
        {
            private readonly ScriptedCuePresenter owner;
            private readonly double end;

            public Beat(ScriptedCuePresenter owner, double end)
            {
                this.owner = owner;
                this.end = end;
            }

            public bool Advance(double now)
            {
                if (now < end)
                    return false;
                owner.Ended++;
                return true;
            }

            // Skip, a rebase or disposal: the beat ends before its time.
            public void Finish() => owner.Finished++;
        }
    }
}
