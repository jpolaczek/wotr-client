using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-327 (owner 2026-10-25: "tokens that change their state should turn over to their other side smoothly"): a two-sided
    // token — the Fellowship's Hidden/Revealed Ring, its marker on the track, a nation's passive or active token — turns over
    // when its side changes. It narrows to its edge (0.15 s), shows its other side at the middle of the turn and widens again
    // (0.15 s). Only its horizontal scale changes, so nothing around it moves. Under Skip animations, or while it is not
    // shown, the side changes at once; a new turn of the same token first finishes the one before.
    public static class TokenFlip
    {
        public const double HalfSeconds = .15;
        private static readonly ConditionalWeakTable<VisualElement, Turning> turning = new ConditionalWeakTable<VisualElement, Turning>();

        public static void Turn(VisualElement token, Action swap)
        {
            if (turning.TryGetValue(token, out var earlier))
                earlier.Finish();
            if (!Animated(token))
            {
                swap();
                return;
            }
            turning.Add(token, new Turning(token, swap));
        }

        public static bool IsTurning(VisualElement token) => turning.TryGetValue(token, out _);

        private static bool Animated(VisualElement token)
        {
            if (token.panel == null || token.resolvedStyle.display == DisplayStyle.None || token.worldBound.width <= 0)
                return false;
            for (var element = token; element != null; element = element.parent)
                if (element.ClassListContains("reduced-motion"))
                    return false;
            return true;
        }

        private sealed class Turning
        {
            private readonly VisualElement token;
            private readonly double start = Time.realtimeSinceStartupAsDouble;
            private readonly IVisualElementScheduledItem tick;
            private Action swap;

            public Turning(VisualElement token, Action swap)
            {
                this.token = token;
                this.swap = swap;
                tick = token.schedule.Execute(Step).Every(16);
                Step();
            }

            private void Step()
            {
                var half = (Time.realtimeSinceStartupAsDouble - start) / HalfSeconds;
                if (half >= 1 && swap != null)
                {
                    swap();
                    swap = null;
                }
                if (half >= 2)
                {
                    Finish();
                    return;
                }
                var width = half < 1 ? 1 - Eased(half) : Eased(half - 1);
                token.style.scale = new Scale(new Vector2(Mathf.Max(.02f, width), 1));
            }

            public void Finish()
            {
                tick.Pause();
                swap?.Invoke();
                swap = null;
                token.style.scale = StyleKeyword.Null;
                turning.Remove(token);
            }

            private static float Eased(double t)
            {
                var x = Mathf.Clamp01((float)t);
                return Mathf.Sin(x * Mathf.PI / 2);
            }
        }
    }
}
