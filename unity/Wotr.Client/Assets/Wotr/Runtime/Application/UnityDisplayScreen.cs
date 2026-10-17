using UnityEngine;

namespace Wotr.Client
{
    // MENU-12: the screen as Unity reports it. UI-176: the screen that holds the game window
    // (Screen.mainWindowDisplayInfo), not the system's main display; Display.main only when the window's screen is
    // unknown.
    public sealed class UnityDisplayScreen : IDisplayScreen
    {
        public DisplaySize Native
        {
            get
            {
                var window = Screen.mainWindowDisplayInfo;
                return window.width > 0 && window.height > 0
                    ? new DisplaySize(window.width, window.height)
                    : new DisplaySize(Display.main.systemWidth, Display.main.systemHeight);
            }
        }
    }
}
