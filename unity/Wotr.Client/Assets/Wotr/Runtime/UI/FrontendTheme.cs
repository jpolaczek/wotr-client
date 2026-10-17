using UnityEngine.UIElements;

namespace Wotr.Client
{
    // The look shared by every screen before the board: the Frontend style sheet, (MENU-15) the map backdrop
    // behind the screen's content and (MENU-16) the credit of the original board game along the bottom edge.
    internal static class FrontendTheme
    {
        public const string Credits =
            "Original game credited to Roberto Di Meglio, Marco Maggi and Francesco Nepitello · Published by Ares Games";

        public static void Apply(VisualElement root)
        {
            var sheet = UnityEngine.Resources.Load<StyleSheet>("Wotr/Styles/Frontend");
            if (sheet != null)
                root.styleSheets.Add(sheet);
            root.AddToClassList("frontend");
            root.Insert(0, Backdrop());
            root.Insert(1, CreditLine());
        }

        private static Label CreditLine()
        {
            var credits = new Label(Credits) { name = "frontend-credits", pickingMode = PickingMode.Ignore };
            credits.AddToClassList("frontend-credits");
            return credits;
        }

        private static VisualElement Backdrop()
        {
            var backdrop = new VisualElement { name = "frontend-backdrop", pickingMode = PickingMode.Ignore };
            backdrop.AddToClassList("frontend-backdrop");
            return backdrop;
        }
    }
}
