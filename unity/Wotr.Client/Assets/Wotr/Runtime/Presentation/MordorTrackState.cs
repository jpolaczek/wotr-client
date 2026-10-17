using Wotr.Contracts;

namespace Wotr.Client
{
    public enum MordorSlabState { Ahead, Passed, Current }

    // UI-154: what the Mordor track on the map shows for the public Fellowship view. The track is always drawn;
    // only a Fellowship on it marks its steps.
    public sealed class MordorTrackState
    {
        public const string ActivityHint = "No attempt to move or hide on the track adds 1 Corruption at the end of the " +
            "action phase. A stopped move counts; entering Mordor does not.";
        public static readonly MordorTrackState Outside = new MordorTrackState(-1, false, 0, null);
        public bool Active => Step >= 0;
        public int Step { get; }
        public bool Hidden { get; }
        public int Corruption { get; }
        public bool? Attempted { get; }

        private MordorTrackState(int step, bool hidden, int corruption, bool? attempted)
        {
            Step = step;
            Hidden = hidden;
            Corruption = corruption;
            Attempted = attempted;
        }

        public static MordorTrackState For(FellowshipView fellowship) => fellowship?.MordorStep is int step
            ? new MordorTrackState(step, fellowship.Hidden, fellowship.Corruption, fellowship.MordorAttemptedThisTurn)
            : Outside;

        public MordorSlabState Slab(int step) => !Active || step > Step ? MordorSlabState.Ahead
            : step < Step ? MordorSlabState.Passed : MordorSlabState.Current;

        // UI-157: the Fellowship enters only from these gates; a Fellowship in Gorgoroth or elsewhere is off the track.
        public const string EntryHint = "The Fellowship enters this track only from Morannon or Minas Morgul (Enter Mordor).";

        public string SlabTooltip(int step)
        {
            var name = step == MordorTrackGeometry.LastStep
                ? "Crack of Doom · Reaching it with Corruption below 12 destroys the Ring"
                : "Mordor · Step " + step + " of " + MordorTrackGeometry.LastStep;
            return Slab(step) switch
            {
                MordorSlabState.Current => name + "\nThe Fellowship is here.",
                MordorSlabState.Passed => name + "\nPassed",
                _ => Active ? name : name + "\n" + EntryHint
            };
        }

        public static string GateTooltip(string region) => (region == "morannon" ? "Morannon" : "Minas Morgul") +
            " · Entrance of the Mordor track\n" + EntryHint;

        public string FigureTooltip => "Fellowship · " + (Hidden ? "Hidden" : "Revealed") + " · Mordor step " + Step +
            " of " + MordorTrackGeometry.LastStep + "\nCorruption " + Corruption + "/12" + (Attempted == false
                ? "\nNo move or hide attempted this turn · Otherwise +1 Corruption" : "");
    }
}
