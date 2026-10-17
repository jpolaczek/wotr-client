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
