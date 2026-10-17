using System;
using System.Linq;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-139: the abilities of a character card as chips: a scroll symbol and the ability's title, its full text
    // in the tooltip. A click unrolls the full text under the chips and a second click rolls it up; nothing is
    // unrolled by default. Three chips per page, as in UI-66.
    public sealed class CharacterAbilityChips
    {
        public const string ChipClass = "character-ability";
        public const string TitleClass = "character-ability-title";
        private const int PageSize = 3;
        public VisualElement Root { get; } = new VisualElement { name = "character-abilities" };
        public VisualElement Pages { get; } = BoardScreen.Row("character-ability-pages");
        public Label Text { get; } = new Label { name = "character-ability-text", enableRichText = false };
        private readonly Button[] chips = new Button[PageSize];
        private readonly Button previous, next;
        private string version = "";
        private string opened = "";
        private int page;

        public CharacterAbilityChips()
        {
            Root.AddToClassList("character-abilities");
            for (var index = 0; index < chips.Length; index++)
            {
                var slot = index;
                chips[index] = BoardScreen.Button("", () => Toggle(slot));
                chips[index].AddToClassList(ChipClass);
                // Symbol and title are children: a button with children is not sized by its own text.
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("character-ability-icon");
                var title = new Label { pickingMode = PickingMode.Ignore, enableRichText = false };
                title.AddToClassList(TitleClass);
                chips[index].Add(icon);
                chips[index].Add(title);
                Root.Add(chips[index]);
            }
            previous = IconButton.Create("back", "More abilities", () => Turn(-1));
            next = IconButton.Create("next", "More abilities", () => Turn(1));
            previous.name = "character-abilities-previous";
            next.name = "character-abilities-next";
            Pages.Add(previous);
            Pages.Add(next);
            // Inside the chips' row, on a line of its own: a decision context hides both together.
            Text.AddToClassList("character-ability-text");
            Text.style.display = DisplayStyle.None;
            Root.Add(Text);
        }

        public bool Paged => CharacterAbilityLibrary.For(version).Count > PageSize;

        public void Apply(string definition)
        {
            if (definition != version)
            {
                version = definition;
                page = 0;
                opened = "";
            }
            Refresh();
        }

        private void Turn(int step)
        {
            page += step;
            opened = "";
            Refresh();
        }

        private void Toggle(int slot)
        {
            var id = chips[slot].userData as string ?? "";
            opened = opened == id ? "" : id;
            Refresh();
        }

        private void Refresh()
        {
            var entries = CharacterAbilityLibrary.For(version);
            page = Math.Max(0, Math.Min(page, Math.Max(0, (entries.Count - 1) / PageSize)));
            for (var index = 0; index < chips.Length; index++)
            {
                var chip = chips[index];
                var entry = page * PageSize + index;
                chip.style.display = entry < entries.Count ? DisplayStyle.Flex : DisplayStyle.None;
                if (entry >= entries.Count)
                    continue;
                var ability = entries[entry];
                chip.name = "character-ability-" + ability.Id;
                chip.userData = ability.Id;
                chip.Q<Label>(className: TitleClass).text = ability.Title;
                chip.tooltip = ability.Title + "\n" + ability.Text + "\n\n" + ability.Source;
                chip.EnableInClassList("selected", opened == ability.Id);
            }
            var open = entries.FirstOrDefault(a => a.Id == opened);
            Text.text = open == null ? "" : open.Text + "\n" + open.Source;
            Text.style.display = open == null ? DisplayStyle.None : DisplayStyle.Flex;
            Pages.style.display = entries.Count > PageSize ? DisplayStyle.Flex : DisplayStyle.None;
            previous.SetEnabled(page > 0);
            next.SetEnabled((page + 1) * PageSize < entries.Count);
        }
    }
}
