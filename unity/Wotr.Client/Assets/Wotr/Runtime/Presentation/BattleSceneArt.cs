using System;
using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-206: the painted battle illustration for a region, chosen by its settlement and the side it belongs to, in one of
    // two versions. Regions of the same kind and side alternate between the versions in the order of their ids, so the
    // choice is the same for both players and every round, and each version has its places.
    public static class BattleSceneArt
    {
        public const string Open = "open";
        public const string Wild = "wild";
        public const int Versions = 2;

        public static string Key(RegionDefinition region, IEnumerable<RegionDefinition> board) =>
            Group(region) + "-" + Version(region, board);

        // UI-226: a town, city or stronghold first shows its nation's picture (its current nation, so Fate and the White
        // Hand count); the side's UI-206 picture stays the fallback when the nation's is missing.
        public static IReadOnlyList<string> Keys(RegionDefinition region, IEnumerable<RegionDefinition> board)
        {
            var places = board as ICollection<RegionDefinition> ?? board.ToList();
            var keys = new List<string>(2);
            var nation = NationGroup(region);
            if (nation != null)
                keys.Add(nation + "-" + NationVersion(region, places));
            keys.Add(Key(region, places));
            return keys;
        }

        public static string NationGroup(RegionDefinition region)
        {
            var kind = Kind(region.Settlement);
            return kind is "town" or "city" or "stronghold" && !string.IsNullOrEmpty(region.Nation) ? kind + "-" + region.Nation : null;
        }

        // Two versions where the nation has two or more such places, otherwise one; alternating by region id.
        public static int NationVersion(RegionDefinition region, IEnumerable<RegionDefinition> board)
        {
            var group = NationGroup(region);
            var places = board.Where(other => NationGroup(other) == group)
                .Select(other => other.Id)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();
            var versions = Math.Max(1, Math.Min(Versions, places.Count));
            return 1 + Math.Max(0, places.IndexOf(region.Id)) % versions;
        }

        public static string Group(RegionDefinition region)
        {
            var kind = Kind(region.Settlement);
            return kind + "-" + Place(kind, region.OriginalOwner);
        }

        public static string Kind(string settlement) => settlement switch
        {
            "Town" => "town",
            "City" => "city",
            "Stronghold" => "stronghold",
            "Fortification" => "fortification",
            _ => Open
        };

        // Fortifications stand on the Free Peoples' frontier; open land that belongs to no side is wild.
        public static string Place(string kind, Side? owner)
        {
            if (kind == "fortification" || owner == Side.FreePeoples)
                return "free";
            if (owner == Side.Shadow)
                return "shadow";
            return kind == Open ? Wild : "free";
        }

        public static int Version(RegionDefinition region, IEnumerable<RegionDefinition> board)
        {
            var group = Group(region);
            var index = board.Where(other => Group(other) == group)
                .Select(other => other.Id)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList()
                .IndexOf(region.Id);
            return 1 + Math.Max(0, index) % Versions;
        }
    }
}
