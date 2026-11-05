using System;
using System.Linq;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-160: the four permanent actions of the console. Each keeps its place; an action not available now is
    // greyed out with the reason in its tooltip. UI-455 (owner, 2026-11-03): a small Cancel under them, always in its
    // place, greyed while there is nothing to cancel (DraftCancel).
    public sealed class ConsoleActions
    {
        public VisualElement Root { get; } = new VisualElement { name = "console-actions" };
        public Button Back { get; }
        public Button Confirm { get; }
        public Button Pass { get; }
        public Button Discard { get; }
        public Button Cancel { get; }
        public event Action BackClicked;
        public event Action ConfirmClicked;
        public event Action PassClicked;
        public event Action DiscardClicked;
        public event Action CancelClicked;
        private readonly ChoicePulse pulse;

        public ConsoleActions()
        {
            Root.AddToClassList("console-actions");
            pulse = new ChoicePulse(Root);
            Back = Add("back", "console-back", "Back · Undo the last unsent step", () => BackClicked?.Invoke());
            Confirm = Add("confirm", "console-confirm", "Finish · Available during an action of several steps", () => ConfirmClicked?.Invoke());
            Pass = Add("pass", "console-pass", "Pass · " + PassDetail, () => PassClicked?.Invoke());
            Discard = Add("discard", "console-discard", "Discard the selected die without effect", () => DiscardClicked?.Invoke());
            Cancel = AddCancel();
        }

        public const string CancelName = "console-cancel";

        // UI-455: the Cancel is a small labelled button on its own line under the 2 x 2 icons.
        private Button AddCancel()
        {
            var button = BoardScreen.Button("Cancel", () => CancelClicked?.Invoke());
            button.name = CancelName;
            button.tooltip = DraftCancel.NothingTooltip;
            button.AddToClassList("console-cancel");
            button.SetEnabled(false);
            Root.Add(button);
            return button;
        }

        // UI-420 (owner): Pass says what passing does.
        public const string PassDetail = "Take no action now; the opponent acts next";

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
