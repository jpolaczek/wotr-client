namespace Wotr.Client
{
    // UI-305: a beat that only lasts (a dwell, an emphasis); what it shows stays with its presenter.
    public sealed class TimedBeat : IPresentationBeat
    {
        private readonly double end;

        public TimedBeat(double now, double seconds)
        {
            end = now + seconds;
        }

        public bool Advance(double now) => now >= end;

        public void Finish() { }
    }
}
