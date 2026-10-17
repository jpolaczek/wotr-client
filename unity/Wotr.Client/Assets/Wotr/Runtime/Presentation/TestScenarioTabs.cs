using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // TEST-09: the test scenarios in tabs. A card scenario's group names its tab before " · " ("Free Peoples cards ·
    // Character"); every other group — dice, Mordor, endings — stays in the first tab.
    public static class TestScenarioTabs
    {
        public const string General = "Dice & game";
        public const string FreePeoplesCards = "Free Peoples cards";
        public const string ShadowCards = "Shadow cards";
        private const string Separator = " · ";
        public static readonly string[] All = { General, FreePeoplesCards, ShadowCards };

        public static string Of(string group)
        {
            var cut = group.IndexOf(Separator, System.StringComparison.Ordinal);
            var head = cut < 0 ? group : group.Substring(0, cut);
            return head is FreePeoplesCards or ShadowCards ? head : General;
        }

        // The tabs that hold any scenario, in their fixed order.
        public static IReadOnlyList<string> For(TestScenarioCatalogView catalog)
        {
            var present = catalog.Scenarios.Select(scenario => Of(scenario.Group)).ToHashSet();
            return All.Where(present.Contains).ToList();
        }

        // Within a card tab the group's own name drops the tab ("Character"); the first tab keeps whole names.
        public static string GroupTitle(string group)
        {
            var tab = Of(group);
            return tab == General ? group : group.Substring(tab.Length + Separator.Length);
        }

        public static string Name(string tab) => "test-tab-" + tab.ToLowerInvariant().Replace(" & ", "-").Replace(' ', '-');
    }
}
