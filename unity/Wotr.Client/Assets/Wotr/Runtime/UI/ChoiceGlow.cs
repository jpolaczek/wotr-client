using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-229/UI-238/UI-240: an object the player is asked to click for a choice (the next Guide, the die an Elven Ring
    // changes, a character's change into another version) glows in gold and pulses in the shared rhythm under
    // .motion-enabled; Reduced motion keeps it bright and still. Sizes and places stay fixed.
    public static class ChoiceGlow
    {
        public const string ClassName = "choice-glow";
        public const string PulseClass = "choice-glow-pulse";

        public static void Apply(VisualElement element, bool on) => element.EnableInClassList(ClassName, on);
    }

    // One ticker per host toggles the pulse on its glowing children, and stops while none glows.
    public sealed class ChoicePulse
    {
        private readonly VisualElement host;
        private readonly IVisualElementScheduledItem tick;

        public ChoicePulse(VisualElement host)
        {
            this.host = host;
            tick = host.schedule.Execute(() => host.ToggleInClassList(ChoiceGlow.PulseClass)).Every(MapTargetGlow.PulseMilliseconds);
            tick.Pause();
        }

        public void Run(bool any)
        {
            if (any)
            {
                tick.Resume();
                return;
            }
            tick.Pause();
            host.RemoveFromClassList(ChoiceGlow.PulseClass);
        }
    }
}
