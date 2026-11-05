using System;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-499 (R35, docs/ui/RANKED_UX.md): the move clock in the top bar of a ranked game. It reads real time, never
    // Time.timeScale, and writes its text only when the text changes. Past the warning point it turns red with a slight
    // pulse, which reduced motion drops, and its owner hears the warning once per decision (Warning). Mute silences the
    // sound and leaves the colour.
    public sealed class TopBarClock
    {
        public const string WarningClass = "move-clock-warning";
        private const long TickMilliseconds = 100;
        private const double PulsesPerSecond = 1;
        private const float LowestOpacity = 0.72f;

        public Label Root { get; } = new Label { name = "move-clock" };
        // UI-500: beside the clock; disabled, not hidden, with no pauses left or during a pause.
        public Button PauseButton { get; }
        public event Action PauseRequested;
        public event Action<string> Warning;
        private readonly MoveClockCountdown countdown = new MoveClockCountdown();
        private readonly Func<bool> reducedMotion;
        private Side you;
        private string warned = "";

        public TopBarClock(Func<bool> reducedMotion)
        {
            this.reducedMotion = reducedMotion;
            Root.AddToClassList("move-clock");
            Root.tooltip = "The time left for this decision. Whoever runs out of time loses the game.";
            Root.schedule.Execute(Tick).Every(TickMilliseconds);
            PauseButton = BoardScreen.Button("Pause", () => PauseRequested?.Invoke());
            PauseButton.name = "move-clock-pause";
            PauseButton.AddToClassList("move-clock-pause");
        }

        public bool Active => countdown.Active;

        public void Show(MoveClockView clock, Side viewer)
        {
            you = viewer;
            countdown.Sync(clock, Now);
            if (!countdown.Active)
                return;
            ShowPauses(clock);
            Tick();
        }

        private void ShowPauses(MoveClockView clock)
        {
            PauseButton.SetEnabled(clock.PausesLeft > 0 && !clock.Paused);
            PauseButton.tooltip = clock.Paused ? "The game is paused."
                : clock.PausesLeft == 0 ? "You have no pauses left. Your opponent has " + clock.OpponentPausesLeft + "."
                : "Pause the game. You have " + clock.PausesLeft + " left, your opponent " + clock.OpponentPausesLeft + "; either of you can resume.";
        }

        private static double Now => Time.realtimeSinceStartupAsDouble;

        private void Tick()
        {
            if (!countdown.Active)
                return;
            var now = Now;
            var text = (countdown.Owner == you ? "You " : "Opponent ") + MoveClockCountdown.Format(countdown.Remaining(now));
            if (Root.text != text)
                Root.text = text;
            var warning = countdown.InWarning(now);
            Root.EnableInClassList(WarningClass, warning);
            Root.style.opacity = warning && !reducedMotion() ? Pulse(now) : 1;
            if (warning && !countdown.Paused && countdown.Owner == you && warned != countdown.DecisionId)
            {
                warned = countdown.DecisionId;
                Warning?.Invoke(warned);
            }
        }

        private static float Pulse(double now)
        {
            var wave = 0.5 + 0.5 * Math.Cos(2 * Math.PI * PulsesPerSecond * now);
            return LowestOpacity + (1 - LowestOpacity) * (float)wave;
        }
    }
}
