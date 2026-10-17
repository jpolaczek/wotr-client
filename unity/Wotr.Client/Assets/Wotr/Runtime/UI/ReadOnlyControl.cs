using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-235: a control that cannot act now is shown as its picture only — no button frame, hover or press — while its
    // tooltip still explains it. A control turns interactive again when a decision needs it.
    public static class ReadOnlyControl
    {
        public const string ClassName = "read-only";

        public static void Apply(VisualElement control, bool interactive) => control.EnableInClassList(ClassName, !interactive);

        public static bool Is(VisualElement control) => control.ClassListContains(ClassName);
    }
}
