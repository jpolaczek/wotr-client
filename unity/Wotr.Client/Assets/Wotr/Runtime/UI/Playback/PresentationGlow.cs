using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-314 (owner review 2026-10-24): the observed light — a soft glow around a marked object's own shape (a zero-offset
    // drop-shadow, Presentation.uss), never a frame around it and never the gold ChoiceGlow of a choice. It fades in over
    // 200 ms and out over 400 ms under .motion-enabled; Reduced motion shows and removes it at once. It changes only the
    // object's filter, so its size, place, pointer, cursor and tooltip stay its own. Two marks of one object (a march's
    // destination that a battle also marks) share its glow: it goes out with the last of them.
    public static class PresentationGlow
    {
        public const string ClassName = "presentation-glow";
        public const string HostClass = "presentation-glow-host";
        public const long FadeOutMilliseconds = 400;
        private static readonly Dictionary<VisualElement, int> lit = new Dictionary<VisualElement, int>();

        public static bool IsLit(VisualElement element) => element != null && element.ClassListContains(ClassName);

        public static void On(VisualElement element)
        {
            if (element == null)
                return;
            lit.TryGetValue(element, out var marks);
            lit[element] = marks + 1;
            element.AddToClassList(HostClass);
            element.AddToClassList(ClassName);
        }

        // The glow fades; the transition's host class leaves after it, unless the object was lit again meanwhile.
        public static void Off(VisualElement element)
        {
            if (element == null || !lit.TryGetValue(element, out var marks))
                return;
            if (marks > 1)
            {
                lit[element] = marks - 1;
                return;
            }
            lit.Remove(element);
            element.RemoveFromClassList(ClassName);
            element.schedule.Execute(() =>
            {
                if (!element.ClassListContains(ClassName))
                    element.RemoveFromClassList(HostClass);
            }).StartingIn(FadeOutMilliseconds);
        }
    }

    // One mark's glow on the object it follows. The object found now may be another element (a marker rebuilt by the
    // staged board, a token instead of its tab), so the glow moves to it; End puts it out.
    public sealed class ObjectGlow
    {
        public VisualElement Target { get; private set; }

        public void Follow(VisualElement target)
        {
            if (target == Target)
                return;
            PresentationGlow.Off(Target);
            Target = target;
            PresentationGlow.On(target);
        }

        public void End() => Follow(null);
    }

    // UI-402: one mark's glow on a force — its army's base and figures and the characters standing beside it, each its
    // own miniature — following the elements found now; End puts them all out.
    public sealed class ForceGlow
    {
        private readonly List<ObjectGlow> glows = new List<ObjectGlow>();

        public IEnumerable<VisualElement> Targets
        {
            get
            {
                foreach (var glow in glows)
                    if (glow.Target != null)
                        yield return glow.Target;
            }
        }

        public void Follow(IEnumerable<VisualElement> targets)
        {
            var index = 0;
            foreach (var target in targets)
            {
                if (index == glows.Count)
                    glows.Add(new ObjectGlow());
                glows[index++].Follow(target);
            }
            for (; index < glows.Count; index++)
                glows[index].End();
        }

        public void End()
        {
            foreach (var glow in glows)
                glow.End();
        }
    }
}
