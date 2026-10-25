using System.Linq;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-309/UI-310: an object a beat may mark — shown now, its whole branch displayed and laid out.
    public static class PresentationTargets
    {
        public static bool Shown(VisualElement element)
        {
            if (element == null || element.worldBound.width <= 0 || element.worldBound.height <= 0)
                return false;
            for (var node = element; node != null; node = node.parent)
                if (node.resolvedStyle.display == DisplayStyle.None || node.resolvedStyle.visibility == Visibility.Hidden ||
                    node.ClassListContains(Appearance.ClosingClass))
                    return false;
            return true;
        }

        // The first candidate the player sees now.
        public static VisualElement First(params VisualElement[] candidates) => candidates.FirstOrDefault(Shown);
    }
}
