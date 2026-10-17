using System.Collections.Generic;
using System.Linq;

namespace Wotr.Client
{
    // What the screen offers: the size of the screen that holds the game window (UI-176).
    public interface IDisplayScreen
    {
        DisplaySize Native { get; }
    }

    public sealed class DisplayChoice
    {
        public DisplaySize Size { get; }
        // The player can pick this size now; in a full-screen mode no size can be picked (MENU-17).
        public bool Supported { get; }
        public string Label { get; }
        public string Tooltip { get; }

        public DisplayChoice(DisplaySize size, bool supported, string label, string tooltip)
        {
            Size = size;
            Supported = supported;
            Label = label;
            Tooltip = tooltip;
        }
    }

    // The fixed list of the most common desktop resolutions; every preset keeps its place. MENU-17: a full-screen
    // mode (Fullscreen or Borderless) always uses the screen's own resolution, so the list is greyed there and only
    // a window (Windowed) picks its size; a preset larger than the screen cannot be picked either.
    public static class DisplayOptions
    {
        public static readonly IReadOnlyList<DisplaySize> Presets = new[]
        {
            DisplaySize.Native,
            new DisplaySize(1280, 720),
            new DisplaySize(1366, 768),
            new DisplaySize(1600, 900),
            new DisplaySize(1920, 1080),
            new DisplaySize(2560, 1440),
            new DisplaySize(3840, 2160)
        };

        public static bool IsPreset(DisplaySize size) => Presets.Contains(size);

        public static bool FillsScreen(WindowMode mode) => mode != WindowMode.Windowed;

        public static IReadOnlyList<DisplayChoice> Choices(IDisplayScreen screen, WindowMode mode) =>
            Presets.Select(size => Choice(size, screen, mode)).ToArray();

        // A window can take the preset while it fits the screen; a full-screen mode takes none.
        public static bool Supports(DisplaySize preset, IDisplayScreen screen, WindowMode mode) =>
            !FillsScreen(mode) && (preset.IsNative || preset.Fits(screen.Native));

        // The size to apply: the screen's own in a full-screen mode, otherwise the chosen preset while it fits.
        public static DisplaySize Resolve(DisplaySize chosen, IDisplayScreen screen, WindowMode mode) =>
            InUse(chosen, screen, mode).IsNative ? screen.Native : chosen;

        // The preset the settings mark as in use: Native in a full-screen mode or when the saved size no longer fits.
        public static DisplaySize InUse(DisplaySize chosen, IDisplayScreen screen, WindowMode mode) =>
            Supports(chosen, screen, mode) ? chosen : DisplaySize.Native;

        private static DisplayChoice Choice(DisplaySize preset, IDisplayScreen screen, WindowMode mode)
        {
            var label = preset.IsNative ? "Native" : preset.ToString();
            return new DisplayChoice(preset, Supports(preset, screen, mode), label, Tooltip(preset, screen, mode));
        }

        private static string Tooltip(DisplaySize preset, IDisplayScreen screen, WindowMode mode)
        {
            if (FillsScreen(mode))
                return "Full screen uses this screen's resolution (" + screen.Native + ") · Choose Windowed to pick a size";
            if (preset.IsNative)
                return "This screen's resolution · " + screen.Native;
            return preset.Fits(screen.Native) ? preset.ToString() : preset + " · Larger than this screen (" + screen.Native + ")";
        }
    }
}
