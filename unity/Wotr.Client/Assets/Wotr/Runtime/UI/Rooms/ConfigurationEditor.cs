using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    internal static class ConfigurationEditor
    {
        public static void Render(VisualElement root, MatchConfiguration config, bool expansions, Action<MatchConfiguration> changed)
        {
            root.Clear();
            if (!expansions)
            {
                BaseOnly(root, config, changed);
                return;
            }
            AddRow(root, config, ConfigurationOptions.Modules(config), changed);
            AddRow(root, config, ConfigurationOptions.Variants(config), changed);
        }

        // UI-96: a server that offers only the base game shows no expansion choices. A table set up
        // earlier with expansions can return to the base game in one click.
        private static void BaseOnly(VisualElement root, MatchConfiguration config, Action<MatchConfiguration> changed)
        {
            root.Add(new Label("Base game") { name = "config-base-only" });
            if (ConfigurationOptions.IsBase(config))
                return;
            var reset = BoardScreen.Button("Use the base game", () => changed(new MatchConfiguration()));
            reset.name = "config-use-base";
            reset.tooltip = "This server offers only the base game.";
            root.Add(reset);
        }

        private static void AddRow(VisualElement root, MatchConfiguration config, IEnumerable<ConfigurationOption> choices,
            Action<MatchConfiguration> changed)
        {
            var row = BoardScreen.Row();
            root.Add(row);
            foreach (var choice in choices)
            {
                var toggle = new Toggle(choice.Label) { name = "config-" + choice.Id };
                toggle.SetValueWithoutNotify(choice.Value);
                toggle.RegisterValueChangedCallback(evt =>
                {
                    var copy = JsonConvert.DeserializeObject<MatchConfiguration>(JsonConvert.SerializeObject(config));
                    choice.Set(copy, evt.newValue);
                    toggle.SetValueWithoutNotify(choice.Value);
                    changed(copy);
                });
                row.Add(toggle);
            }
        }
    }
}
