using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-272: Three Rings for the Elven-Kings recovers Rings on their sockets in the console: the Rings it may return
    // glow, each click picks one, and the click that completes the count sends the choice.
    public static class RingRecovery
    {
        public const string Presentation = "ElvenRings";

        public static bool Active(PlayerView view) => view?.Decision?.Kind == DecisionKind.EffectChoice &&
            view.Decision.Presentation == Presentation && view.DecisionOwner == view.You && !view.Winner.HasValue;

        public static string Hint(PlayerView view, TurnDraft draft)
        {
            var option = Active(view) ? DraftOptions.For(view, draft) : null;
            if (option == null)
                return "";
            var left = option.Minimum - draft.Payload.Ids.Count;
            return left > 0 ? "Click " + left + " glowing Ring" + (left == 1 ? "" : "s") + " in the console to recover " + (left == 1 ? "it" : "them")
                : "Ready to recover the chosen Rings";
        }
    }
}
