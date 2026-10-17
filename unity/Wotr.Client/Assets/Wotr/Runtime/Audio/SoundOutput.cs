namespace Wotr.Client
{
    // AUDIO-02: where the cues live and how they sound; the policy knows only cues, variants and lengths.
    public interface ISoundLibrary
    {
        int Variants(SoundCue cue);
        float Length(SoundCue cue, int variant);
        SoundChannel Channel(SoundCue cue);
    }

    public interface ISoundOutput
    {
        // Starts the clip; whatever still sounds fades out quickly instead of stopping with a click.
        void Play(SoundCue cue, int variant, float volume);
        void Stop();
        void Update(float seconds);
    }
}
