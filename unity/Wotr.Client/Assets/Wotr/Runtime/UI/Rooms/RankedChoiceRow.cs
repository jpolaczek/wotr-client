using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-497 (docs/ui/RANKED_UX.md): one row of the Find game window, a name and its segments in fixed places. A click on a
    // segment chooses it, and every segment's tooltip says what it means for the match. Unavailable segments are disabled,
    // not hidden, with their tooltip saying why.
    public sealed class RankedChoiceRow
    {
        public const string SelectedClass = "selected";
        public VisualElement Root { get; } = new VisualElement();
        public RankedChoice Value { get; private set; }
        public event Action<RankedChoice> Chosen;
        private readonly Dictionary<RankedChoice, Button> segments = new Dictionary<RankedChoice, Button>();

        public RankedChoiceRow(string id, string caption, string detail, params RankedChoice[] choices)
        {
            Root.name = id;
            Root.AddToClassList("find-row");
            var label = new VisualElement { name = id + "-label", pickingMode = PickingMode.Ignore };
            label.AddToClassList("find-row-label");
            var name = new Label(caption) { name = id + "-name", pickingMode = PickingMode.Ignore };
            name.AddToClassList("find-row-name");
            label.Add(name);
            if (detail.Length > 0)
                label.Add(Detail(id, detail));
            Root.Add(label);
            var buttons = new VisualElement { name = id + "-segments" };
            buttons.AddToClassList("host-segments");
            buttons.AddToClassList("find-segments");
            foreach (var choice in choices)
                buttons.Add(Segment(id, choice));
            Root.Add(buttons);
            Show(choices[0]);
        }

        public void Show(RankedChoice value)
        {
            Value = value;
            foreach (var pair in segments)
                pair.Value.EnableInClassList(SelectedClass, pair.Key == value);
        }

        public void Explain(RankedChoice choice, string meaning) => segments[choice].tooltip = meaning;

        public void Allow(RankedChoice choice, bool allowed) => segments[choice].SetEnabled(allowed);

        private static Label Detail(string id, string detail)
        {
            var label = new Label(detail) { name = id + "-detail", pickingMode = PickingMode.Ignore };
            label.AddToClassList("find-row-detail");
            return label;
        }

        private Button Segment(string id, RankedChoice choice)
        {
            var button = BoardScreen.Button(choice.ToString(), () => Choose(choice));
            button.name = id + "-" + choice.ToString().ToLowerInvariant();
            button.AddToClassList("host-segment");
            segments[choice] = button;
            return button;
        }

        private void Choose(RankedChoice choice)
        {
            Show(choice);
            Chosen?.Invoke(choice);
        }
    }
}
