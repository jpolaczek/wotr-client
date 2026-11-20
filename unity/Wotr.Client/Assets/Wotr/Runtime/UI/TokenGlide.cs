using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-544 (owner, 2026-11-17: "when I use Wisdom of Elrond the nation activates and advances with a jump; the tokens must
    // always animate, never jump; the same for the Ring on the Corruption and Fellowship tracks, whatever effect causes
    // it"). A track token's travel, independent of style classes and of the frame its new place is written in: the
    // element stands on its new place at once (left/top as before) and an offset (translate) from where it was last seen
    // eases back to nothing. A USS transition on left needed a class resolved before the write, so a change written in the
    // same frame as its class (a card's own choice) or while its panel was hidden jumped. Here the move is remembered by
    // track position: a change seen while the panel was closed travels when the panel opens, and a new layout of an
    // unchanged position (a resize) places at once. A first placement, Reduced motion and a quiet view (Skip, a catch-up)
    // place at once.
    // UI-557 (owner, 2026-11-20: animations "sometimes start late"): a change seen while its panel was closed travels only
    // when the panel shows it within FreshSeconds (a panel opened for that very step). A later opening finds the token at
    // its place, so an old move never plays inside a later one (the opponent's presentation opening the panel).
    public sealed class TokenGlide
    {
        public const double FreshSeconds = 0.6;
        // A quiet view (Skip, a catch-up after an absence) places every token at once.
        public static bool Quiet { get; set; }
        private int unplacedPosition = -1;
        private double unplacedAt;
        private readonly VisualElement[] elements;
        private readonly Func<int, double> seconds;
        private IVisualElementScheduledItem tick;
        private int shownPosition = -1;
        private float shownLeft;
        private float offset;
        private float from;
        private double start;
        private double duration;

        // seconds: how long a travel over that many positions lasts.
        public TokenGlide(Func<int, double> seconds, params VisualElement[] elements)
        {
            this.seconds = seconds;
            this.elements = elements;
        }

        public bool Moving => tick != null && offset != 0;

        public float Offset => offset;

        // The token now stands at `left` (written by the caller) for this track position. delay: seconds before the travel
        // begins (an activation's turn first).
        public void Placed(int position, float left, double delay = 0)
        {
            var previous = shownPosition;
            var seen = shownLeft + offset;
            var stale = unplacedPosition == position && Now() - unplacedAt > FreshSeconds;
            unplacedPosition = -1;
            shownPosition = position;
            shownLeft = left;
            if (previous < 0 || previous == position || stale || Quiet || !Motion(elements[0]) || Mathf.Abs(seen - left) < .5f)
            {
                // A resize keeps a travel under way going to the new place.
                if (previous == position && Moving && !Quiet)
                    return;
                Stop();
                return;
            }
            from = seen - left;
            duration = seconds(Math.Abs(position - previous));
            start = Now() + delay;
            Set(from);
            tick ??= elements[0].schedule.Execute(Step).Every(16);
            tick.Resume();
        }

        // UI-557: the token's position changed while the caller could not place it (its panel closed or not laid out yet);
        // the moment counts from the first sight of that position.
        public void Unplaced(int position)
        {
            if (position == unplacedPosition)
                return;
            unplacedPosition = position == shownPosition ? -1 : position;
            unplacedAt = Now();
        }

        // Ends any travel at its place (Skip, a closed match).
        public void Stop()
        {
            tick?.Pause();
            Set(0);
        }

        private void Step()
        {
            var t = duration <= 0 ? 1 : (Now() - start) / duration;
            if (t < 0)
                return;
            if (t >= 1)
            {
                Stop();
                return;
            }
            Set((float)(from * (1 - Ease(t))));
        }

        private void Set(float value)
        {
            offset = value;
            foreach (var element in elements)
                element.style.translate = value == 0 ? new StyleTranslate(StyleKeyword.Null) : new Translate(value, 0);
        }

        // Ease out: a token sets off at once (the move answers the change) and settles on its place.
        private static double Ease(double t) => 1 - Math.Pow(1 - t, 3);

        private static double Now() => Time.realtimeSinceStartupAsDouble;

        // Motion follows the board's setting (.motion-enabled on an ancestor; Reduced motion has none).
        private static bool Motion(VisualElement element)
        {
            for (var e = element; e != null; e = e.parent)
                if (e.ClassListContains("motion-enabled"))
                    return true;
            return false;
        }
    }
}
