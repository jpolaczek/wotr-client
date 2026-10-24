using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-321 (owner rule 2026-10-24: "smooth transitions, as few quick jumps as possible; the jumps are an option, skip
    // animations"): a panel, window or part of a window that comes into view fades in under .motion-enabled (Wotr.uss
    // .appear-host, .appearing) instead of appearing in one frame. Only its opacity changes, so its place and size are its
    // own from the first frame; under Skip animations it appears at once. Hiding stays immediate.
    public static class Appearance
    {
        public const string HostClass = "appear-host";
        public const string AppearingClass = "appearing";

        public static void Display(VisualElement element, bool visible)
        {
            if (!visible)
            {
                element.style.display = DisplayStyle.None;
                return;
            }
            var hidden = element.style.display == DisplayStyle.None || element.resolvedStyle.display == DisplayStyle.None;
            element.style.display = DisplayStyle.Flex;
            if (!hidden)
                return;
            Arrive(element);
        }

        // A new element (a plate, a caption, the Skip medallion) fades in as it joins the panel.
        public static void Arrive(VisualElement element)
        {
            element.AddToClassList(HostClass);
            element.AddToClassList(AppearingClass);
            element.schedule.Execute(() => element.RemoveFromClassList(AppearingClass));
        }
    }
}
