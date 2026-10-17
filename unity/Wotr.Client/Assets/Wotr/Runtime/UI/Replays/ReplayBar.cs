using System;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public enum ReplaySpeed { Slow, Normal, Fast }

    // REPLAY-05: the controls of a replay in a lane under the viewing bar — previous and next turn and action, Play or
    // Pause, the timeline of all steps, where the game stands and the pace of Play. Every control keeps its place; a
    // way past an end is greyed out.
    public sealed class ReplayBar
    {
        public VisualElement Root { get; } = new VisualElement { name = "replay-lane", pickingMode = PickingMode.Ignore };
        public VisualElement Bar { get; } = new VisualElement { name = "replay-bar" };
        public event Action PreviousTurn;
        public event Action Previous;
        public event Action PlayPause;
        public event Action Next;
        public event Action NextTurn;
        public event Action<int> Seek;
        public event Action<ReplaySpeed> SpeedChosen;
        public bool Visible => Root.style.display == DisplayStyle.Flex;
        public SliderInt Timeline { get; } = new SliderInt(0, 0) { name = "replay-timeline" };
        private readonly Label position = new Label { name = "replay-position", pickingMode = PickingMode.Ignore };
        private readonly Button previousTurn;
        private readonly Button previous;
        private readonly Button play;
        private readonly Button next;
        private readonly Button nextTurn;
        private readonly Button[] speeds = new Button[3];

        public ReplayBar()
        {
            Root.AddToClassList("replay-lane");
            Root.style.display = DisplayStyle.None;
            Bar.AddToClassList("replay-bar");
            Bar.AddToClassList("map-edge-control");
            Root.Add(Bar);
            previousTurn = Control("replay-previous-turn", "◀◀ Turn", "The start of the previous turn", () => PreviousTurn?.Invoke());
            previous = Control("replay-previous", "◀ Step", "The previous step", () => Previous?.Invoke());
            play = Control("replay-play", "Play", "Play the replay step by step", () => PlayPause?.Invoke());
            next = Control("replay-next", "Step ▶", "The next step", () => Next?.Invoke());
            nextTurn = Control("replay-next-turn", "Turn ▶▶", "The start of the next turn", () => NextTurn?.Invoke());
            Timeline.AddToClassList("replay-timeline");
            Timeline.RegisterValueChangedCallback(e => Seek?.Invoke(e.newValue));
            Bar.Add(Timeline);
            position.AddToClassList("replay-position");
            Bar.Add(position);
            speeds[0] = Speed("replay-speed-slow", "Slow", ReplaySpeed.Slow);
            speeds[1] = Speed("replay-speed-normal", "Normal", ReplaySpeed.Normal);
            speeds[2] = Speed("replay-speed-fast", "Fast", ReplaySpeed.Fast);
        }

        public void Apply(int step, int count, int turn, GamePhase phase, bool playing, bool hasPreviousTurn, bool hasNextTurn, ReplaySpeed speed)
        {
            Root.style.display = DisplayStyle.Flex;
            Timeline.highValue = Math.Max(0, count - 1);
            Timeline.SetValueWithoutNotify(step);
            position.text = $"Step {step + 1}/{count} · Turn {turn} · {BoardScreen.PhaseName(phase)}";
            var last = step >= count - 1;
            previousTurn.SetEnabled(hasPreviousTurn);
            previous.SetEnabled(step > 0);
            next.SetEnabled(!last);
            nextTurn.SetEnabled(hasNextTurn);
            play.text = playing ? "Pause" : "Play";
            play.SetEnabled(playing || !last);
            for (var index = 0; index < speeds.Length; index++)
                speeds[index].EnableInClassList("selected", (int)speed == index);
        }

        private Button Control(string name, string text, string tooltip, Action click)
        {
            var button = BoardScreen.Button(text, click);
            button.name = name;
            button.tooltip = tooltip;
            button.AddToClassList("replay-control");
            Bar.Add(button);
            return button;
        }

        private Button Speed(string name, string text, ReplaySpeed speed)
        {
            var button = Control(name, text, "Play at a " + text.ToLowerInvariant() + " pace", () => SpeedChosen?.Invoke(speed));
            button.AddToClassList("replay-speed");
            return button;
        }
    }
}
