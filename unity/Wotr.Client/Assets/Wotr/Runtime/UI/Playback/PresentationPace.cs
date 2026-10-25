namespace Wotr.Client
{
    // UI-315 (owner review 2026-10-24: "windows open very fast and you often cannot see what really happens"): one pace for
    // a presented change, so the viewer sees it happen. A panel the presentation opens settles first; the changing object
    // glows before its value changes; the change moves visibly (a token slides, a count or a face turns); the object stays
    // lit at its new value. Skip still ends everything at once.
    public static class PresentationPace
    {
        public const double PanelSettleSeconds = .4;
        public const double MarkSeconds = .7;
        public const double ChangeSeconds = .7;
        public const double HoldSeconds = .8;
    }
}
