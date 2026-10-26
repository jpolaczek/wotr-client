using System;
using System.Linq;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-160: the four permanent actions of the console. Each keeps its place; an action not available now is
    // greyed out with the reason in its tooltip.
    public sealed class ConsoleActions
    {
        public VisualElement Root { get; } = new VisualElement { name = "console-actions" };
        public Button Back { get; }
        public Button Confirm { get; }
        public Button Pass { get; }
        public Button Discard { get; }
        public event Action BackClicked;
        public event Action ConfirmClicked;
        public event Action PassClicked;
        public event Action DiscardClicked;
        private readonly ChoicePulse pulse;

        public ConsoleActions()
        {
            Root.AddToClassList("console-actions");
            pulse = new ChoicePulse(Root);
            Back = Add("back", "console-back", "Back · Undo the last unsent step", () => BackClicked?.Invoke());
            Confirm = Add("confirm", "console-confirm", "Finish · End this action", () => ConfirmClicked?.Invoke());
            Pass = Add("pass", "console-pass", "Pass", () => PassClicked?.Invoke());
            Discard = Add("discard", "console-discard", "Discard the selected die without effect", () => DiscardClicked?.Invoke());
        }

        public static void Set(Button button, bool enabled, string tooltip)
        {
            button.SetEnabled(enabled);
            button.tooltip = tooltip;
        }

        // UI-291: an action a click can use now glows and pulses (Pass, also as a reaction's decline).
        public void Invite(Button button, bool on)
        {
            ChoiceGlow.Apply(button, on);
            pulse.Run(Root.Children().Any(child => child.ClassListContains(ChoiceGlow.ClassName)));
        }

        private Button Add(string icon, string name, string tooltip, Action action)
        {
            var button = IconButton.Create(icon, tooltip, action);
            button.name = name;
            button.AddToClassList("console-action");
            Root.Add(button);
            return button;
        }
    }
}
