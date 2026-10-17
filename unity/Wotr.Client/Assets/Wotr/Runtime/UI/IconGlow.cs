using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-197: a soft gilt light (art: ornaments/move-glow) behind an icon, so a crest or a figure stands out on dark
    // enamel. Its size and strength come from the style sheet of the panel that holds it; it never takes the pointer.
    internal static class IconGlow
    {
        public const string ClassName = "icon-glow";

        public static VisualElement Create(string name)
        {
            var glow = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            glow.AddToClassList(ClassName);
            return glow;
        }
    }
}
