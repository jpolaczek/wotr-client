using System;
using UnityEngine;

namespace Wotr.Client
{
    // MENU-12: applies the saved resolution and window mode at start and after each change in Settings. The editor's
    // game view keeps its own size, so only a player build changes the window. MENU-17: a full-screen mode uses the
    // screen's own resolution, so Unity draws no black bars. UI-176: the screen is the one that holds the game window,
    // and Refresh follows the window to another screen.
    public sealed class DisplayController : IDisposable
    {
        private readonly InterfacePreferences preferences;
        private readonly IDisplayScreen screen;
        private readonly Action<DisplaySize, WindowMode> apply;
        private string applied = "";

        // apply changes the window; by default the player's screen, and nothing in the editor's game view.
        public DisplayController(InterfacePreferences preferences, IDisplayScreen screen, Action<DisplaySize, WindowMode> apply = null)
        {
            this.preferences = preferences;
            this.screen = screen;
            this.apply = apply ?? (Application.isEditor ? null : (Action<DisplaySize, WindowMode>)SetScreen);
            preferences.Changed += Apply;
            Apply();
        }

        // UI-176: called once a second. A full-screen game moved to another screen, or a screen whose resolution
        // changed, gets the size shaped to that screen; a window is only resized when its size no longer fits.
        public void Refresh() => Apply();

        private void Apply()
        {
            if (apply == null)
                return;
            var mode = preferences.WindowMode;
            var size = DisplayOptions.Resolve(preferences.Resolution, screen, mode);
            var key = size.Key + "/" + mode + (DisplayOptions.FillsScreen(mode) ? "/" + screen.Native.Key : "");
            if (key == applied)
                return;
            applied = key;
            apply(size, mode);
        }

        private static void SetScreen(DisplaySize size, WindowMode mode)
        {
            Screen.SetResolution(size.Width, size.Height, FullScreen(mode));
            Cursor.lockState = CursorLock(mode);
        }

        // UI-174: a full-screen game keeps the cursor on its own screen, so the screen's edges are the physical limits
        // of edge panning (Windows; elsewhere MapScreenPointer holds a cursor beyond the picture at its edge).
        public static CursorLockMode CursorLock(WindowMode mode) =>
            DisplayOptions.FillsScreen(mode) ? CursorLockMode.Confined : CursorLockMode.None;

        public static FullScreenMode FullScreen(WindowMode mode)
        {
            switch (mode)
            {
                case WindowMode.Fullscreen:
                    return Application.platform == RuntimePlatform.WindowsPlayer ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.MaximizedWindow;
                case WindowMode.Windowed:
                    return FullScreenMode.Windowed;
                default:
                    return FullScreenMode.FullScreenWindow;
            }
        }

        public void Dispose() => preferences.Changed -= Apply;
    }
}
