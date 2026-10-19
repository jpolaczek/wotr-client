using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-263: the waiting room's rules beside the chat: a column of three expansion medallions (rules/medallion with
    // the module's crest, lit in its enamel when the game uses it) and a column of six variant chips (the button frame,
    // red lacquer and a ticked ring when on), every one in a fixed place. A click asks to switch that rule; the strip itself changes only when the
    // server's room comes back (RulesChoices).
    public sealed class RulesStrip
    {
        public VisualElement Root { get; } = new VisualElement { name = "waiting-rules" };
        public event Action<string> Toggled;
        private readonly Dictionary<string, Button> medallions = new Dictionary<string, Button>();
        private readonly Dictionary<string, Button> chips = new Dictionary<string, Button>();

        public RulesStrip()
        {
            Root.AddToClassList("waiting-rules");
            var modules = new VisualElement { name = "waiting-rules-modules" };
            modules.AddToClassList("waiting-rules-modules");
            foreach (var module in RulesChoices.ModuleIds)
                modules.Add(MedallionCell(module));
            Root.Add(modules);
            var variants = new VisualElement { name = "waiting-rules-variants" };
            variants.AddToClassList("waiting-rules-variants");
            foreach (var variant in RulesChoices.VariantIds)
                variants.Add(Chip(variant));
            Root.Add(variants);
        }

        public Button Medallion(string module) => medallions[module];

        public Button VariantChip(string variant) => chips[variant];

        public void Show(RoomView room, bool busy)
        {
            foreach (var choice in RulesChoices.Modules(room))
                Apply(medallions[choice.Id], choice, busy);
            foreach (var choice in RulesChoices.Variants(room))
                Apply(chips[choice.Id], choice, busy);
        }

        private static void Apply(Button button, RuleChoice choice, bool busy)
        {
            button.EnableInClassList("on", choice.On);
            button.EnableInClassList("selected", choice.On && button.ClassListContains("rules-chip"));
            button.SetEnabled(!busy && choice.Enabled);
            button.tooltip = choice.Tooltip;
        }

        private VisualElement MedallionCell(string module)
        {
            var cell = new VisualElement { pickingMode = PickingMode.Ignore };
            cell.AddToClassList("rules-module");
            var button = BoardScreen.Button("", () => Toggled?.Invoke(module));
            button.name = "rules-module-" + module;
            button.AddToClassList("rules-medallion");
            button.AddToClassList("module-" + module);
            var crest = ExpansionCrests.Crest(module);
            crest.AddToClassList("rules-medallion-crest");
            button.Add(crest);
            medallions[module] = button;
            cell.Add(button);
            var name = new Label(ShortName(module)) { pickingMode = PickingMode.Ignore };
            name.AddToClassList("rules-module-name");
            cell.Add(name);
            return cell;
        }

        private Button Chip(string variant)
        {
            var button = BoardScreen.Button("", () => Toggled?.Invoke(variant));
            button.name = "rules-variant-" + variant;
            button.AddToClassList("rules-chip");
            var check = new VisualElement { pickingMode = PickingMode.Ignore };
            check.AddToClassList("rules-check");
            button.Add(check);
            var label = new Label(RulesChoices.VariantName(variant)) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("rules-chip-name");
            button.Add(label);
            chips[variant] = button;
            return button;
        }

        // The medallion's caption: the expansion's own word (Fate, Lords, Kings); the tooltip has its full name.
        private static string ShortName(string module) => module switch
        {
            ExpansionModules.Fate => "Fate",
            ExpansionModules.Lords => "Lords",
            _ => "Kings"
        };
    }
}
