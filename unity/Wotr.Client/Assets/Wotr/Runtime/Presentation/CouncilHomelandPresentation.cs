using Wotr.Contracts;

namespace Wotr.Client
{
    public static class CouncilHomelandPresentation
    {
        public static bool Owns(PlayerView view) => view != null && !view.Winner.HasValue &&
            view.You == Side.FreePeoples && view.DecisionOwner == view.You &&
            view.Decision?.Kind == DecisionKind.CouncilHomelands;

        public static string TokenEffect(int departures) => departures == 0 ? "No tokens"
            : departures == 1 ? "Shadow chooses one" : "Shadow receives both";

        public static string TokenDetail(int departures) => departures == 0
            ? "All companions stay with the Fellowship. Shadow receives no Council Action Tokens."
            : departures == 1
                ? "After you confirm, Shadow chooses either the Politics or Travel Action Token in a separate decision."
                : "After you confirm, Shadow receives both Council Action Tokens. No further token choice is required.";
    }
}
