using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-316 (owner review 2026-10-24: "a played card should not just appear on the screen but slide out of the place it
    // takes effect from"): a presented card's own motion. It grows out of the place it comes from into its reading place,
    // and later shrinks into the place it goes to. Only its translate, scale and opacity change — none touches the layout
    // or the card's own placement — eased. UI-321: under Skip animations it takes no time and the card simply stands.
    // Clear leaves the card as it stands.
    public sealed class CardFlight
    {
        public const double Seconds = .6;
        private const float Smallest = .12f;
        private const float Largest = .35f;
        private const float Faint = .55f;
        private readonly VisualElement card;
        private readonly bool reduced;
        private Vector2 offset;
        private float scale = 1;
        private bool moving;

        public CardFlight(VisualElement card, bool reduced)
        {
            this.card = card;
            this.reduced = reduced;
        }

        public double Duration => reduced ? 0 : Seconds;

        // Before the card has its place: nothing of it shows yet (at once under Skip animations).
        public void Hide()
        {
            if (!reduced)
                card.style.opacity = 0;
        }

        // From the place it comes from, in world points (or, without one, a fade in place), to where it stands now.
        public void From(Rect? origin) => Aim(origin);

        // From where it stands now to the place it goes to (or, without one, a fade in place).
        public void To(VisualElement destination) =>
            Aim(destination != null && PresentationTargets.Shown(destination) ? destination.worldBound : (Rect?)null);

        // t from 0 to 1: out of the origin into the card's place, slow at both ends so both are seen.
        public void Arriving(double t)
        {
            var eased = Eased(t);
            Show(1 - eased, Mathf.Lerp(Faint, 1, eased));
        }

        // t from 0 to 1: out of the card's place into the destination.
        public void Leaving(double t)
        {
            var eased = Eased(t);
            Show(eased, Mathf.Lerp(1, Faint, eased));
        }

        private static float Eased(double t)
        {
            var x = Mathf.Clamp01((float)t);
            return x < .5f ? 4 * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 3) / 2;
        }

        public void Clear()
        {
            card.style.translate = StyleKeyword.Null;
            card.style.scale = StyleKeyword.Null;
            card.style.opacity = StyleKeyword.Null;
        }

        public static bool Laid(VisualElement element) =>
            !float.IsNaN(element.worldBound.width) && element.worldBound.width > 0 && element.worldBound.height > 0;

        private void Aim(Rect? other)
        {
            moving = !reduced && other.HasValue && Laid(card);
            if (!moving)
                return;
            var from = card.worldBound;
            var to = other.Value;
            offset = to.center - from.center;
            // As small as the object it comes from or goes to (a chip, a tab, the console's plaque), never full size there.
            scale = Mathf.Clamp(to.height / from.height, Smallest, Largest);
        }

        // away: 0 at the card's place, 1 at the other end.
        private void Show(float away, float opacity)
        {
            if (reduced)
                return;
            if (!moving)
            {
                card.style.opacity = 1 - away;
                return;
            }
            card.style.translate = new Translate(offset.x * away, offset.y * away);
            card.style.scale = new Scale(Vector2.one * Mathf.Lerp(1, scale, away));
            card.style.opacity = opacity;
        }
    }
}
