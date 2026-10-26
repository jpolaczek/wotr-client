using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-322 (owner rule 2026-10-24: "smooth transitions, as few quick jumps as possible"): a number that changes — a
    // count on a plaque, a counter, a track's value — swells and lights for a moment as it takes its new value
    // (Wotr.uss .value-pulse). The value itself changes at once, so every query and test reads the new one; only a
    // transform and a colour move, so the layout does not. Under Skip animations nothing moves, and a quiet view
    // (reconnect, a replay's manual step) or a first value never pulses.
    public static class ValuePulse
    {
        public const string HostClass = "value-pulse-host";
        public const string PulseClass = "value-pulse";
        public const long PulseMilliseconds = 320;

        // BoardScreen.ApplyQuietly shows a state without its changes.
        public static bool Quiet { get; set; }

        // The label's new text; a different value it already showed pulses.
        public static void Set(TextElement label, string text)
        {
            if (label.text == text)
                return;
            var changed = label.text.Length > 0;
            label.text = text;
            if (changed)
                Pulse(label);
        }

        public static void Pulse(VisualElement element)
        {
            if (Quiet)
                return;
            element.AddToClassList(HostClass);
            element.AddToClassList(PulseClass);
            element.schedule.Execute(() => element.RemoveFromClassList(PulseClass)).ExecuteLater(PulseMilliseconds);
        }
    }
}
