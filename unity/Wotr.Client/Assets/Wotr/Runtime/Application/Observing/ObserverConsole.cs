using Wotr.Contracts;

namespace Wotr.Client
{
    // OBS-03: the console of an observer's board says which side decides and on what, in neutral words; it never
    // glows for the observer and offers no step.
    public sealed class ObserverConsole
    {
        private readonly BoardScreen board;
        private readonly TurnDraft draft = new TurnDraft();

        public ObserverConsole(BoardScreen board) => this.board = board;

        // decision: what the side that decides is choosing (the board itself shows no decision to an observer).
        public void Apply(PlayerView view, DecisionView decision)
        {
            var state = TurnConsoleState.For(view, draft, board.ActionDie, RegionName);
            board.Console.Status.Apply(state, "");
            board.Console.Status.SetYourTurn(false);
            board.Console.Status.SetMustAct(false);
            board.Console.Status.SetHint(Hint(view, decision, state));
        }

        public static string Hint(PlayerView view, DecisionView decision, TurnConsoleState state)
        {
            if (view.Winner.HasValue || !view.DecisionOwner.HasValue)
                return state.Guidance;
            var side = BoardScreen.SideName(view.DecisionOwner.Value);
            return decision == null ? side + " is deciding." : side + " is deciding · " + DecisionTray.Title(decision.Kind, null);
        }

        private string RegionName(string id) => board.Presentation.Regions.TryGetValue(id, out var region) ? region.Name : id;
    }
}
