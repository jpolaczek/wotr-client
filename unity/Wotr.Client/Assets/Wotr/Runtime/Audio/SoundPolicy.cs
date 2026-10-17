using System;
using System.Collections.Generic;
using System.Linq;

namespace Wotr.Client
{
    // AUDIO-02: decides which cue sounds, on a clock it is given (so tests can move time):
    // - an event key sounds once per game, and is remembered even when muted or in the background, so nothing is
    //   caught up later;
    // - the mute and background filters run before the priorities, so a filtered battle never hides the allowed
    //   decision;
    // - one resolution plays its most important cue only;
    // - one clip at a time: a more important cue fades out a less important one, anything else is dropped, never
    //   queued; clicks repeat at most every 100 ms and card looks every 250 ms;
    // - a sequence (AUDIO-06's two tracks) plays every stage at its time regardless of those limits, then at most
    //   one important cue; a newer sequence, a new game or Cancel drop what is left of it.
    public sealed class SoundPolicy
    {
        private const int Remembered = 512;
        private readonly ISoundLibrary library;
        private readonly ISoundOutput output;
        private readonly Func<double> clock;
        private readonly HashSet<string> heard = new HashSet<string>();
        private readonly Queue<string> heardOrder = new Queue<string>();
        private readonly Dictionary<SoundCue, double> lastStarted = new Dictionary<SoundCue, double>();
        private readonly Dictionary<SoundCue, int> nextVariant = new Dictionary<SoundCue, int>();
        private readonly List<(SoundCue Cue, double At, bool Staged)> scheduled = new List<(SoundCue, double, bool)>();
        private SoundCue? current;
        private double currentEnds;
        private SoundLevels levels = SoundLevels.Default;
        private bool focused = true;

        public event Action<SoundCue> Played;

        public SoundPolicy(ISoundLibrary library, ISoundOutput output, Func<double> clock)
        {
            this.library = library;
            this.output = output;
            this.clock = clock;
        }

        public SoundLevels Levels
        {
            get => levels;
            set
            {
                levels = value;
                if (!value.Enabled)
                    Silence();
            }
        }

        // Losing focus silences what plays; the cues still due are filtered when they come up.
        public bool Focused
        {
            get => focused;
            set
            {
                if (focused && !value && current.HasValue && !SoundCues.HeardInBackground(current.Value))
                    Silence();
                focused = value;
            }
        }

        public bool Sequencing => scheduled.Any(s => s.Staged);

        // A gesture or a single event.
        public void Request(SoundRequest request) => Resolve(new[] { request });

        // Everything one resolution produced: its most important new, allowed cue.
        public void Resolve(IReadOnlyCollection<SoundRequest> requests)
        {
            var fresh = requests.Where(r => Remember(r.Key)).ToList();
            var allowed = fresh.Where(r => Allowed(r.Cue)).ToList();
            if (allowed.Count == 0)
                return;
            var best = allowed.OrderByDescending(r => SoundCues.Priority(r.Cue)).First();
            Start(best.Cue, false);
        }

        // AUDIO-06: the two tracks of one effect, each stage at its own moment, then one important cue.
        public void Sequence(SoundSequence sequence)
        {
            if (!Remember(sequence.Key))
                return;
            var trailing = sequence.Trailing.HasValue && Remember(sequence.Trailing.Value.Key);
            CancelSequence();
            var start = clock();
            foreach (var (cue, at) in sequence.Stages)
                scheduled.Add((cue, start + at, true));
            if (trailing)
                scheduled.Add((sequence.Trailing.Value.Cue, start + sequence.TrailingAt, false));
            Update(0);
        }

        // Plays what has come due, then lets the output fade.
        public void Update(float seconds)
        {
            var now = clock();
            foreach (var due in scheduled.Where(s => s.At <= now).ToList())
            {
                scheduled.Remove(due);
                if (Allowed(due.Cue))
                    Start(due.Cue, due.Staged);
            }
            output.Update(seconds);
        }

        // A newer state, a reconnect or leaving the board: the rest of a sequence no longer belongs to the screen.
        public void CancelSequence() => scheduled.Clear();

        // A new game starts with nothing heard and nothing pending.
        public void BeginGame()
        {
            heard.Clear();
            heardOrder.Clear();
            CancelSequence();
        }

        public void Silence()
        {
            output.Stop();
            current = null;
        }

        private bool Remember(string key)
        {
            if (key.Length == 0)
                return true;
            if (!heard.Add(key))
                return false;
            heardOrder.Enqueue(key);
            if (heardOrder.Count > Remembered)
                heard.Remove(heardOrder.Dequeue());
            return true;
        }

        private bool Allowed(SoundCue cue) =>
            levels.Enabled && (focused || levels.BackgroundAlerts && SoundCues.HeardInBackground(cue));

        private void Start(SoundCue cue, bool staged)
        {
            var now = clock();
            if (!staged && TooSoon(cue, now))
                return;
            if (!staged && current.HasValue && now < currentEnds && SoundCues.Priority(cue) < SoundCues.Priority(current.Value))
                return;
            var variant = NextVariant(cue);
            output.Play(cue, variant, Volume(cue));
            current = cue;
            currentEnds = now + library.Length(cue, variant);
            lastStarted[cue] = now;
            Played?.Invoke(cue);
        }

        private bool TooSoon(SoundCue cue, double now) =>
            lastStarted.TryGetValue(cue, out var last) && now - last < SoundCues.ShortestRepeat(cue);

        // Variants take turns, so a repeated effect never repeats the same clip twice in a row.
        private int NextVariant(SoundCue cue)
        {
            var count = Math.Max(1, library.Variants(cue));
            nextVariant.TryGetValue(cue, out var variant);
            nextVariant[cue] = (variant + 1) % count;
            return variant % count;
        }

        private float Volume(SoundCue cue) => library.Channel(cue) == SoundChannel.Alerts ? levels.Alerts : levels.Effects;
    }
}
