namespace Wotr.Client
{
    // UI-305 (OPPONENT_ACTIONS_UX §5): the observed trace, the program's one result treatment — a 2 pt pale-blue ring
    // (gold since UI-378, rgba 255, 214, 112, 0.85) with no plate, pulse or lift, which ignores the pointer. It is never a legal target, a
    // choice glow, a selection, a ghost or a pressed state. Styles/Presentation.uss draws it; the family presenters of
    // UI-306–UI-312 reuse it for figures, crests, markers, portraits and cards.
    public static class ObservedTrace
    {
        public const string ClassName = "observed-trace";
    }
}
