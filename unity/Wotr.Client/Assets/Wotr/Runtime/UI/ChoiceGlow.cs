using System.Collections.Generic;
using System.Linq;
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

    // UI-434 (owner, after UI-433): the dice a choice offers in a pool wear the shared gold glow and pulse with the pool,
    // wherever the pool stands (the console's own pool, the top bar's opponent pool). Several interactions offer dice in
    // the same pool (an Elven Ring's die, a payment, a resource decision); each names its own and the pool lights their
    // union, so one interaction's refresh never puts out another's. Anything else in the pool that glows (a Council
    // medallion) breathes with the same pulse.
    public sealed class PoolChoiceGlow
    {
        private readonly VisualElement pool;
        private readonly ChoicePulse pulse;
        private readonly Dictionary<object, HashSet<string>> offers = new Dictionary<object, HashSet<string>>();

        public PoolChoiceGlow(VisualElement pool)
        {
            this.pool = pool;
            pulse = new ChoicePulse(pool);
        }

        public void Offer(object source, IEnumerable<string> dice)
        {
            offers[source] = new HashSet<string>(dice);
            Apply();
        }

        // An interaction that ends takes its dice back.
        public void Withdraw(object source)
        {
            if (offers.Remove(source))
                Apply();
        }

        public bool Offers(string die) => offers.Values.Any(dice => dice.Contains(die));

        // Lights the offered dice and runs the pulse while anything in the pool glows.
        public void Apply()
        {
            foreach (var button in pool.Query<Button>().ToList())
                if (button.userData is DraftToken { Object: DraftObject.Die } token)
                    ChoiceGlow.Apply(button, Offers(token.Id));
            pulse.Run(pool.Query(className: ChoiceGlow.ClassName).ToList().Count > 0);
        }
    }
}
