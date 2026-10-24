using System;
using System.Collections.Generic;

namespace Wotr.Client
{
    // UI-310: a beat of successive timed steps (a ring, then a track's first stage, then its second). The first step
    // starts with the beat; each next one when the previous one's time is up. Skip or a rebase ends it where it is: what
    // the steps show stays with their presenter, and the release shows the latest state.
    public sealed class StepsBeat : IPresentationBeat
    {
        private readonly IReadOnlyList<(Action Start, double Seconds)> steps;
        private readonly Action end;
        private int next;
        private double due;
        private bool ended;

        public StepsBeat(double now, Action end, IReadOnlyList<(Action Start, double Seconds)> steps)
        {
            this.steps = steps;
            this.end = end;
            due = now;
            Advance(now);
        }

        public bool Advance(double now)
        {
            while (!ended && now >= due)
            {
                if (next == steps.Count)
                {
                    Finish();
                    return true;
                }
                var step = steps[next++];
                step.Start();
                due = now + step.Seconds;
            }
            return ended;
        }

        public void Finish()
        {
            if (ended)
                return;
            ended = true;
            end?.Invoke();
        }
    }
}
