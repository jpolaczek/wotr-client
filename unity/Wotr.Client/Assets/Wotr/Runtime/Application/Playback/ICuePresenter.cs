namespace Wotr.Client
{
    // UI-304: shows the cues of one presentation family (OPPONENT_ACTIONS_UX §8) on the presented board. The camera,
    // card and object presenters of UI-305–UI-312 implement it and register with PresentationPlayback.
    public interface ICuePresenter
    {
        // The beat that shows this cue, or null when this presenter does not show it.
        IPresentationBeat Begin(PresentationCue cue, double now);
    }

    // One staged visual change of a cue. It ends by itself, or at once on Skip, a rebase or disposal; either way it leaves
    // no visual override behind, because the latest view is applied right after.
    public interface IPresentationBeat
    {
        // True once the beat has finished.
        bool Advance(double now);

        void Finish();
    }
}
