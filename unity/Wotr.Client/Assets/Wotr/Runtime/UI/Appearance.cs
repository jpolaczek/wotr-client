using System.Linq;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-321 (owner rule 2026-10-24: "smooth transitions, as few quick jumps as possible; the jumps are an option, skip
    // animations"): a panel, window or part of a window that comes into view fades in under .motion-enabled (Wotr.uss
    // .appear-host, .appearing) instead of appearing in one frame. Only its opacity changes, so its place and size are its
    // own from the first frame; under Skip animations it appears at once.
    // UI-322: closing is the mirror image. The element fades out over CloseMilliseconds where it stands (.closing) and
    // only then leaves the layout, but it counts as closed at once (Closed) and takes no pointer meanwhile; opening it
    // again during the fade keeps it. Under Skip animations it leaves at once.
    public static class Appearance
    {
        public const string HostClass = "appear-host";
        public const string AppearingClass = "appearing";
        public const string ClosingClass = "closing";
        public const long CloseMilliseconds = 150;

        public static void Display(VisualElement element, bool visible)
        {
            if (!visible)
            {
                Close(element);
                return;
            }
            element.RemoveFromClassList(ClosingClass);
            var hidden = element.style.display == DisplayStyle.None || element.resolvedStyle.display == DisplayStyle.None;
            element.style.display = DisplayStyle.Flex;
            if (!hidden)
                return;
            Arrive(element);
        }

        // Closed for every decision and query, while it may still be fading out.
        public static bool Closed(VisualElement element) =>
            element.style.display == DisplayStyle.None || element.ClassListContains(ClosingClass);

        // A new element (a plate, a caption, the Skip medallion) fades in as it joins the panel; `delay` lets what changed
        // first be seen (UI-327: a Hunt phase's content after the ribbon's marker sets off).
        public static void Arrive(VisualElement element, long delay = 0)
        {
            element.AddToClassList(HostClass);
            element.AddToClassList(AppearingClass);
            element.schedule.Execute(() => element.RemoveFromClassList(AppearingClass)).ExecuteLater(delay);
        }

        // A part that changed in place (a die's face): the new one fades in over the old one, which fades out and goes.
        // The caller crossfades only under .motion-enabled (Moving). Both are new elements, so each fade starts once it
        // has been laid out: before it has a style there is nothing to animate from.
        public static void Crossfade(VisualElement old, VisualElement next)
        {
            Depart(old);
            next.AddToClassList(HostClass);
            next.AddToClassList(AppearingClass);
            next.RegisterCallback<GeometryChangedEvent>(FadeIn);
        }

        // A new copy of what just left (a figure stack, a die's old face) fades out where it stood and goes; it takes no
        // pointer. The caller departs only under .motion-enabled (Moving).
        public static void Depart(VisualElement ghost)
        {
            ghost.pickingMode = PickingMode.Ignore;
            ghost.AddToClassList(HostClass);
            ghost.RegisterCallback<GeometryChangedEvent>(FadeOut);
        }

        // Transitions run: Skip animations is off for this element's screen.
        public static bool Moving(VisualElement element)
        {
            for (var node = element; node != null; node = node.parent)
                if (node.ClassListContains("motion-enabled"))
                    return true;
            return false;
        }

        private static void FadeIn(GeometryChangedEvent evt)
        {
            var element = (VisualElement)evt.currentTarget;
            element.UnregisterCallback<GeometryChangedEvent>(FadeIn);
            element.RemoveFromClassList(AppearingClass);
        }

        private static void FadeOut(GeometryChangedEvent evt)
        {
            var element = (VisualElement)evt.currentTarget;
            element.UnregisterCallback<GeometryChangedEvent>(FadeOut);
            element.AddToClassList(ClosingClass);
            element.schedule.Execute(element.RemoveFromHierarchy).ExecuteLater(CloseMilliseconds + 50);
        }

        private static void Close(VisualElement element)
        {
            if (Closed(element))
                return;
            if (!Moving(element) || element.resolvedStyle.display == DisplayStyle.None || Filtered(element))
            {
                element.style.display = DisplayStyle.None;
                return;
            }
            element.RemoveFromClassList(AppearingClass);
            element.AddToClassList(HostClass);
            element.AddToClassList(ClosingClass);
            BlockPointer(element);
            element.schedule.Execute(() => Leave(element)).ExecuteLater(CloseMilliseconds);
        }

        // Out of the layout first; the fade's class goes a frame later, so its opacity never animates back on a shown element.
        private static void Leave(VisualElement element)
        {
            if (!element.ClassListContains(ClosingClass))
                return;
            element.style.display = DisplayStyle.None;
            element.schedule.Execute(() =>
            {
                if (element.style.display == DisplayStyle.None)
                    element.RemoveFromClassList(ClosingClass);
            });
        }

        // Unity's renderer fails (RenderTreeCompositor) when an element fading out holds a filtered one — a presentation's
        // glow, a hit's drop shadow — so such an element leaves at once.
        private static bool Filtered(VisualElement element)
        {
            if (element.resolvedStyle.filter?.Any() == true)
                return true;
            foreach (var child in element.Children())
                if (child.resolvedStyle.display != DisplayStyle.None && Filtered(child))
                    return true;
            return false;
        }

        // A closing element's controls take no press, release or click (registered once; the same callback is not added
        // twice).
        private static void BlockPointer(VisualElement element)
        {
            element.RegisterCallback<PointerDownEvent>(StopWhileClosing, TrickleDown.TrickleDown);
            element.RegisterCallback<PointerUpEvent>(StopWhileClosing, TrickleDown.TrickleDown);
            element.RegisterCallback<ClickEvent>(StopWhileClosing, TrickleDown.TrickleDown);
        }

        private static void StopWhileClosing(EventBase evt)
        {
            if (evt.currentTarget is VisualElement element && element.ClassListContains(ClosingClass))
                evt.StopImmediatePropagation();
        }
    }
}
