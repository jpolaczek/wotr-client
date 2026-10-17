using UnityEngine;

namespace Wotr.Client
{
    // UI-180: in a full screen with focus, the edges of the game's screen are a barrier for the cursor. Windows confines
    // it through Unity (CursorLockMode.Confined); macOS cannot, so the screen under the cursor is remembered while the
    // cursor is over the game and a cursor that leaves that screen is put back on its nearest edge.
    public sealed class CursorBarrier
    {
        private readonly InterfacePreferences preferences;
        private Rect? screen;

        public CursorBarrier(InterfacePreferences preferences) => this.preferences = preferences;

        // A player build in a full-screen mode, with focus; never the editor's game view.
        public static bool Active(bool player, WindowMode mode, bool focused) =>
            player && focused && DisplayOptions.FillsScreen(mode);

        // The nearest point inside the area (its far edges excluded); true when the point had left it.
        public static bool Clamp(Vector2 point, Rect area, out Vector2 inside)
        {
            inside = new Vector2(Mathf.Clamp(point.x, area.xMin, area.xMax - 1), Mathf.Clamp(point.y, area.yMin, area.yMax - 1));
            return inside != point;
        }

        // Called once a frame by the client host.
        public void Update()
        {
            if (!Active(!Application.isEditor, preferences.WindowMode, Application.isFocused))
            {
                screen = null;
                return;
            }
            // Windows and Linux (X11/XWayland, the Steam Deck) confine the cursor themselves; macOS needs KeepOnScreen.
            if ((Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.LinuxPlayer) &&
                Cursor.lockState != CursorLockMode.Confined)
                Cursor.lockState = CursorLockMode.Confined;
            else if (Application.platform == RuntimePlatform.OSXPlayer)
                KeepOnScreen();
        }

        private void KeepOnScreen()
        {
            var cursor = MacCursor.Location();
            if (OverGame())
                screen = MacCursor.ScreenOf(cursor) ?? screen;
            if (screen is Rect area && Clamp(cursor, area, out var inside))
                MacCursor.Warp(inside);
        }

        private static bool OverGame()
        {
            var position = Input.mousePosition;
            return position.x >= 0 && position.y >= 0 && position.x <= Screen.width && position.y <= Screen.height;
        }
    }
}
