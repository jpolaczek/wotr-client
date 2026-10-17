using System.Collections.Generic;

namespace Wotr.Client
{
    // AUDIO-02: the sound effects of the game (docs/ui/AUDIO_UX.md); each names its own clips in the catalogue.
    public enum SoundCue
    {
        UiClick,
        CardInspect,
        CardMove,
        DiceRoll,
        ActionAccepted,
        BattleStarted,
        FellowshipAdvanced,
        CorruptionChanged,
        DecisionRequired,
        ActionFailed,
        Victory,
        Defeat
    }

    public enum SoundChannel
    {
        Effects,
        Alerts
    }

    // What decides between cues: the priority of one resolution, the gestures' shortest repeat, and which cues may
    // sound while the game is in the background.
    public static class SoundCues
    {
        // AUDIO_UX: end of game → failure → battle → decision → Ring → Fellowship → dice → cards → accepted → click.
        private static readonly Dictionary<SoundCue, int> Priorities = new Dictionary<SoundCue, int>
        {
            [SoundCue.UiClick] = 0,
            [SoundCue.CardInspect] = 1,
            [SoundCue.ActionAccepted] = 2,
            [SoundCue.CardMove] = 3,
            [SoundCue.DiceRoll] = 4,
            [SoundCue.FellowshipAdvanced] = 5,
            [SoundCue.CorruptionChanged] = 6,
            [SoundCue.DecisionRequired] = 7,
            [SoundCue.BattleStarted] = 8,
            [SoundCue.ActionFailed] = 9,
            [SoundCue.Victory] = 10,
            [SoundCue.Defeat] = 10
        };

        public static int Priority(SoundCue cue) => Priorities[cue];

        public static double ShortestRepeat(SoundCue cue) => cue switch
        {
            SoundCue.UiClick => 0.1,
            SoundCue.CardInspect => 0.25,
            _ => 0
        };

        // Without focus only a new decision and the end of the open game may sound (AUDIO_UX, Settings and background).
        public static bool HeardInBackground(SoundCue cue) =>
            cue is SoundCue.DecisionRequired or SoundCue.Victory or SoundCue.Defeat;
    }
}
