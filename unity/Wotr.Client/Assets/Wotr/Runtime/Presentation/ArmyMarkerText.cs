using System.Collections.Generic;

namespace Wotr.Client
{
    // UI-126: the tooltip of an army marker, built only from the public projection.
    public static class ArmyMarkerText
    {
        public static string Describe(ArmyMarker army, string region)
        {
            var lines = new List<string> { region + " · " + (army.Garrison ? "Garrison" : "Field army") };
            foreach (var force in army.Nations)
                lines.Add(FigureGrid.Name(force.Nation) + ": " + Figures(force));
            if (army.CharacterNames.Count > 0)
                lines.Add("Characters: " + string.Join(", ", army.CharacterNames));
            else if (army.Characters > 0)
                lines.Add("Characters: " + army.Characters);
            if (army.CrownedTitles.Count > 0)
                lines.Add("Led by " + string.Join(", ", army.CrownedTitles) + " (not awakened)");
            return string.Join("\n", lines);
        }

        private static string Figures(NationForce force)
        {
            var parts = new List<string>();
            Add(parts, force.Regular, "regular", "regular");
            Add(parts, force.Elite, "elite", "elite");
            Add(parts, force.Leaders, "leader", "leaders");
            Add(parts, force.Nazgul, "Nazgûl", "Nazgûl");
            return string.Join(", ", parts);
        }

        private static void Add(List<string> parts, int count, string one, string many)
        {
            if (count > 0)
                parts.Add(count + " " + (count == 1 ? one : many));
        }
    }
}
