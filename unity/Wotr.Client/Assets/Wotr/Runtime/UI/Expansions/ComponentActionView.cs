using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // Stable controls preserve focus while the chosen die changes their availability.
    public sealed class ComponentActionView
    {
        public VisualElement Root { get; } = BoardScreen.Row("component-actions");
        public event Action<ComponentAction> Selected;
        public Func<ComponentAction, string> Label { get; set; }
        private readonly Dictionary<string, Button> buttons = new Dictionary<string, Button>();
        private Dictionary<string, ComponentAction> choices = new Dictionary<string, ComponentAction>();

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
                    button = item.Icon.Length > 0 ? IconButton.Create(item.Icon, item.Detail, select) : BoardScreen.Button("", select);
                    button.name = "component-" + key;
                    buttons.Add(key, button);
                    Root.Add(button);
                }
                if (item.Icon.Length == 0)
                    button.text = Label?.Invoke(item) ?? item.Symbol + (item.Owner.Length > 0 ? " " + item.Owner : "");
                button.userData = new DraftToken(DraftObject.Action, item.Key, item.Kind);
                button.tooltip = item.Detail + (item.Enabled ? "" : " · Select an eligible die");
                button.SetEnabled(enabled && item.Enabled);
                button.EnableInClassList("legal-target", item.Enabled);
            }
            Root.style.display = choices.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
