using UnityEngine.UIElements;

namespace Wotr.Client
{
    // The look shared by every screen before the board: the Frontend style sheet, (MENU-15) the map backdrop
    // behind the screen's content and (MENU-16) the credit of the original board game along the bottom edge.
    // UI-470 (owner, 2026-11-03): every menu window — these screens, Settings, the questions and the board's Menu —
    // wears the same thin-line chrome (Styles/Menus.uss), scoped to the roots that ApplyChrome marks.
    internal static class FrontendTheme
    {
        public const string Credits =
            "Original game credited to Roberto Di Meglio, Marco Maggi and Francesco Nepitello · Published by Ares Games";
        public const string ChromeClass = "menu-chrome";

        public static void Apply(VisualElement root)
        {
            ApplyChrome(root);
            AddSheet(root, "Wotr/Styles/Frontend");
            root.AddToClassList("frontend");
            root.Insert(0, Backdrop());
            root.Insert(1, CreditLine());
        }

        // The menus' chrome under this root only; the board's HUD keeps its own look.
        public static void ApplyChrome(VisualElement root)
        {
            AddSheet(root, "Wotr/Styles/Menus");
            root.AddToClassList(ChromeClass);
        }

        private static void AddSheet(VisualElement root, string path)
        {
            var sheet = UnityEngine.Resources.Load<StyleSheet>(path);
            if (sheet != null)
                root.styleSheets.Add(sheet);
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
