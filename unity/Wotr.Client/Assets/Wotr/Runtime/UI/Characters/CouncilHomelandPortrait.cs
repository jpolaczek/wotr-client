using System;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    internal sealed class CouncilHomelandPortrait
    {
        public Button Root { get; }
        private readonly Label location = new Label { name = "homeland-location" };

        public CouncilHomelandPortrait(CharacterActionOption option, CharacterView character, Action select)
        {
            var definition = character?.DefinitionId ?? option.CharacterId;
            Root = BoardScreen.Button("", select);
            Root.name = "homeland-character-" + option.CharacterId;
            Root.AddToClassList("homeland-portrait");
            Root.userData = new DraftToken(DraftObject.CouncilPortrait, option.CharacterId);
            // Select before a drag starts, so that its own legal map targets are visible.
            Root.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button == 0)
                    select();
            });
            var symbol = new Label(CharacterLibrary.Symbol(definition));
            symbol.AddToClassList("homeland-portrait-symbol");
            Root.Add(symbol);
            Root.Add(new Label(CharacterLibrary.ShortName(definition)));
            location.AddToClassList("homeland-location");
            Root.Add(location);
            var icon = character?.NationIcon;
            var nation = new Label(string.IsNullOrEmpty(icon) ? "—" : "◇ " + FigureGrid.Name(icon));
            nation.name = "homeland-nation";
            nation.AddToClassList("homeland-nation");
            Root.Add(nation);
            Root.tooltip = option.Title + "\n" + CharacterLibrary.Edition(definition) +
                "\nSelect or drag to a highlighted homeland." +
                (character == null ? "" : string.IsNullOrEmpty(icon)
                    ? "\nNo nation icon: placement does not activate a nation."
                    : "\nNation icon: " + FigureGrid.Name(icon) + ". Normal arrival rules apply after confirmation.");
        }

        public void Refresh(bool selected, string region)
        {
            Root.EnableInClassList("selected", selected);
            Root.EnableInClassList("homeland-placed", region.Length > 0);
            location.text = region.Length > 0 ? "→ " + region : "Fellowship";
        }
    }
}
