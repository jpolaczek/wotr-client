using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    internal static class ConfigurationSummary
    {
        public static void Render(VisualElement root, MatchConfiguration config)
        {
            root.Add(new Label("Base Second Edition · Configuration locked at start"));
            var choices = BoardScreen.Row("configuration-summary");
            root.Add(choices);
            foreach (var choice in ConfigurationOptions.Modules(config).Concat(ConfigurationOptions.Variants(config)))
                choices.Add(new Label((choice.Value ? "✓ " : "○ ") + choice.Label)
                {
                    name = "rule-" + choice.Id,
                    tooltip = choice.Value ? "Enabled for this game" : "Disabled for this game"
                });
        }
    }
}
