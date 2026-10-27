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
        Defeat,
        // AUDIO-12: a nation's token slides on the Politics track.
        PoliticsMoved
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
        // AUDIO_UX: end of game → failure → battle → decision → Ring → Fellowship → politics → dice → cards → accepted →
        // click. AUDIO-12: a political step outranks the card or die that caused it, and yields to the tracks.
        private static readonly Dictionary<SoundCue, int> Priorities = new Dictionary<SoundCue, int>
        {
            [SoundCue.UiClick] = 0,
            [SoundCue.CardInspect] = 1,
            [SoundCue.ActionAccepted] = 2,
            [SoundCue.CardMove] = 3,
            [SoundCue.DiceRoll] = 4,
            [SoundCue.PoliticsMoved] = 5,
            [SoundCue.FellowshipAdvanced] = 6,
            [SoundCue.CorruptionChanged] = 7,
            [SoundCue.DecisionRequired] = 8,
            [SoundCue.BattleStarted] = 9,
            [SoundCue.ActionFailed] = 10,
            [SoundCue.Victory] = 11,
            [SoundCue.Defeat] = 11
        };

        public static int Priority(SoundCue cue) => Priorities[cue];

        // How long a playing clip keeps less important cues out: its whole length, except the Politics bell (AUDIO-12),
        // which holds only through the token's 0.7 s slide; its quiet ring may then give way to a click.
        public static double Holds(SoundCue cue, double length) => cue == SoundCue.PoliticsMoved ? System.Math.Min(length, 0.8) : length;

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
