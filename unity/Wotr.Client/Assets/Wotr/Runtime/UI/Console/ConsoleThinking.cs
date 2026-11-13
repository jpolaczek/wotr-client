using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Owner (2026-11-11): a CPU that thinks for a while must not look like a frozen game. While the CPU opponent holds the
    // decision, the start of the console's line shows the One Ring turning and "CPU is thinking…"; the hint gives way to
    // them. With reduced motion the ring stands still and only the dots after the words change.
    public sealed class ConsoleThinking
    {
        public const string Words = "CPU is thinking";
        public const long FrameMilliseconds = 40;
        // One turn in 1.6 s.
        public const float DegreesPerFrame = 9f;
        public const long DotMilliseconds = 500;

        public VisualElement Root { get; } = new VisualElement { name = "console-thinking", pickingMode = PickingMode.Ignore };
        public VisualElement Ring { get; } = new VisualElement { name = "console-thinking-ring", pickingMode = PickingMode.Ignore };
        public Label Text { get; } = new Label { name = "console-thinking-text", pickingMode = PickingMode.Ignore };
        public bool Shown => Root.style.display == DisplayStyle.Flex;
        public float Angle { get; private set; }
        private IVisualElementScheduledItem turning;
        private IVisualElementScheduledItem dots;
        private int dotCount = 3;

        public ConsoleThinking()
        {
            Root.AddToClassList("console-thinking");
            Ring.AddToClassList("console-thinking-ring");
            Text.AddToClassList("console-thinking-text");
            Root.Add(Ring);
            Root.Add(Text);
            Root.tooltip = "The CPU is choosing its move. The game goes on by itself.";
            Root.style.display = DisplayStyle.None;
            Text.text = Words + "…";
            turning = Ring.schedule.Execute(Turn).Every(FrameMilliseconds);
            turning.Pause();
            dots = Text.schedule.Execute(NextDots).Every(DotMilliseconds);
            dots.Pause();
        }

        // Shows or hides the indicator; the animation runs only while it is shown.
        public void Show(bool value)
        {
            if (value == Shown)
                return;
            Root.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            if (value)
            {
                turning.Resume();
                dots.Resume();
                return;
            }
            turning.Pause();
            dots.Pause();
            dotCount = 3;
            Text.text = Words + "…";
        }

        private void Turn()
        {
            if (ReducedMotion())
                return;
            Angle = (Angle + DegreesPerFrame) % 360f;
            Ring.style.rotate = new Rotate(new Angle(Angle, AngleUnit.Degree));
        }

        // Reduced motion keeps the ring still, so the words carry the sign of life: one to three dots in turn.
        private void NextDots()
        {
            if (!ReducedMotion())
            {
                Text.text = Words + "…";
                return;
            }
            dotCount = dotCount % 3 + 1;
            Text.text = Words + new string('.', dotCount);
        }

        private bool ReducedMotion()
        {
            for (var element = Root; element != null; element = element.parent)
                if (element.ClassListContains("reduced-motion"))
                    return true;
            return false;
        }

        // The CPU thinks when the match is against the CPU, still running and connected, nothing of this player is being
        // sent or presented, and the decision is the CPU's.
        public static bool Thinking(PlayerView view, bool cpuOpponent, bool online, bool busy) =>
            cpuOpponent && online && !busy && view != null && view.Winner == null && view.DecisionOwner.HasValue &&
            view.DecisionOwner.Value != view.You;
    }
}
