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
        private readonly InterfacePreferences preferences;
        private readonly Button normal;
        private readonly Button large;
        private readonly Toggle motion = new Toggle("Reduced motion") { name = "reduced-motion" };
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
            FillInterfaceColumn(resetWindows);
            sound = new SoundSettingsSection(preferences, testSound);
            interfaceColumn.Add(sound.Root);
            columns.Add(interfaceColumn);
            display = new DisplaySettingsSection(preferences, screen ?? new UnityDisplayScreen());
            columns.Add(display.Root);
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
            interfaceColumn.Add(new Label("Skip visual transitions. Choices always appear immediately.") { name = "motion-description" });
            var reset = BoardScreen.Button("Reset window positions", () => resetWindows?.Invoke());
            reset.name = "reset-window-layout";
            interfaceColumn.Add(reset);
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
            button.tooltip = "Interface size, motion, sound, window mode and resolution.";
            return button;
        }

        private Button ScaleButton(string name, float scale)
        {
            var button = BoardScreen.Button("", () => preferences.SetScale(scale));
            button.name = name;
            button.AddToClassList("scale-choice");
            return button;
        }

        private void Render()
        {
            normal.text = preferences.Scale == 1f ? "100% ✓" : "100%";
            large.text = preferences.Scale == 1.25f ? "125% ✓" : "125%";
            normal.EnableInClassList("selected", preferences.Scale == 1f);
            large.EnableInClassList("selected", preferences.Scale == 1.25f);
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
