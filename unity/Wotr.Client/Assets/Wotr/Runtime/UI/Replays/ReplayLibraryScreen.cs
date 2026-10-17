using UnityEngine.UIElements;

namespace Wotr.Client
{
    // REPLAY-04: Replays before sign-in — the library page in the main menu's frame, with Back to sign-in. It needs no
    // account and no server.
    public sealed class ReplayLibraryScreen
    {
        public VisualElement Root { get; } = new VisualElement { name = "replay-library-screen" };
        public ReplayLibraryPage Page { get; } = new ReplayLibraryPage(true);

        public ReplayLibraryScreen()
        {
            FrontendTheme.Apply(Root);
            Root.AddToClassList("gateway");
            Root.AddToClassList("main-menu");
            var page = new VisualElement { pickingMode = PickingMode.Ignore };
            page.AddToClassList("menu-page");
            Root.Add(page);
            var masthead = new VisualElement { pickingMode = PickingMode.Ignore };
            masthead.AddToClassList("menu-masthead");
            var title = new Label("WAR OF THE RING") { pickingMode = PickingMode.Ignore };
            title.AddToClassList("menu-title");
            var player = new Label("Replays on this computer") { pickingMode = PickingMode.Ignore };
            player.AddToClassList("menu-player");
            masthead.Add(title);
            masthead.Add(player);
            page.Add(masthead);
            var window = new VisualElement { name = "replay-window" };
            window.AddToClassList("menu-window");
            window.AddToClassList("replay-window");
            var header = new VisualElement();
            header.AddToClassList("menu-window-header");
            var heading = new Label("Replays") { pickingMode = PickingMode.Ignore };
            heading.AddToClassList("menu-window-title");
            header.Add(heading);
            window.Add(header);
            window.Add(Page.Root);
            page.Add(window);
        }
    }
}
