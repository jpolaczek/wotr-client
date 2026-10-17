using System;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    public sealed class InterfaceController : IDisposable
    {
        public VisualElement Content { get; } = new VisualElement { name = "client-content" };
        public InterfaceSettingsPanel Settings { get; }
        public InterfacePreferences Preferences { get; }
        public WindowLayout Windows { get; } = new WindowLayout();
        private readonly VisualElement root;
        private readonly PanelSettings panel;
        private readonly RuntimeTooltip tooltips;
        public PressFeedback Presses => presses;
        private readonly PressFeedback presses;
        private readonly GestureSounds gestures;
        private readonly DisplayController display;
        private readonly IVisualElementScheduledItem displayRefresh;
        private bool held;
        // UI-180: the screen's edges hold the cursor in a full screen with focus.
        public CursorBarrier Barrier { get; }
        // AUDIO-02: the application's one sound owner; games come and go, it stays.
        public SoundModule Sounds { get; } = new SoundModule();

        public InterfaceController(VisualElement root, PanelSettings panel, InterfacePreferences preferences = null)
        {
            this.root = root;
            this.panel = panel;
            Preferences = preferences ?? new InterfacePreferences();
            var screen = new UnityDisplayScreen();
            Settings = new InterfaceSettingsPanel(Preferences, Windows.Clear, screen, Sounds.TestSound);
            display = new DisplayController(Preferences, screen);
            // UI-176: follow the game window to another screen.
            displayRefresh = root.schedule.Execute(display.Refresh).Every(1000);
            Barrier = new CursorBarrier(Preferences);
            Content.style.flexGrow = 1;
            Content.style.minHeight = 0;
            root.AddToClassList("client-interface");
            root.Add(Content);
            root.Add(Settings.Root);
            tooltips = new RuntimeTooltip(root);
            presses = new PressFeedback(root);
            gestures = new GestureSounds(presses, Sounds, root);
            Preferences.Changed += Apply;
            Settings.Closed += Closed;
            Apply();
        }

        private void Apply()
        {
            tooltips.Dismiss();
            panel.scale = Preferences.Scale;
            root.EnableInClassList("motion-enabled", !Preferences.ReducedMotion);
            root.EnableInClassList("reduced-motion", Preferences.ReducedMotion);
            Sounds.Policy.Levels = Preferences.SoundLevels;
        }

        public void Open()
        {
            tooltips.Dismiss();
            Settings.Show();
            Content.SetEnabled(false);
        }

        private void Closed() => Content.SetEnabled(!held);

        // DIST-07: a layer over every screen and Settings (the update notice and the required-update overlay).
        public void AddLayer(VisualElement layer) => root.Add(layer);

        // DIST-07: while a required update blocks the game, its screen keeps showing but takes no input.
        public void Hold(bool hold)
        {
            held = hold;
            Content.SetEnabled(!hold && !Settings.IsOpen);
        }

        public void Dispose()
        {
            Sounds.Dispose();
            Preferences.Changed -= Apply;
            displayRefresh.Pause();
            display.Dispose();
            Settings.Closed -= Closed;
            Settings.Dispose();
            tooltips.Dispose();
            gestures.Dispose();
            presses.Dispose();
            Content.SetEnabled(true);
        }
    }
}
