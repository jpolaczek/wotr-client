using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class FigureGrid
    {
        private readonly List<Action> refresh = new List<Action>();
        public VisualElement Root { get; } = new VisualElement();
        private string expanded;

        public FigureGrid(IEnumerable<UnitView> units, Func<string, bool> selected, Func<string, bool> locked,
            Action<string[]> choose, IEnumerable<CharacterView> characters = null, Action<string[]> chooseCharacters = null,
            bool allowGroupSelection = true, ICollection<string> requiredAnyIds = null)
        {
            var pieces = units.Select(u => new Piece(u.Id, u.Nation, Symbol(u.Kind), Name(u.Nation) + " " + u.Kind,
                FigureAppearances.Art(u), u.Side)).ToList();
            if (characters != null)
                pieces.AddRange(characters.Select(c => new Piece(c.Id, "characters", CharacterLibrary.Symbol(c.DefinitionId),
                    CharacterLibrary.Name(c.DefinitionId), FigureAppearances.Art(c), c.Side)));
            var groups = pieces.GroupBy(p => p.Nation).OrderBy(g => g.Key).ToList();
            var compact = pieces.Count > 18;
            expanded = groups.OrderBy(g => g.Key == "characters").ThenByDescending(g => g.Count()).FirstOrDefault()?.Key;
            Root.AddToClassList(compact ? "figure-picker" : "figure-grid");
            var tabs = BoardScreen.Row();
            if (compact)
                Root.Add(tabs);
            foreach (var group in groups)
            {
                var select = group.Key == "characters" ? chooseCharacters : choose;
                var column = new VisualElement();
                column.AddToClassList("figure-nation");
                var ids = group.Select(p => p.Id).ToArray();
                TextElement heading = !compact && !allowGroupSelection
                    ? new Label(Name(group.Key))
                    : BoardScreen.Button(Name(group.Key), () =>
                    {
                        if (compact)
                        {
                            expanded = group.Key;
                            Refresh();
                        }
                        else
                            select?.Invoke(ids);
                    });
                heading.AddToClassList(compact ? "figure-tab" : "group-heading");
                heading.name = "group-" + group.Key;
                (compact ? tabs : column).Add(heading);
                if (compact && allowGroupSelection)
                    column.Add(BoardScreen.Button("All / none", () => select?.Invoke(ids)));
                var row = BoardScreen.Row();
                column.Add(row);
                foreach (var piece in group)
                    AddFigure(row, piece, selected, locked, select, requiredAnyIds?.Contains(piece.Id) == true);
                Root.Add(column);
                refresh.Add(() =>
                {
                    if (!compact)
                        return;
                    heading.text = Name(group.Key) + "  " + group.Count(p => selected(p.Id)) + "/" + group.Count();
                    heading.tooltip = string.Join(" · ", group.GroupBy(p => p.Label).Select(g => g.Key + " " + g.Count(p => selected(p.Id)) + "/" + g.Count()));
                    heading.EnableInClassList("selected", expanded == group.Key);
                    column.style.display = expanded == group.Key ? DisplayStyle.Flex : DisplayStyle.None;
                });
            }
        }

        private void AddFigure(VisualElement row, Piece piece, Func<string, bool> selected, Func<string, bool> locked, Action<string[]> choose, bool required)
        {
            var button = BoardScreen.Button((required ? "◇ " : "") + piece.Label, () => choose?.Invoke(new[] { piece.Id }));
            button.name = "figure-" + piece.Id;
            button.AddToClassList("figure");
            FigureArtwork.Apply(button, piece.Appearance, piece.Side);
            button.EnableInClassList("required-commander", required);
            row.Add(button);
            refresh.Add(() =>
            {
                var moved = locked(piece.Id);
                button.SetEnabled(!moved);
                button.EnableInClassList("unavailable-figure", moved);
                button.EnableInClassList("selected", selected(piece.Id));
                button.EnableInClassList("left-behind", !selected(piece.Id));
                button.tooltip = piece.Description + (required ? " · This army must include at least one ◇ commander" : "") +
                    (moved ? " · Unavailable / already moved" : selected(piece.Id) ? " · Moving / selected" : " · Staying");
            });
        }

        public void Refresh()
        {
            foreach (var update in refresh)
                update();
        }

        private sealed class Piece
        {
            public string Id, Nation, Label, Description;
            public FigureArt Appearance;
            public Side Side;
            public Piece(string id, string nation, string label, string description, FigureArt appearance, Side side)
            {
                Id = id;
                Nation = nation;
                Label = label;
                Description = description;
                Appearance = appearance;
                Side = side;
            }
        }
        public static string Symbol(UnitKind kind) => kind switch
        {
            UnitKind.Regular => "●",
            UnitKind.Elite => "◆",
            UnitKind.Leader => "▲",
            UnitKind.Nazgul => "N",
            _ => "?"
        };
        public static string Name(string id) => string.Join(" ", id.Split('-').Select(s => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1)));
        private static string ShortName(string id)
        {
            var name = Name(id);
            return name.Substring(0, Math.Min(2, name.Length));
        }
    }
}
