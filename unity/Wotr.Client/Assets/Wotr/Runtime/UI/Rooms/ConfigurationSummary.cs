using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // The board's "Game rules": the expansions' crests and every rule of the waiting room's strip (UI-263) with
    // whether this game uses it. The rules are fixed once the game starts.
    internal static class ConfigurationSummary
    {
        public static void Render(VisualElement root, MatchConfiguration config)
        {
            root.Add(new Label(RulesChoices.Summary(config) + " · Rules fixed at the start") { name = "rules-summary" });
            var crests = ExpansionCrests.Row("rules-crests");
            ExpansionCrests.Show(crests, config);
            root.Add(crests);
            var choices = BoardScreen.Row("configuration-summary");
            root.Add(choices);
            foreach (var id in RulesChoices.ModuleIds.Concat(RulesChoices.VariantIds))
            {
                var on = RulesChoices.IsOn(config, id);
                var name = RulesChoices.ModuleIds.Contains(id) ? RulesChoices.ModuleName(id) : RulesChoices.VariantName(id);
                choices.Add(new Label((on ? "✓ " : "○ ") + name)
                {
                    name = "rule-" + id,
                    tooltip = on ? "In this game" : "Not in this game"
                });
            }
        }
    }
}
