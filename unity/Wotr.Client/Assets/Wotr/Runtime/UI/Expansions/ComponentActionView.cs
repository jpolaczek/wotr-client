using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // Stable controls preserve focus while the chosen die changes their availability. UI-286: a character's ability is
    // a small picture that glows and pulses only while a click can use it.
    public sealed class ComponentActionView
    {
        public VisualElement Root { get; } = BoardScreen.Row("component-actions");
        public event Action<ComponentAction> Selected;
        public Func<ComponentAction, string> Label { get; set; }
        // UI-345: the row fades in and out (UI-321/UI-322) instead of appearing in one frame; the Council's choices in the
        // Dice & tokens panel use it.
        public bool Fades { get; set; }
        private readonly Dictionary<string, Button> buttons = new Dictionary<string, Button>();
        private Dictionary<string, ComponentAction> choices = new Dictionary<string, ComponentAction>();
        private readonly ChoicePulse pulse;

        public ComponentActionView()
        {
            pulse = new ChoicePulse(Root);
        }

        public void Apply(IEnumerable<ComponentAction> actions, bool enabled)
        {
            choices = actions.ToDictionary(a => a.Key);
            foreach (var key in buttons.Keys.Where(key => !choices.ContainsKey(key)).ToArray())
            {
                buttons[key].RemoveFromHierarchy();
                buttons.Remove(key);
            }
            foreach (var item in choices.Values)
            {
                if (!buttons.TryGetValue(item.Key, out var button))
                {
                    var key = item.Key;
                    Action select = () =>
                    {
                        if (choices.TryGetValue(key, out var current))
                            Selected?.Invoke(current);
                    };
                    button = Create(item, select);
                    button.name = "component-" + key;
                    buttons.Add(key, button);
                    Root.Add(button);
                }
                if (item.Icon.Length == 0 && item.Picture.Length == 0)
                    button.text = Label?.Invoke(item) ?? item.Symbol + (item.Owner.Length > 0 ? " " + item.Owner : "");
                button.userData = new DraftToken(DraftObject.Action, item.Key, item.Kind);
                button.tooltip = Tooltip(item);
                button.SetEnabled(enabled && item.Enabled);
                button.EnableInClassList("legal-target", item.Enabled);
                if (item.Picture.Length > 0)
                    ChoiceGlow.Apply(button, enabled && item.Enabled);
            }
            pulse.Run(enabled && choices.Values.Any(item => item.Picture.Length > 0 && item.Enabled));
            if (Fades)
                Appearance.Display(Root, choices.Count > 0);
            else
                Root.style.display = choices.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private static Button Create(ComponentAction item, Action select)
        {
            if (item.Icon.Length > 0)
                return IconButton.Create(item.Icon, item.Detail, select);
            if (item.Picture.Length == 0)
                return BoardScreen.Button("", select);
            var button = IconButton.Picture(item.Picture, Tooltip(item), select);
            button.AddToClassList("ability-icon");
            return button;
        }

        // A picture names its owner and what it does; a text button already shows its owner.
        private static string Tooltip(ComponentAction item)
        {
            if (item.Picture.Length == 0)
                return item.Detail + (item.Enabled ? "" : " · Select an eligible die");
            var detail = (item.Owner.Length > 0 ? item.Owner + " · " : "") + item.Detail;
            return detail + (item.Enabled ? "\nClick to use" : "\nSelect an eligible die to use it");
        }
    }
}
