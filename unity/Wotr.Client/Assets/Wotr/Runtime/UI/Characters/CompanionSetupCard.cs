using System;
using System.Linq;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    internal sealed class CompanionSetupCard
    {
        public Button Root { get; }
        public string DefinitionId { get; }
        private readonly Label selection = new Label();

        public CompanionSetupCard(string definitionId, string caption, Action choose)
        {
            DefinitionId = definitionId;
            Root = BoardScreen.Button("", choose);
            Root.name = "version-" + definitionId;
            Root.AddToClassList("setup-version-card");
            var header = BoardScreen.Row("setup-version-heading");
            header.Add(new Label(caption));
            selection.AddToClassList("setup-version-selection");
            header.Add(selection);
            Root.Add(header);
            var title = new Label(CharacterLibrary.Name(definitionId));
            title.AddToClassList("setup-version-title");
            Root.Add(title);
            Root.Add(Statistics(definitionId));
            foreach (var ability in CharacterAbilityLibrary.For(definitionId))
            {
                var label = new Label("◇ " + ability.Title) { focusable = true, enableRichText = false,
                    name = "setup-ability-" + ability.Id,
                    tooltip = ability.Title + "\n" + ability.Text + "\n\n" + ability.Source };
                label.AddToClassList("setup-version-ability");
                Root.Add(label);
            }
            Root.tooltip = "Click to choose " + CharacterLibrary.Name(definitionId) + " · " + caption;
        }

        public void Select(bool selected)
        {
            Root.EnableInClassList("selected", selected);
            // UI-345: the version not chosen glows as a choice.
            ChoiceGlow.Apply(Root, !selected);
            selection.text = selected ? "✓" : "○";
        }

        private static Label Statistics(string id)
        {
            var card = CharacterLibrary.Find(id);
            var movement = card?.Abilities.Contains("movement-level-two") == true ? 2 : card?.Level;
            var nation = string.IsNullOrEmpty(card?.NationIcon) ? "None" : FigureGrid.Name(card.NationIcon);
            var label = new Label("Level " + card?.Level + "  ·  Move " + movement +
                "  ·  Leadership " + card?.Leadership + "\nNation " + nation);
            label.AddToClassList("setup-version-statistics");
            return label;
        }
    }
}
