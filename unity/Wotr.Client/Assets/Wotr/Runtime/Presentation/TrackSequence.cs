using System.Collections.Generic;
using Newtonsoft.Json;
using Wotr.Contracts;

namespace Wotr.Client
{
    // AUDIO-06: the Fellowship's two tracks in one resolution are shown one after the other, each with its sound.
    // - A march is a real step forward: more Progress on the journey, or a further step on the Mordor track. Entering
    //   the track, a reset when the Fellowship is declared, and a step back are no march.
    // - Corruption changes in either direction.
    // - When both change, the journey shows the Fellowship first and the Corruption after it; in Mordor the Hunt is
    //   resolved before the step, so the Corruption comes first. The second stage starts when the first one's sound
    //   has ended; the pair takes under 900 ms. Only the presentation waits: state and choices are current at once.
    public static class TrackSequence
    {
        public const double SecondStageSeconds = 0.38;

        public static bool Marched(FellowshipView before, FellowshipView after) => after.MordorStep.HasValue
            ? before.MordorStep.HasValue && after.MordorStep > before.MordorStep
            : !before.MordorStep.HasValue && after.Progress > before.Progress;

        public static bool Corrupted(FellowshipView before, FellowshipView after) => after.Corruption != before.Corruption;

        public static bool Both(FellowshipView before, FellowshipView after) => Marched(before, after) && Corrupted(before, after);

        public static bool CorruptionFirst(FellowshipView after) => after.MordorStep.HasValue;

        // What the tracks show during the first stage: the first track moved, the other still where it was.
        public static FellowshipView FirstStage(FellowshipView before, FellowshipView after)
        {
            var stage = JsonConvert.DeserializeObject<FellowshipView>(JsonConvert.SerializeObject(after));
            if (CorruptionFirst(after))
            {
                stage.MordorStep = before.MordorStep;
                stage.Progress = before.Progress;
            }
            else
                stage.Corruption = before.Corruption;
            return stage;
        }

        // The sounds of both stages in order, then at most one important cue after the pair.
        public static SoundSequence Sounds(string key, FellowshipView after, SoundRequest? trailing = null)
        {
            var march = SoundCue.FellowshipAdvanced;
            var ring = SoundCue.CorruptionChanged;
            var stages = CorruptionFirst(after)
                ? new List<(SoundCue, double)> { (ring, 0), (march, SecondStageSeconds) }
                : new List<(SoundCue, double)> { (march, 0), (ring, SecondStageSeconds) };
            return new SoundSequence(key, stages, trailing, SecondStageSeconds * 2);
        }
    }
}
