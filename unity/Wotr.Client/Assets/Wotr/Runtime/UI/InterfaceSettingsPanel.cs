using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    public sealed class InterfaceSettingsPanel : IDisposable
    {
        public VisualElement Root { get; } = new VisualElement { name = "interface-settings" };
        public bool IsOpen => Root.style.display.value == DisplayStyle.Flex;
        public event Action Closed;
        // UI-505: Account page, which opens the site's account page in the system browser (AVATAR-01 A4).
        public event Action AccountPageRequested;
        private readonly InterfacePreferences preferences;
        private readonly Button normal;
        private readonly Button large;
        // UI-529: the map's figures at their standard size or larger.
        private readonly Button[] figureSizes;
        // UI-321 (owner rule): smooth transitions by default; this setting makes every change happen at once.
        private readonly Toggle motion = new Toggle("Skip animations") { name = "reduced-motion" };
        private readonly Button close;
        private VisualElement returnFocus;
        private readonly VisualElement panel = new VisualElement();
        private readonly VisualElement interfaceColumn = new VisualElement { name = "interface-options" };
        private readonly DisplaySettingsSection display;
        private readonly SoundSettingsSection sound;

        // MENU-12: two columns — the interface (size, motion, window positions) and the display (window mode and
        // resolution). MENU-20: the server comes from the build's configuration file, never from Settings. AUDIO-03: the
        // Sound settings follow the interface options in the first column.
        public InterfaceSettingsPanel(InterfacePreferences preferences, Action resetWindows = null, IDisplayScreen screen = null,
            Action testSound = null)
        {
            this.preferences = preferences;
            Root.AddToClassList("settings-scrim");
            // UI-470 (owner, 2026-11-03): Settings wears the menus' thin-line chrome, over the menus and the board alike.
            FrontendTheme.ApplyChrome(Root);
            Root.style.display = DisplayStyle.None;
            panel.AddToClassList("interface-settings-panel");
            Root.Add(panel);
            close = IconButton.Create("close", "Close", Close);
            close.name = "close-interface";
            panel.Add(Heading());
            var columns = new VisualElement();
            columns.AddToClassList("settings-columns");
            panel.Add(columns);
            normal = ScaleButton("scale-100", 1f);
            large = ScaleButton("scale-125", 1.25f);
            figureSizes = Array.ConvertAll(InterfacePreferences.FigureSizes, FigureSizeButton);
            FillInterfaceColumn(resetWindows);
            sound = new SoundSettingsSection(preferences, testSound);
            interfaceColumn.Add(sound.Root);
            columns.Add(interfaceColumn);
            display = new DisplaySettingsSection(preferences, screen ?? new UnityDisplayScreen());
            columns.Add(display.Root);
            AddFigureSizes(display.Root);
            preferences.Changed += Render;
            Root.RegisterCallback<KeyDownEvent>(KeyDown, TrickleDown.TrickleDown);
            Render();
        }

        private VisualElement Heading()
        {
            var heading = BoardScreen.Row();
            heading.AddToClassList("settings-heading");
            heading.Add(BoardScreen.Title("Settings"));
            heading.Add(close);
            return heading;
        }

        private void FillInterfaceColumn(Action resetWindows)
        {
            interfaceColumn.AddToClassList("settings-column");
            interfaceColumn.Add(new Label("Interface size"));
            var scales = BoardScreen.Row();
            scales.Add(normal);
            scales.Add(large);
            interfaceColumn.Add(scales);
            motion.RegisterValueChangedCallback(e => preferences.SetReducedMotion(e.newValue));
            interfaceColumn.Add(motion);
            interfaceColumn.Add(new Label("Every change happens at once, without animation. Choices always appear immediately.") { name = "motion-description" });
            var reset = BoardScreen.Button("Reset window positions", () => resetWindows?.Invoke());
            reset.name = "reset-window-layout";
            interfaceColumn.Add(reset);
            var account = BoardScreen.Button("Account page", () => AccountPageRequested?.Invoke());
            account.name = "open-account-page";
            account.tooltip = "Choose your avatar on the game's site, in your browser.";
            interfaceColumn.Add(account);
        }

        // DIST-07: a full-width section under the two columns (Updates).
        public void AddSection(VisualElement section)
        {
            section.AddToClassList("settings-section");
            panel.Add(section);
        }

        public static Button OpenButton(Action open)
        {
            var button = BoardScreen.Button("Settings", open);
            button.name = "open-interface";
            button.AddToClassList("interface-button");
            button.tooltip = "Interface size, figure size, motion, sound, window mode and resolution.";
            return button;
        }

        private Button ScaleButton(string name, float scale)
        {
            var button = BoardScreen.Button("", () => preferences.SetScale(scale));
            button.name = name;
            button.AddToClassList("scale-choice");
            return button;
        }

        // UI-529: under the resolutions, where the shorter column has room; the interface column would push Settings past
        // a 1366 × 768 screen at 125 %.
        private void AddFigureSizes(VisualElement column)
        {
            column.Add(new Label("Figures on the map · larger ones may overlap") { name = "figure-size-heading" });
            var sizes = BoardScreen.Row();
            sizes.name = "figure-sizes";
            foreach (var size in figureSizes)
                sizes.Add(size);
            column.Add(sizes);
        }

        private Button FigureSizeButton(float size)
        {
            var button = BoardScreen.Button("", () => preferences.SetFigureSize(size));
            button.name = "figure-size-" + Percent(size);
            button.AddToClassList("figure-size-choice");
            return button;
        }

        private static int Percent(float size) => Mathf.RoundToInt(size * 100);

        private void Render()
        {
            normal.text = preferences.Scale == 1f ? "100% ✓" : "100%";
            large.text = preferences.Scale == 1.25f ? "125% ✓" : "125%";
            normal.EnableInClassList("selected", preferences.Scale == 1f);
            large.EnableInClassList("selected", preferences.Scale == 1.25f);
            for (var i = 0; i < figureSizes.Length; i++)
            {
                var size = InterfacePreferences.FigureSizes[i];
                var chosen = preferences.FigureSize == size;
                figureSizes[i].text = Percent(size) + "%" + (chosen ? " ✓" : "");
                figureSizes[i].EnableInClassList("selected", chosen);
            }
            motion.SetValueWithoutNotify(preferences.ReducedMotion);
            display.Render();
            sound.Render();
        }

        public void Show()
        {
            if (IsOpen)
                return;
            returnFocus = Root.panel?.focusController.focusedElement as VisualElement;
            // The window may have moved to another screen since Settings last opened.
            display.Render();
            Root.style.display = DisplayStyle.Flex;
            Root.BringToFront();
            Root.schedule.Execute(() => { if (IsOpen) close.Focus(); });
        }

        public void Close()
        {
            Root.style.display = DisplayStyle.None;
            Closed?.Invoke();
            if (returnFocus?.panel != null)
                returnFocus.Focus();
        }

        private void KeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape)
                return;
            Close();
            evt.StopImmediatePropagation();
        }

        public void Dispose() => preferences.Changed -= Render;
    }
}
