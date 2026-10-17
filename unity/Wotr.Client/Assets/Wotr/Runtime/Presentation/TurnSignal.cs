using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-167 (SIGNALS_UX B): the moment the action passes to the player. A new decision of the player that starts
    // their acting — an action, the Fellowship phase or the Hunt allocation — right after a decision that was not
    // theirs gives the ribbon's text. Decisions inside a battle, the Hunt or a card effect open windows of their own,
    // and the first view of a match or after a reconnection only lights the console.
    public sealed class TurnSignal
    {
        private bool primed;
        private string decision = "";
        private bool owned;

        public string Observe(PlayerView view)
        {
            var id = view.Decision?.Id ?? "";
            var own = Own(view);
            var passes = primed && PassesTo(decision, owned, view);
            var text = passes && !view.Winner.HasValue && Starts(view.Decision.Kind) ? Text(view) : "";
            primed = true;
            decision = id;
            owned = own;
            return text;
        }

        // AUDIO-07: the decision passes to the player — any kind, a reaction in a battle, the Hunt or a card effect too:
        // a new own decision right after one that was not theirs, while the game goes on. Their next own steps and a
        // choice the server skipped for them are not such a moment.
        public static bool Passes(PlayerView before, PlayerView after) =>
            PassesTo(before.Decision?.Id ?? "", Own(before), after) && !after.Winner.HasValue;

        private static bool PassesTo(string previous, bool previouslyOwn, PlayerView view) =>
            Own(view) && view.Decision.Id.Length > 0 && view.Decision.Id != previous && !previouslyOwn;

        private static bool Own(PlayerView view) => view.Decision != null && view.DecisionOwner == view.You;

        // After the connection returns, the next view catches up; it is not a moment to signal.
        public void Rebase() => primed = false;

        public static bool Starts(DecisionKind kind) =>
            kind is DecisionKind.Action or DecisionKind.FellowshipPhase or DecisionKind.AllocateHunt;

        public static string Text(PlayerView view) => view.Decision.Kind switch
        {
            DecisionKind.FellowshipPhase => "FELLOWSHIP PHASE",
            DecisionKind.AllocateHunt => "ALLOCATE HUNT DICE",
            _ => "YOUR ACTION · Turn " + view.Turn
        };

        // The console's plaque glows while such a decision waits for its first step.
        public static bool Waiting(PlayerView view, TurnDraft draft) => view.Decision != null && view.DecisionOwner == view.You &&
            !view.Winner.HasValue && Starts(view.Decision.Kind) && Untouched(draft);

        // Nothing chosen yet: no die, no selection, and no command beyond the Hunt allocation the client opens itself.
        private static bool Untouched(TurnDraft draft) => draft.Payload.DieId.Length == 0 && draft.Payload.Ids.Count == 0 &&
            (!draft.Kind.HasValue || draft.Kind == CommandKind.AllocateHunt);
    }
}
