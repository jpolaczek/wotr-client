using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    internal static class RegionInformation
    {
        public static string Render(VisualElement root, PlayerView view, string region, string selected, Action<string> select)
        {
            var units = view.Units.Where(u => u.Location == region).ToList();
            var characters = view.Characters.Where(c => c.Location == region).ToList();
            var forces = units.Select(u => (u.Side, u.Garrison)).Concat(characters.Select(c => (c.Side, c.Garrison)))
                .Distinct().OrderBy(f => f.Side).ThenBy(f => f.Garrison).ToList();
            var keys = forces.Select(f => Key(f.Side, f.Garrison)).ToList();
            if (characters.Count > 0)
                keys.Add("characters");
            if (!keys.Contains(selected))
                selected = keys.FirstOrDefault() ?? "";
            var tabs = BoardScreen.Row("information-tabs");
            root.Add(tabs);
            foreach (var force in forces)
            {
                var key = Key(force.Side, force.Garrison);
                var button = BoardScreen.Button(ForceName(force.Side, force.Garrison), () => select(key));
                button.name = "inspect-force-" + key;
                button.EnableInClassList("selected", key == selected);
                tabs.Add(button);
            }
            if (characters.Count > 0)
            {
                var button = BoardScreen.Button("Characters", () => select("characters"));
                button.name = "inspect-characters";
                button.EnableInClassList("selected", selected == "characters");
                tabs.Add(button);
            }
            if (selected == "characters")
                Characters(root, characters);
            else if (selected.Length > 0)
                Units(root, units.Where(u => Key(u.Side, u.Garrison) == selected).ToList());
            else
                root.Add(Text("No army or character in this region."));
            return selected;
        }

        private static void Units(VisualElement root, List<UnitView> units)
        {
            var rows = new VisualElement { name = "inspected-units" };
            root.Add(rows);
            foreach (var nation in units.GroupBy(u => u.Nation).OrderBy(g => g.Key))
            {
                var row = BoardScreen.Row("information-unit-row");
                row.name = "inspected-nation-" + nation.Key;
                row.Add(Text(FigureGrid.Name(nation.Key)));
                row.Add(Text(string.Join("   ", nation.GroupBy(u => u.Kind).OrderBy(g => g.Key)
                    .Select(g => FigureGrid.Symbol(g.Key) + " " + g.Count() + " " + g.Key))));
                rows.Add(row);
            }
            if (units.Count == 0)
                rows.Add(Text("No army units · See Characters."));
        }

        private static void Characters(VisualElement root, List<CharacterView> characters)
        {
            var portraits = BoardScreen.Row("information-characters");
            root.Add(portraits);
            foreach (var character in characters.OrderBy(c => c.Side).ThenBy(c => c.DefinitionId))
            {
                var title = CharacterLibrary.Name(character.DefinitionId.Length > 0 ? character.DefinitionId : character.Id);
                var label = Text(title);
                label.name = "inspected-character-" + character.Id;
                label.tooltip = title + " · " + ForceName(character.Side, character.Garrison) +
                    " · Leadership " + character.Leadership + " · Level " + character.Level;
                portraits.Add(label);
            }
        }

        private static Label Text(string text) => new Label(text) { enableRichText = false };
        private static string Key(Side side, bool garrison) => side + (garrison ? "-garrison" : "-field");
        private static string ForceName(Side side, bool garrison) => (side == Side.FreePeoples ? "FP" : "SH") + (garrison ? " · Garrison" : " · Field");
    }
}
