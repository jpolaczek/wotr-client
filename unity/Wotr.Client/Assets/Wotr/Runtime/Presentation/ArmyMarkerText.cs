using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-126: the tooltip of an army marker, built only from the public projection.
    public static class ArmyMarkerText
    {
        public static string Describe(ArmyMarker army, string region)
        {
            var lines = new List<string> { region + " · " + (army.Garrison ? "Garrison" : "Field army") };
            foreach (var force in army.Nations)
                lines.Add(Composition(force));
            if (army.CharacterNames.Count > 0)
                lines.Add("Characters · " + string.Join(", ", army.CharacterNames));
            else if (army.Characters > 0)
                lines.Add("Characters · " + army.Characters);
            if (army.CrownedTitles.Count > 0)
                lines.Add("Led by " + string.Join(", ", army.CrownedTitles) + " · ♙ Initial");
            return string.Join("\n", lines);
        }

        // UI-420 (owner): one form of a nation's figures wherever a force is described (the army marker, the region
        // window): "Gondor · 3 Regular · 1 Elite · 1 Leader".
        public static string Composition(NationForce force) =>
            Composition(force.Nation, force.Regular, force.Elite, force.Leaders, force.Nazgul);

        public static string Composition(string nation, IReadOnlyCollection<UnitKind> kinds) => Composition(nation,
            kinds.Count(kind => kind == UnitKind.Regular), kinds.Count(kind => kind == UnitKind.Elite),
            kinds.Count(kind => kind == UnitKind.Leader), kinds.Count(kind => kind == UnitKind.Nazgul));

        public static string Composition(string nation, int regular, int elite, int leaders, int nazgul) => TooltipText.Parts(
            TooltipText.Nation(nation),
            Figures(regular, "Regular", "Regular"),
            Figures(elite, "Elite", "Elite"),
            Figures(leaders, "Leader", "Leaders"),
            Figures(nazgul, "Nazgûl", "Nazgûl"));

        private static string Figures(int count, string one, string many) => count == 0 ? "" : count + " " + (count == 1 ? one : many);
    }
}
