using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-160: who acts and what now. The plaque carries the acting side's enamel and opens the decision's host;
    // the hint below holds at most two lines, and its full text stays in the tooltip. UI-214: a card's object-less
    // options replace the hint as up to three buttons in the same place, so the console keeps its height.
    public sealed class ConsoleStatus
    {
        public VisualElement Root { get; } = new VisualElement { name = "console-status" };
        public Button Plaque { get; }
        public Label Headline { get; } = new Label { name = "console-headline", pickingMode = PickingMode.Ignore };
        public Label Hint { get; } = new Label { name = "console-hint" };
        public VisualElement Options { get; } = new VisualElement { name = "console-options" };
        public event Action Clicked;
        public event Action<ConsoleOption> OptionChosen;
        public bool YourTurn => Plaque.ClassListContains("your-turn");
        // UI-241: while any decision of this player waits, the plaque glows and pulses on its own.
        public const string MustActClass = "must-act";
        public bool MustAct => Plaque.ClassListContains(MustActClass);
        private readonly ChoicePulse pulse;
        public int Flashes { get; private set; }
        private readonly VisualElement emblem = new VisualElement { name = "console-emblem", pickingMode = PickingMode.Ignore };
        private readonly VisualElement glow = new VisualElement { name = "console-status-glow", pickingMode = PickingMode.Ignore };
        private IVisualElementScheduledItem flash;
        private string optionsKey = "";

        public ConsoleStatus()
        {
            Root.AddToClassList("console-status");
            Plaque = new Button(() => Clicked?.Invoke()) { name = "console-status-plaque" };
            Plaque.AddToClassList("console-status-plaque");
            pulse = new ChoicePulse(Plaque);
            glow.AddToClassList("console-status-glow");
            Plaque.Add(glow);
            emblem.AddToClassList("console-emblem");
            Plaque.Add(emblem);
            Headline.AddToClassList("console-headline");
            Plaque.Add(Headline);
            Root.Add(Plaque);
            Hint.AddToClassList("console-hint");
            Root.Add(Hint);
            Options.AddToClassList("console-options");
            Options.style.display = DisplayStyle.None;
            Root.Add(Options);
        }

        public void Apply(TurnConsoleState state, string opens)
        {
            Plaque.EnableInClassList("side-free", state.Actor == Side.FreePeoples);
            Plaque.EnableInClassList("side-shadow", state.Actor == Side.Shadow);
            Plaque.EnableInClassList("console-own", state.Own);
            Headline.text = state.Headline;
            Plaque.tooltip = opens.Length > 0 ? state.Headline + "\n" + opens : state.Headline;
        }

        // UI-167: the plaque glows while the player's starting decision waits for its first step.
        public void SetYourTurn(bool value) => Plaque.EnableInClassList("your-turn", value);

        public void SetMustAct(bool value)
        {
            Plaque.EnableInClassList(MustActClass, value);
            pulse.Run(value);
        }

        // UI-167: with reduced motion the ribbon is replaced by one short flash of the glow.
        public void Flash()
        {
            Flashes++;
            flash?.Pause();
            Plaque.AddToClassList("flash");
            flash = Plaque.schedule.Execute(() => Plaque.RemoveFromClassList("flash")).StartingIn(FlashMilliseconds);
        }

        public const long FlashMilliseconds = 700;

        // The buttons are rebuilt only when the set of options changes; their state follows every refresh.
        public void SetOptions(IReadOnlyList<ConsoleOption> options, bool enabled)
        {
            var key = string.Join("|", options.Select(o => o.Name + ":" + o.Label));
            if (key != optionsKey)
            {
                optionsKey = key;
                Options.Clear();
                foreach (var option in options)
                    Options.Add(OptionButton(option));
            }
            for (var i = 0; i < options.Count; i++)
            {
                var button = (Button)Options[i];
                button.userData = options[i];
                button.tooltip = options[i].Tooltip;
                button.SetEnabled(enabled && options[i].Enabled);
            }
            var shown = options.Count > 0;
            Options.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            Hint.style.display = shown ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private Button OptionButton(ConsoleOption option)
        {
            Button button = null;
            button = BoardScreen.Button(option.Label, () =>
            {
                if (button.userData is ConsoleOption current)
                    OptionChosen?.Invoke(current);
            });
            button.name = option.Name;
            button.AddToClassList("console-option");
            return button;
        }

        public void SetHint(string text, string detail = "")
        {
            Hint.text = text;
            Hint.tooltip = detail.Length > 0 ? detail : text;
        }
    }
}
