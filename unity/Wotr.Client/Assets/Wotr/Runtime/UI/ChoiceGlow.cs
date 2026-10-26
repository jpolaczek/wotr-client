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
        // UI-345 (EXP-15 D6): the expansions' lit art (special, Lore and Council dice; Keeper Ring sockets; Council
        // medallions; the Spear's deck) keeps its art and pulses in the same rhythm, with a ticker of its own.
        public const string LitArtPulseClass = "lit-art-pulse";

        public static void Apply(VisualElement element, bool on) => element.EnableInClassList(ClassName, on);
    }

    // One ticker per host toggles the pulse on its glowing children, and stops while none glows.
    public sealed class ChoicePulse
    {
        private readonly VisualElement host;
        private readonly string pulseClass;
        private readonly IVisualElementScheduledItem tick;

        public ChoicePulse(VisualElement host, string pulseClass = ChoiceGlow.PulseClass)
        {
            this.host = host;
            this.pulseClass = pulseClass;
            tick = host.schedule.Execute(() => host.ToggleInClassList(pulseClass)).Every(MapTargetGlow.PulseMilliseconds);
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
            host.RemoveFromClassList(pulseClass);
        }
    }
}
