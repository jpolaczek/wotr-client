namespace Wotr.Client
{
    // UI-342 (owner 2026-10-26: "let's not wait for the end of the opponent's turn to close the politics track; close it
    // when the next animation begins"): registered first, so the Politics panel a presentation opened closes as the next
    // scene begins rather than at the release (PoliticsPresenter.Retire).
    public sealed class PoliticsRetirement : ICuePresenter
    {
        private readonly PoliticsPresenter politics;
        private readonly PresentationPlayback playback;

        public PoliticsRetirement(PoliticsPresenter politics, PresentationPlayback playback)
        {
            this.politics = politics;
            this.playback = playback;
        }

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            politics.Retire(cue, playback.Upcoming);
            return null;
        }
    }
}
