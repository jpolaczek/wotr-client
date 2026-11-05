using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // TEST-09: the test scenarios in tabs. A card scenario's group names its tab before " · " ("Free Peoples cards ·
    // Character"); every other group — dice, Mordor, endings — stays in the first tab. TEST-10: an expansion scenario
    // stands in its module's tab (Fate, Lords, Kings); a module the server does not release has no scenarios, so its
    // tab stays in place, disabled.
    public static class TestScenarioTabs
    {
        public const string General = "Dice & game";
        public const string FreePeoplesCards = "Free Peoples cards";
        public const string ShadowCards = "Shadow cards";
        public const string Fate = "Fate";
        public const string Lords = "Lords";
        public const string Kings = "Kings";
        private const string Separator = " · ";
        public static readonly string[] Modules = { Fate, Lords, Kings };
        public static readonly string[] All = { General, FreePeoplesCards, ShadowCards, Fate, Lords, Kings };

        public static string Of(TestScenarioView scenario) =>
            scenario.Modules.Count > 0 ? ModuleTab(scenario.Modules[0]) : Of(scenario.Group);

        public static bool IsModule(string tab) => Modules.Contains(tab);

        public static string Of(string group)
        {
            var cut = group.IndexOf(Separator, System.StringComparison.Ordinal);
            var head = cut < 0 ? group : group.Substring(0, cut);
            return head is FreePeoplesCards or ShadowCards ? head : General;
        }

        // The tabs that hold any scenario, in their fixed order.
        public static IReadOnlyList<string> For(TestScenarioCatalogView catalog)
        {
            var present = catalog.Scenarios.Select(Of).ToHashSet();
            return All.Where(present.Contains).ToList();
        }

        // Within a card or module tab the group's own name drops the tab ("Character", "Special dice"); the first tab
        // and a group without the tab's name ("Fate of Erebor") keep whole names.
        public static string GroupTitle(string tab, string group) =>
            tab != General && group.StartsWith(tab + Separator, System.StringComparison.Ordinal)
                ? group.Substring(tab.Length + Separator.Length) : group;

        private static string ModuleTab(string module) => module switch
        {
            ExpansionModules.Fate => Fate,
            ExpansionModules.Lords => Lords,
            _ => Kings
        };

        public static string Name(string tab) => "test-tab-" + tab.ToLowerInvariant().Replace(" & ", "-").Replace(' ', '-');
    }
}
