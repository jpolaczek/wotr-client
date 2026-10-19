using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-263: the expansions' crests (rules/crest-*): one on its medallion in the waiting room, and a small row of them
    // wherever a game is listed or played (Open games, Saved games, Replays, the phase plaque, the Victory header).
    // A base game shows none; the row's tooltip names the expansions and the variants in play.
    public static class ExpansionCrests
    {
        public const string RowClass = "expansion-crests";

        public static VisualElement Crest(string module)
        {
            var crest = new VisualElement { name = "crest-" + module, pickingMode = PickingMode.Ignore };
            crest.AddToClassList("expansion-crest");
            crest.AddToClassList("crest-" + module);
            return crest;
        }

        // A row of small crests that shows a configuration; empty and without a tooltip for the base game.
        public static VisualElement Row(string name)
        {
            var row = new VisualElement { name = name };
            row.AddToClassList(RowClass);
            return row;
        }

        // The crests are rebuilt only when the expansions change; every view refresh keeps them.
        public static void Show(VisualElement row, MatchConfiguration configuration)
        {
            configuration ??= new MatchConfiguration();
            row.tooltip = Tooltip(configuration);
            var modules = string.Join(",", RulesChoices.ModuleIds.Where(module => RulesChoices.IsOn(configuration, module)));
            if (row.userData is string shown && shown == modules)
                return;
            row.userData = modules;
            row.Clear();
            foreach (var module in RulesChoices.ModuleIds.Where(module => RulesChoices.IsOn(configuration, module)))
                row.Add(Crest(module));
            row.EnableInClassList("base-game", row.childCount == 0);
        }

        // "Fate of Erebor + Kings of Middle-earth\nVariants: New Cities · Dismay"; empty for the base game.
        public static string Tooltip(MatchConfiguration configuration)
        {
            if (RulesChoices.IsBase(configuration))
                return "";
            var variants = RulesChoices.VariantSummary(configuration);
            return RulesChoices.Summary(configuration) + (variants.Length > 0 ? "\nVariants: " + variants : "");
        }
    }
}
