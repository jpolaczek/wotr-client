using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-310 (OPPONENT_ACTIONS_UX §6.8): a presentation sounds a beat's fact when the beat shows it — a track moving, the
    // Hunt's roll — through the game's sound policy (muted, background and focus rules apply); the released view then
    // leaves those cues out, so nothing sounds twice.
    public interface IPresentationSounds
    {
        void Play(SoundRequest request);

        // UI-490 (owner, 2026-11-04): a view waits for the player's return; what is heard in the background sounds now.
        void Away(PlayerView view);
    }

    // The presenters' end of it: the open game's sounds once bound, silence before (or without sound at all).
    public sealed class PresentationSoundSink
    {
        public IPresentationSounds Target { get; set; }

        public void Play(SoundCue cue, string key) => Target?.Play(new SoundRequest(cue, key));

        public void Away(PlayerView view) => Target?.Away(view);
    }
}
