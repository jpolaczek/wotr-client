using System.Collections.Generic;

namespace Wotr.Client
{
    // AUDIO-02: one cue for one event. The key names the event within the open game (a command, a step, a decision,
    // a battle) so that the same event seen twice — a receipt and a new view, two renderers — sounds once. A gesture
    // has no key: every completed gesture is new.
    public readonly struct SoundRequest
    {
        public SoundCue Cue { get; }
        public string Key { get; }

        public SoundRequest(SoundCue cue, string key = "")
        {
            Cue = cue;
            Key = key ?? "";
        }
    }

    // AUDIO-02/AUDIO-06: a short ordered presentation with its own cue at the start of each stage (seconds from the
    // start), and at most one important cue after the last stage. The stages never suppress one another.
    public sealed class SoundSequence
    {
        public string Key { get; }
        public IReadOnlyList<(SoundCue Cue, double At)> Stages { get; }
        public SoundRequest? Trailing { get; }
        public double TrailingAt { get; }

        public SoundSequence(string key, IReadOnlyList<(SoundCue Cue, double At)> stages, SoundRequest? trailing = null, double trailingAt = 0)
        {
            Key = key ?? "";
            Stages = stages;
            Trailing = trailing;
            TrailingAt = trailingAt;
        }
    }

    // AUDIO-03: the player's sound settings as the policy sees them.
    public readonly struct SoundLevels
    {
        public bool Enabled { get; }
        public float Effects { get; }
        public float Alerts { get; }
        public bool BackgroundAlerts { get; }

        public SoundLevels(bool enabled, float effects, float alerts, bool backgroundAlerts)
        {
            Enabled = enabled;
            Effects = effects;
            Alerts = alerts;
            BackgroundAlerts = backgroundAlerts;
        }

        public static SoundLevels Default => new SoundLevels(true, 0.4f, 0.6f, true);
    }
}
