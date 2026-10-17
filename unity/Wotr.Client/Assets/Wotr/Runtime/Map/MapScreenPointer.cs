using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // The screen host samples native position even when UI Toolkit cannot pick
    // an element at the exclusive right/bottom boundary. Click routing is unchanged.
    public static class MapScreenPointer
    {
        public static void Update(MapCanvas map)
        {
            if (map?.panel == null)
                return;
            var buttons = 0;
            for (var button = 0; button < 3; button++)
                if (Input.GetMouseButton(button))
                    buttons |= 1 << button;
            Observe(map, Input.mousePosition, new Vector2(Screen.width, Screen.height),
                Application.isFocused, Input.mousePresent, buttons, FillsScreen());
        }

        // The editor's game view is a window inside the editor whatever its full-screen setting.
        private static bool FillsScreen() => !Application.isEditor && Screen.fullScreenMode != FullScreenMode.Windowed;

        public static void Observe(MapCanvas map, Vector2 position, Vector2 screenSize,
            bool focused, bool present, int pressedButtons, bool fillsScreen)
        {
            if (map?.panel == null)
                return;
            map.EdgePan.SetFocused(focused);
            if (!focused || !present || !TryInside(position, screenSize, fillsScreen, out var inside))
            {
                map.EdgePan.Stop();
                return;
            }
            var topLeft = new Vector2(inside.x, screenSize.y - inside.y);
            var point = RuntimePanelUtils.ScreenToPanel(map.panel, topLeft);
            map.EdgePan.SamplePointer(point, pressedButtons);
        }

        // UI-174: in a full-screen mode the physical edges of the screen bound the game, so a cursor beyond the
        // picture (a bar around another aspect ratio, or past the screen's edge) holds at the picture's edge and
        // keeps panning; a window loses the cursor one pixel outside.
        public static bool TryInside(Vector2 position, Vector2 screenSize, bool fillsScreen, out Vector2 inside)
        {
            inside = position;
            if (screenSize.x <= 0 || screenSize.y <= 0)
                return false;
            if (fillsScreen)
            {
                inside = new Vector2(Mathf.Clamp(position.x, 0, screenSize.x), Mathf.Clamp(position.y, 0, screenSize.y));
                return true;
            }
            return position.x >= 0 && position.x <= screenSize.x && position.y >= 0 && position.y <= screenSize.y;
        }
    }
}
