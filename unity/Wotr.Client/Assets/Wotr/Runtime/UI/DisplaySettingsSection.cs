using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // MENU-12: the Display column of Settings — the window mode and the resolution presets. Every preset keeps its
    // place; one that cannot be picked is greyed and says why in its tooltip (MENU-17: all of them in a full-screen
    // mode). A click applies at once.
    public sealed class DisplaySettingsSection
    {
        private static readonly WindowMode[] Modes = { WindowMode.Fullscreen, WindowMode.Borderless, WindowMode.Windowed };
        public VisualElement Root { get; } = new VisualElement { name = "display-settings" };
        private readonly InterfacePreferences preferences;
        private readonly IDisplayScreen screen;
        private readonly Dictionary<WindowMode, Button> modeButtons = new Dictionary<WindowMode, Button>();
        private readonly Dictionary<DisplaySize, Button> sizeButtons = new Dictionary<DisplaySize, Button>();

        public DisplaySettingsSection(InterfacePreferences preferences, IDisplayScreen screen)
        {
            this.preferences = preferences;
            this.screen = screen;
            Root.AddToClassList("settings-column");
            Root.Add(new Label("Window mode"));
            Root.Add(ModeRow());
            Root.Add(new Label("Resolution"));
            Root.Add(SizeGrid());
            Render();
        }

        private VisualElement ModeRow()
        {
            var row = new VisualElement { name = "window-modes" };
            row.AddToClassList("display-choices");
            foreach (var mode in Modes)
            {
                var button = BoardScreen.Button("", () => preferences.SetWindowMode(mode));
                button.name = "window-mode-" + mode.ToString().ToLowerInvariant();
                button.AddToClassList("display-choice");
                button.tooltip = ModeTooltip(mode);
                modeButtons.Add(mode, button);
                row.Add(button);
            }
            return row;
        }

        private VisualElement SizeGrid()
        {
            var grid = new VisualElement { name = "resolutions" };
            grid.AddToClassList("display-choices");
            foreach (var size in DisplayOptions.Presets)
            {
                var button = BoardScreen.Button("", () => preferences.SetResolution(size));
                button.name = "resolution-" + size.Key;
                button.AddToClassList("display-choice");
                sizeButtons.Add(size, button);
                grid.Add(button);
            }
            return grid;
        }

        // The selected resolution is the one in use: Native in a full-screen mode (MENU-17, every preset greyed in
        // place), or a saved preset this screen cannot show.
        public void Render()
        {
            foreach (var pair in modeButtons)
                Mark(pair.Value, pair.Key.ToString(), pair.Key == preferences.WindowMode);
            var used = DisplayOptions.InUse(preferences.Resolution, screen, preferences.WindowMode);
            foreach (var choice in DisplayOptions.Choices(screen, preferences.WindowMode))
            {
                var button = sizeButtons[choice.Size];
                Mark(button, choice.Label, choice.Size.Equals(used));
                button.tooltip = choice.Tooltip;
                button.SetEnabled(choice.Supported);
            }
        }

        private static void Mark(Button button, string label, bool selected)
        {
            button.text = selected ? label + " ✓" : label;
            button.EnableInClassList("selected", selected);
        }

        private static string ModeTooltip(WindowMode mode)
        {
            switch (mode)
            {
                case WindowMode.Fullscreen:
                    return "The game takes over the screen.";
                case WindowMode.Windowed:
                    return "A framed window of the chosen resolution.";
                default:
                    return "A window without a frame that covers the screen.";
            }
        }
    }
}
